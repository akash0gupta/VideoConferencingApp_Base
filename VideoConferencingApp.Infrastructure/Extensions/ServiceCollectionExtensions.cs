using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using VideoConferencingApp.Application.EventHandlers;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Interfaces;
using VideoConferencingApp.Application.Interfaces.Common.IAuthServices;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Interfaces.Common.INotificationServices;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;
using VideoConferencingApp.Application.Interfaces.Common.IUserServices;
using VideoConferencingApp.Application.Services;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Infrastructure.Auth;
using VideoConferencingApp.Infrastructure.Caching;
using VideoConferencingApp.Infrastructure.Caching.Presence;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.HealthChecks;
using VideoConferencingApp.Infrastructure.Configuration.Redis;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Infrastructure.EventsPublisher;
using VideoConferencingApp.Infrastructure.Messaging;
using VideoConferencingApp.Infrastructure.Messaging.Kafka;
using VideoConferencingApp.Infrastructure.Messaging.RabbitMq;
using VideoConferencingApp.Infrastructure.Notifications;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Repositories;
using VideoConferencingApp.Infrastructure.Persistence.Migrations;
using VideoConferencingApp.Infrastructure.RealTime;
using VideoConferencingApp.Infrastructure.Services;
using VideoConferencingApp.Services;

namespace VideoConferencingApp.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = new AppSettings();

            // Load all IConfig classes dynamically
            var configTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IConfig).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var type in configTypes)
            {
                var tempInstance = (IConfig)Activator.CreateInstance(type)!;
                var section = configuration.GetSection(tempInstance.SectionName);
                if (!section.Exists()) continue;

                var instance = (IConfig)section.Get(type);
                if (instance != null)
                    appSettings.Register(instance);

            }

            services.AddSingleton(appSettings);
            //services.AddSingleton<IHostedService>(provider =>
            //  new ConfigReloadService(provider, appSettings, configTypes));

            return services;
        }

        public static void AddJwtAuthentication(this IServiceCollection services)
        {
            // --- Ensure AppSettings is registered first ---
            var sp = services.BuildServiceProvider();
            var appSettings = sp.GetService<AppSettings>()
                ?? throw new InvalidOperationException("AddAutoConfigurations must be called before AddInfrastructure.");

            var jwtConfig = appSettings.Get<JwtSettings>();

            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            //add main jwt authentication
            authenticationBuilder.AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // If the request is for our hub...
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        //var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        //logger.LogWarning("Authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });
        }
        public static void AddSwagger(this IServiceCollection services)
        {
            // Add Swagger services
            services.AddSwaggerGen(c =>
            {
                //c.SwaggerDoc("v1", new OpenApiInfo { Title = "Project API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
             {
             {
                 new OpenApiSecurityScheme
                 {
                     Reference = new OpenApiReference
                     {
                         Type = ReferenceType.SecurityScheme,
                         Id = "Bearer"
                     }
                 },
                 new string[] { }
             }
         });
            });
        }

        public static void AddCorsConfiguration(this IServiceCollection services)
        {
            // --- Ensure AppSettings is registered first ---
            var sp = services.BuildServiceProvider();
            var appSettings = sp.GetService<AppSettings>()
                ?? throw new InvalidOperationException("AddAutoConfigurations must be called before AddInfrastructure.");

            var cacheSettings = appSettings.Get<CommonConfig>();
            services.AddCors(options =>
            {
                options.AddPolicy(name: "DefaultCorsPolicy", policy =>
                {
                    if (string.IsNullOrWhiteSpace(cacheSettings.CorsDomains))
                    {
                        policy.WithOrigins();
                    }
                    else
                    {
                        var allowedOrigins = cacheSettings.CorsDomains
                             .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(o => o.Trim())
                             .ToArray();
                        policy.WithOrigins(allowedOrigins).AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                    }
                });
            });
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // --- Ensure AppSettings is registered first ---
            var sp = services.BuildServiceProvider();
            var appSettings = sp.GetService<AppSettings>()
                ?? throw new InvalidOperationException("AddAutoConfigurations must be called before AddInfrastructure.");

            services.AddJwtAuthentication();
            //services.AddCorsConfiguration();
            services.AddSwagger();


            // --- Database Settings ---
            services.DataBaseSettings(appSettings);

            // --- Cache Services ---
            services.RegisterCacheServices(appSettings);

            // --- Message Broker ---
            services.RegisterMessageBroker(appSettings);

            // --- Health Checks ---
            services.RegisterHealthChecks();

            // --- Auto-register all Event Handlers using Scrutor ---
            services.Scan(scan => scan
                .FromAssemblyOf<ContactRequestAcceptedEventHandler>()
                .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // --- Application Services ---
            // 📡 Add SignalR with security options
            services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.MaximumReceiveMessageSize = 102400; // 100 KB limit
                options.StreamBufferCapacity = 10;
            });

            // Register services
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
            services.AddSingleton<IRateLimitService, RateLimitService>();
            services.AddSingleton<IInputValidator, InputValidator>();
            services.AddSingleton<IAuditLogger, AuditLogger>();

            services.AddHttpContextAccessor();
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<INotificationService, SignalRNotificationService>();
            services.AddSingleton<IEmailService,EmailService>();
            services.AddSingleton<ISmsService,SmsService>();
            services.AddSingleton<BaseDataProvider, SqlServerDataProvider>();
            services.AddSingleton<INameCompatibility, DefaultNameCompatibility>();
            services.AddScoped<IEventPublisher, EventPublisher>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IContactService, ContactService>();
            return services;
        }

        private static void DataBaseSettings(this IServiceCollection services, AppSettings appSettings)
        {
            var databaseConfig = appSettings.Get<DataSettings>();

            if (string.IsNullOrEmpty(databaseConfig.ConnectionString))
                throw new InvalidOperationException("DefaultConnection string not found.");

            services.AddSingleton(Options.Create(databaseConfig));
            DataSettingsManager.Init(databaseConfig);

            // --- FluentMigrator Setup ---
            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddSqlServer()
                    .WithGlobalConnectionString(databaseConfig.ConnectionString)
                    .ScanIn(typeof(SchemaMigration).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole());

            services.AddScoped<IUnitOfWork,UnitOfWork>();
        }

        private static void RegisterHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "sql" })
                .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "redis" })
                .AddCheck<MessageBrokerHealthCheck>("message_broker", tags: new[] { "messaging" });
        }

        private static void RegisterCacheServices(this IServiceCollection services, AppSettings appSettings)
        {
            var cacheSettings = appSettings.Get<CacheSettings>();
            bool useRedis = cacheSettings.CacheProvider == CacheProviderType.Hybrid || cacheSettings.CacheProvider == CacheProviderType.Distributed;
            if (useRedis)
            {
                var redisSettings = appSettings.Get<RedisSettings>();
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisSettings.ConnectionStrings[CacheSettings.CacheConnectionKey];
                    options.InstanceName = redisSettings.RedisInstanceName;
                });
            }
       
            services.AddMemoryCache(options =>
            {
                if (cacheSettings.MemorySizeLimitMB > 0)
                    options.SizeLimit = cacheSettings.MemorySizeLimitMB * 1024L * 1024L;
            });

            switch (cacheSettings.CacheProvider)
            {
                case CacheProviderType.Hybrid:
                    services.AddSingleton<IStaticCacheManager, HybridCacheManager>();
                    break;
                case CacheProviderType.Distributed:
                    services.AddSingleton<IStaticCacheManager, DistributedCacheManager>();
                    break;
                case CacheProviderType.InMemory:
                    services.AddSingleton<IStaticCacheManager, InMemoryCacheManager>();
                    break;
                case CacheProviderType.None:
                default:
                    services.AddSingleton<IStaticCacheManager, NullStaticCacheManager>();
                    break;
            }

            RegisterPresenceServices(services, appSettings);
        }

        private static void RegisterPresenceServices(this IServiceCollection services, AppSettings appSettings)
        {
            var cacheSettings = appSettings.Get<CacheSettings>();
            bool useRedis = cacheSettings.PresenceProviderType == CacheProviderType.Hybrid
                           || cacheSettings.PresenceProviderType == CacheProviderType.Distributed;

            if (useRedis)
                services.AddSingleton<RedisConnectionManager>();

            switch (cacheSettings.PresenceProviderType)
            {
                case CacheProviderType.Hybrid:
                    services.AddSingleton<IPresenceService, HybridPresenceService>();
                    break;
                case CacheProviderType.Distributed:
                    services.AddSingleton<IPresenceService, RedisPresenceService>();
                    break;
                default:
                    services.AddSingleton<IPresenceService, InMemoryPresenceService>();
                    break;
            }
        }
        private static void RegisterMessageBroker(this IServiceCollection services, AppSettings appSettings)
        {
            var messageBrokerSettings = appSettings.Get<MessageBrokerSettings>();
            switch (messageBrokerSettings.Provider)
            {
                case MessageBusProvider.Kafka:
                    services.AddSingleton<KafkaConnection>();
                    services.AddSingleton<IMessageProducer, KafkaProducer>();
                    break;
                case MessageBusProvider.RabbitMQ:
                    services.AddSingleton<RabbitMqConnection>();
                    services.AddSingleton<IMessageProducer, RabbitMqProducer>();
                    break;
                default:
                    services.AddSingleton<IMessageProducer, InMemoryEventBus>();
                    break;
            }

            services.AddHostedService<EventBusSubscriberHostedService>();
        }
    }

}
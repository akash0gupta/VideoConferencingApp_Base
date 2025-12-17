using FluentMigrator.Runner;
using Mapster;
using MapsterMapper;
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
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Common.INotificationServices;
using VideoConferencingApp.Application.EventHandlers;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Services.ContactServices;
using VideoConferencingApp.Application.Services.FileServices;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Application.Services.UserServices;
using VideoConferencingApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using VideoConferencingApp.Infrastructure.Caching;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.HealthChecks;
using VideoConferencingApp.Infrastructure.Configuration.Redis;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Infrastructure.EventsPublisher;
using VideoConferencingApp.Infrastructure.FileDocumentManager;
using VideoConferencingApp.Infrastructure.Messaging;
using VideoConferencingApp.Infrastructure.Messaging.InMemory;
using VideoConferencingApp.Infrastructure.Messaging.Kafka;
using VideoConferencingApp.Infrastructure.Messaging.RabbitMq;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Repositories;
using VideoConferencingApp.Infrastructure.Persistence.Migrations;
using VideoConferencingApp.Infrastructure.Services;
using VideoConferencingApp.Infrastructure.Services.AuthServices;
using VideoConferencingApp.Infrastructure.Services.NotificationServices;


namespace VideoConferencingApp.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // --- Ensure AppSettings is registered first ---
            var sp = services.BuildServiceProvider();
            var appSettings = sp.GetService<AppSettings>()
                ?? throw new InvalidOperationException("AddAutoConfigurations must be called before AddInfrastructure.");

            services.AddJwtAuthentication();
            services.AddCorsConfiguration();
            services.AddSwagger();
            services.AddApplicationMappings();
            services.RegisterFileDocumentManager();


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
            //Add SignalR with security options
            services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.MaximumReceiveMessageSize = 102400; // 100 KB limit
                options.StreamBufferCapacity = 10;
            });

            // Register services
            services.AddSingleton<IFirebasePushNotificationService,FirebasePushNotificationService>();
            services.AddSingleton<IRateLimitService, RateLimitService>();
            services.AddHttpContextAccessor();
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            services.AddScoped<IBCryptPasswordServices,BCryptPasswordServices>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<ISmsService, SmsService>();
            services.AddSingleton<IEventValidator, EventValidator>();
            services.AddSingleton<BaseDataProvider, SqlServerDataProvider>();
            services.AddSingleton<INameCompatibility, DefaultNameCompatibility>();
            services.AddScoped<IEventPublisher, EventPublisher>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IHttpContextService, HttpContextService>();
            services.AddScoped<IResponseHeaderService, ResponseHeaderService>();
            services.AddScoped<IUserDeviceTokenService,UserDeviceTokenService>();
            services.AddScoped<IUserFileManagerService,UserFileManagerService>();
            services.AddScoped<IFileManagerService,FileManagerService>();
            services.AddScoped<INotificationOrchestrator,NotificationOrchestrator>();

            services.AddScoped<IConnectionManagerService, ConnectionManagerService>();
            services.AddScoped<IPresenceService, PresenceService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<ICallService, CallService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IAuditLogger, AuditLogger>();
            return services;
        }
        public static IServiceCollection AddSignalRConfiguration(
            this IServiceCollection services,
             AppSettings appSettings)
        {
            var redisConnection = appSettings.Get<RedisSettings>();

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                options.MaximumReceiveMessageSize = 102400; // 100 KB
            })
            .AddStackExchangeRedis(redisConnectionString: redisConnection.ConnectionStrings["presence"], options =>
            {
                options.Configuration.ChannelPrefix = "hubs";
            });

            return services;
        }
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
        public static IServiceCollection RegisterFileDocumentManager(this IServiceCollection services)
        {
            // --- Ensure AppSettings is registered first ---
            var sp = services.BuildServiceProvider();
            var appSettings = sp.GetService<AppSettings>()
                ?? throw new InvalidOperationException("AddAutoConfigurations must be called before AddInfrastructure.");

            var storageProvider = appSettings.Get<FileDocumentSettings>();

            switch (storageProvider.StorageProvider)
            {
                case StorageProvider.azureblob:
                    if (storageProvider.azureBlobStorageSettings == null)
                        throw new InvalidOperationException("Azure Blob Storage settings are not configured.");
                    services.AddSingleton<IFileStorageService>(_ => new AzureBlobStorageService(storageProvider.azureBlobStorageSettings.ConnectionString, storageProvider.azureBlobStorageSettings.ContainerName));
                    break;

                case StorageProvider.local:
                default:
                    var basePath = string.IsNullOrWhiteSpace(storageProvider.PathFolder) ? "wwwroot/uploads" : storageProvider.PathFolder;
                    services.AddSingleton<IFileStorageService>(_ => new LocalStorageService(basePath));
                    break;
            }

            return services;
        }
        public static IServiceCollection AddApplicationMappings(
            this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

            services.AddSingleton<IMapper>(new Mapper(TypeAdapterConfig.GlobalSettings));

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

            var corsDomains = appSettings.Get<CommonConfig>();
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", builder =>
                {
                    if (string.IsNullOrWhiteSpace(corsDomains.CorsDomains))
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .SetIsOriginAllowed(_ => true) 
                            .AllowCredentials();         
                    }
                    else
                    {
                        var allowedOrigins = corsDomains.CorsDomains
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .ToArray();

                        builder
                            .WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                });
            });
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
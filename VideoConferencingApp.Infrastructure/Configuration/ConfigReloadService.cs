using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration
{
    public class ConfigReloadService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly AppSettings _appSettings;
        private readonly IEnumerable<Type> _configTypes;

        public ConfigReloadService(IServiceProvider provider, AppSettings appSettings, IEnumerable<Type> configTypes)
        {
            _provider = provider;
            _appSettings = appSettings;
            _configTypes = configTypes;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var type in _configTypes)
            {
                var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(type);
                var monitor = _provider.GetService(monitorType);
                if (monitor == null) continue;

                var onChangeMethod = monitorType.GetMethod("OnChange");
                var actionType = typeof(Action<>).MakeGenericType(type);

                var action = Delegate.CreateDelegate(actionType, this,
                    typeof(ConfigReloadService).GetMethod(nameof(OnConfigChanged))!.MakeGenericMethod(type));

                onChangeMethod?.Invoke(monitor, new object[] { action });
            }

            return Task.CompletedTask;
        }

        public void OnConfigChanged<T>(T newValue) where T : class, IConfig
        {
            _appSettings.Register(newValue);
            Console.WriteLine($"🔄 {typeof(T).Name} reloaded at {DateTime.Now}");
        }
    }
}

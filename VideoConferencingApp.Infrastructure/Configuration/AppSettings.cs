using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Configuration
{
    public class AppSettings
    {
        private readonly Dictionary<string, IConfig> _configurations;
        private readonly Dictionary<Type, IConfig> _configurationsByType;

        public AppSettings()
        {
            _configurations = new Dictionary<string, IConfig>(StringComparer.OrdinalIgnoreCase);
            _configurationsByType = new Dictionary<Type, IConfig>();
        }

        /// <summary>
        /// Register a configuration instance.
        /// Stores in both section name dictionary and type dictionary.
        /// </summary>
        public void Register<T>(T config) where T : IConfig
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Store by SectionName (allows multiple configs of same type)
            _configurations[config.SectionName] = config;

            // Store by Type (one instance per type)
            _configurationsByType[config.GetType()] = config;
        }

        /// <summary>
        /// Get configuration by type (strongly typed)
        /// </summary>
        public T Get<T>() where T : IConfig
        {
            if (_configurationsByType.TryGetValue(typeof(T), out var config))
            {
                return (T)config;
            }
            throw new KeyNotFoundException($"Configuration of type {typeof(T).Name} not found");
        }

        /// <summary>
        /// Try get configuration by type
        /// </summary>
        public bool TryGet<T>(out T config) where T : IConfig
        {
            if (_configurationsByType.TryGetValue(typeof(T), out var found))
            {
                config = (T)found;
                return true;
            }
            config = default;
            return false;
        }

        /// <summary>
        /// Get configuration by section name
        /// </summary>
        public IConfig Get(string sectionName)
        {
            if (_configurations.TryGetValue(sectionName, out var config))
                return config;

            throw new KeyNotFoundException($"Configuration with section '{sectionName}' not found");
        }

        /// <summary>
        /// Check if a configuration of a given type exists
        /// </summary>
        public bool Contains<T>() where T : IConfig
        {
            return _configurationsByType.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Check if a configuration with given section name exists
        /// </summary>
        public bool Contains(string sectionName)
        {
            return _configurations.ContainsKey(sectionName);
        }

        /// <summary>
        /// Get all configurations
        /// </summary>
        public IEnumerable<IConfig> GetAll()
        {
            return _configurations.Values;
        }

        /// <summary>
        /// Get all configurations of a specific type
        /// </summary>
        public IEnumerable<T> GetAll<T>() where T : IConfig
        {
            return _configurations.Values.OfType<T>();
        }

        /// <summary>
        /// Remove configuration by type
        /// </summary>
        public bool Remove<T>() where T : IConfig
        {
            if (_configurationsByType.TryGetValue(typeof(T), out var config))
            {
                _configurationsByType.Remove(typeof(T));
                _configurations.Remove(config.SectionName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a summary of all registered configurations
        /// </summary>
        public Dictionary<string, string> GetSummary()
        {
            var summary = new Dictionary<string, string>();
            foreach (var kvp in _configurations)
            {
                summary[kvp.Key] = kvp.Value.GetType().Name;
            }
            return summary;
        }
    }

}

using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider
{
    /// <summary>
    /// A static manager that provides easy access to data settings throughout the application.
    //' It should be initialized once at application startup.
    /// </summary>
    public static class DataSettingsManager
    {
        private static bool _isInitialized;
        private static DataSettings _settings = new();

        /// <summary>
        /// Initializes the static manager with the application's configured settings.
        /// This method should be called only once from Program.cs.
        /// </summary>
        /// <param name="settings">The configured data settings object.</param>
        public static void Init(DataSettings settings)
        {
            if (_isInitialized) return;

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _isInitialized = true;
        }

        /// <summary>
        /// Gets the configured database connection string.
        /// </summary>
        public static string GetCurrentConnectionString()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("DataSettingsManager has not been initialized.");

            return _settings.ConnectionString;
        }

        /// <summary>
        /// Gets the configured SQL command timeout.
        /// </summary>
        public static int GetSqlCommandTimeout()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("DataSettingsManager has not been initialized.");

            return _settings.CommandTimeout;
        }
    }
}
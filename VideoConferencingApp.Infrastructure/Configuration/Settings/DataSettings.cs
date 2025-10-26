using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    /// <summary>
    /// Represents the data access settings that are bound from application configuration.
    /// This class is used with the IOptions pattern.
    /// </summary>
    public class DataSettings: IConfig
    {
        /// <summary>
        /// A key for the configuration section.
        /// </summary>
        public  string SectionName => "DatabaseSettings";

        /// <summary>
        /// Gets or sets the database connection string.
        /// This will be populated from the "ConnectionStrings" section.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default command timeout in seconds.
        /// </summary>
        public int CommandTimeout { get; set; } = 30; // Default value
    }
}
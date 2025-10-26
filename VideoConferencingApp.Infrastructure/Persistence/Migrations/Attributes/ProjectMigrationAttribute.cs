using FluentMigrator;
using System.Globalization;

namespace VideoConferencingApp.Infrastructure.Persistence.Migrations.Attributes
{
    /// <summary>
    /// An attribute that automatically converts a date string (like "20231101103000")
    /// into a unique version number for FluentMigrator.
    /// Changing the date string creates a new version.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProjectMigrationAttribute : MigrationAttribute
    {
        /// <param name="dateTimeVersion">
        /// The unique version, represented as a date string in "yyyyMMddHHmmss" format.
        /// To re-run this migration, simply change this string (e.g., increment the last digit).
        /// </param>
        /// <param name="description">A description of what the migration does.</param>
        public ProjectMigrationAttribute(string dateTimeVersion, string description)
            : base(ParseToVersion(dateTimeVersion), description)
        {
        }

        /// <summary>
        /// A helper method to convert the date string into a long integer that FluentMigrator uses.
        /// </summary>
        private static long ParseToVersion(string dateTimeVersion)
        {
            if (string.IsNullOrEmpty(dateTimeVersion))
                throw new ArgumentException("A date/time version string is required.", nameof(dateTimeVersion));

            // This ensures the string is treated as a simple sequence of numbers.
            // For example, "20231101103000" becomes the number 20231101103000.
            if (long.TryParse(dateTimeVersion, out long version))
            {
                return version;
            }

            throw new FormatException($"The dateTimeVersion '{dateTimeVersion}' is not in a valid yyyyMMddHHmmss format and could not be parsed to a long.");
        }
    }
}
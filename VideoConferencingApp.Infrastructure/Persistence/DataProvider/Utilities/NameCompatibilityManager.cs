using System.Reflection;
using VideoConferencingApp.Infrastructure.Persistence.Migrations;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities
{
    public static partial class NameCompatibilityManager
    {
        #region Fields

        private static readonly Dictionary<Type, string> _tableNames = new();
        private static readonly Dictionary<Type, string> _viewNames = new();
        private static readonly Dictionary<Type, string> _databaseNames = new();
        private static readonly Dictionary<(Type, string), string> _columnNames = new();
        private static readonly IList<Type> _loadedFor = new List<Type>();
        private static bool _isInitialized;
        private static readonly ReaderWriterLockSlim _locker = new();

        #endregion

        #region Utilities

        private static void Initialize()
        {
            using (new ReaderWriteLockDisposable(_locker))
            {
                if (_isInitialized)
                    return;

                // Find all INameCompatibility implementations in the current assembly
                var compatibilities = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => typeof(INameCompatibility).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .Select(t => Activator.CreateInstance(t) as INameCompatibility)
                    .ToList();

                foreach (var nameCompatibility in compatibilities.Where(nc => nc != null))
                {
                    if (_loadedFor.Contains(nameCompatibility.GetType()))
                        continue;

                    _loadedFor.Add(nameCompatibility.GetType());

                    foreach (var (key, value) in nameCompatibility.TableNames.Where(pair => !_tableNames.ContainsKey(pair.Key)))
                        _tableNames.Add(key, value);

                    foreach (var (key, value) in nameCompatibility.ViewNames.Where(pair => !_viewNames.ContainsKey(pair.Key)))
                        _viewNames.Add(key, value);

                    foreach (var (key, value) in nameCompatibility.DatabaseNames.Where(pair => !_databaseNames.ContainsKey(pair.Key)))
                        _databaseNames.Add(key, value);

                    foreach (var (key, value) in nameCompatibility.ColumnNames.Where(pair => !_columnNames.ContainsKey(pair.Key)))
                        _columnNames.Add(key, value);
                }

                _isInitialized = true;
            }
        }

        #endregion

        #region Methods

        public static string GetTableName(Type type)
        {
            Initialize();
            return _tableNames.TryGetValue(type, out var value) ? value : type.Name + "s"; // Added 's' for pluralization convention
        }

        public static string GetViewName(Type type)
        {
            Initialize();
            _viewNames.TryGetValue(type, out var viewName);
            return viewName;
        }

        public static string GetDatabaseName(Type type)
        {
            Initialize();
             _databaseNames.TryGetValue(type, out var databaseName);
            return databaseName;
        }

        public static string GetColumnName(Type type, string propertyName)
        {
            Initialize();
            return _columnNames.TryGetValue((type, propertyName), out var value) ? value : propertyName;
        }

        #endregion
    }
}
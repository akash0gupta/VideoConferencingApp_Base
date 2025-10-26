using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Tools;
using System.Data.Common;
using System.Linq.Expressions;
using VideoConferencingApp.Domain.Entities;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider
{
    public abstract partial class BaseDataProvider
    {
        #region Utilities

        protected abstract DbConnection GetInternalDbConnection(string connectionString);

        public virtual DataConnection CreateDataConnection()
        {
            return CreateDataConnection(LinqToDbDataProvider);
        }


        protected virtual DataConnection CreateDataConnection(IDataProvider dataProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProvider);
            var dataConnection = new DataConnection(dataProvider, CreateDbConnection(), LinqToDbMappingConfigurator.GetMappingSchema())
            {
                CommandTimeout = DataSettingsManager.GetSqlCommandTimeout()
            };
            return dataConnection;
        }

        protected virtual DbConnection CreateDbConnection(string? connectionString = null)
        {
            return GetInternalDbConnection(!string.IsNullOrEmpty(connectionString) ? connectionString : GetCurrentConnectionString());
        }

        #endregion

        #region Methods    
        /// <summary>
        /// A helper method to create a new DataContext with all our custom rules applied.
        /// </summary>
        private DataContext CreateConfiguredDataContext()
        {
            var options = new DataOptions()
                .UseConnectionString(LinqToDbDataProvider, GetCurrentConnectionString())
                .UseMappingSchema(LinqToDbMappingConfigurator.GetMappingSchema());

            return new DataContext(options)
            {
                CommandTimeout = DataSettingsManager.GetSqlCommandTimeout()
            };
        }


        public virtual IQueryable<TEntity> GetTable<TEntity>() where TEntity : BaseEntity
        {
            return CreateConfiguredDataContext().GetTable<TEntity>();
        }

        public virtual async Task<TEntity> InsertEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateConfiguredDataContext();
            entity.Id = await dataContext.InsertWithInt64IdentityAsync(entity);
            return entity;
        }

        public virtual async Task<TEntity> InsertWithoutInt64EntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateConfiguredDataContext();
            await dataContext.InsertAsync(entity);
            return entity;
        }


        public virtual async Task UpdateEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateConfiguredDataContext();          
            await dataContext.UpdateAsync(entity);
        }


        public virtual async Task DeleteEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateConfiguredDataContext();
            await dataContext.DeleteAsync(entity);
        }

        public virtual async Task BulkInsertEntitiesAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection(LinqToDbDataProvider);
            await dataContext.BulkCopyAsync(new BulkCopyOptions() { KeepIdentity = true }, entities.RetrieveIdentity(dataContext, useSequenceName: false));
        }

        public virtual async Task<int> BulkDeleteEntitiesAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            using var dataContext = CreateConfiguredDataContext();
            return await dataContext.GetTable<TEntity>()
                .Where(predicate)
                .DeleteAsync();
        }

        /// <summary>
        /// Truncates a database table. This is a destructive operation that removes all rows.
        /// </summary>
        /// <param name="resetIdentity">If true, resets the identity column seed.</param>
        public virtual async Task TruncateAsync<TEntity>(bool resetIdentity = false) where TEntity : BaseEntity
        {
            await using var dataContext = CreateConfiguredDataContext();
            await dataContext.GetTable<TEntity>().TruncateAsync(resetIdentity);
        }

        #region Raw SQL and Stored Procedure Execution

        /// <summary>
        /// Executes a non-query SQL command (e.g., an UPDATE or DELETE statement without a result set)
        /// and returns the number of affected records.
        /// </summary>
        /// <param name="sql">The raw SQL command text.</param>
        /// <param name="dataParameters">The parameters for the SQL command.</param>
        /// <returns>The number of records affected by the command execution.</returns>
        public virtual async Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] dataParameters)
        {
            await using var dataConnection = CreateDataConnection(LinqToDbDataProvider);
            var command = new CommandInfo(dataConnection, sql, dataParameters);
            return await command.ExecuteAsync();
        }

        /// <summary>
        /// Executes a stored procedure that returns a result set (a table).
        /// </summary>
        /// <typeparam name="T">The type to map the result records to.</typeparam>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters for the stored procedure.</param>
        /// <returns>A list of result records of the specified type.</returns>
        public virtual Task<IList<T>> QueryProcAsync<T>(string procedureName, params DataParameter[] parameters)
        {
            using var dataConnection = CreateDataConnection(LinqToDbDataProvider);
            var command = new CommandInfo(dataConnection, procedureName, parameters);

            var rez = command.QueryProc<T>()?.ToList();
            return Task.FromResult<IList<T>>(rez ?? new List<T>());
        }

        /// <summary>
        /// Executes SQL command and returns results as collection of values of specified type
        /// </summary>
        /// <typeparam name="T">Type of result items</typeparam>
        /// <param name="sql">SQL command text</param>
        /// <param name="parameters">Parameters to execute the SQL command</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the collection of values of specified type
        /// </returns>
        public virtual Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
        {
            using var dataContext = CreateDataConnection();
            return Task.FromResult<IList<T>>(dataContext.Query<T>(sql, parameters)?.ToList() ?? new List<T>());
        }
        #endregion


        #region Transaction Management

        /// <summary>
        /// Starts a new database transaction.
        /// The returned DataConnection should be disposed, which will automatically
        /// roll back the transaction if it was not explicitly committed.
        /// </summary>
        /// <returns>A DataConnection with an active transaction.</returns>
        public virtual async Task<DataConnection> BeginTransactionAsync()
        {
            // Create a new connection using our existing logic
            var transactionConnection = CreateDataConnection();

            // Begin a transaction on that connection
            await transactionConnection.BeginTransactionAsync();

            // Return the connection itself. It now holds the transaction state.
            return transactionConnection;
        }

        #endregion


        #endregion

        #region Properties
        protected abstract IDataProvider LinqToDbDataProvider { get; }
        protected static string GetCurrentConnectionString() => DataSettingsManager.GetCurrentConnectionString();
        public string ConfigurationName => LinqToDbDataProvider.Name;
        #endregion
    }
}
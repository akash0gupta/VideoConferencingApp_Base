using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Tools;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider.Repositories;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly BaseDataProvider _dataProvider;
        private DataConnection _dataConnection;
        private DbTransaction _transaction;
        private bool _disposed;
        private readonly List<Func<Task>> _commands;

        public UnitOfWork(BaseDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _commands = new List<Func<Task>>();
        }

        /// <summary>
        /// Gets the current data connection
        /// </summary>
        public DataConnection Connection => GetOrCreateConnection();

        private DataConnection GetOrCreateConnection()
        {
            if (_dataConnection == null)
            {
                _dataConnection = _dataProvider.CreateDataConnection();
            }
            return _dataConnection;
        }

        public async Task<DbTransaction> BeginTransactionAsync()
        {
            var connection = GetOrCreateConnection();

            if (_transaction != null)
            {
                return _transaction;
            }

            await connection.BeginTransactionAsync();
            _transaction = connection.Transaction;
            return _transaction;
        }

        public async Task<int> SaveChangesAsync()
        {
            if (_commands.Count == 0)
            {
                return 0;
            }

            try
            {
                // Execute all queued commands
                foreach (var command in _commands)
                {
                    await command();
                }

                // Commit transaction if exists
                if (_transaction != null)
                {
                    await _dataConnection.CommitTransactionAsync();
                }

                var affectedRecords = _commands.Count;
                _commands.Clear();
                return affectedRecords;
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        public void Commit()
        {
            if (_transaction != null)
            {
                _dataConnection.CommitTransaction();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            if (_transaction != null)
            {
                _dataConnection.RollbackTransaction();
                _transaction = null;
            }
            _commands.Clear();
        }

        #region Repository Methods

        public IQueryable<TEntity> GetTable<TEntity>() where TEntity : BaseEntity
        {
            return GetOrCreateConnection().GetTable<TEntity>();
        }

        public void Insert<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            _commands.Add(async () =>
            {
                await _dataConnection.InsertAsync(entity);
            });
        }

        public void Update<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            _commands.Add(async () =>
            {
                await _dataConnection.UpdateAsync(entity);
            });
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            _commands.Add(async () =>
            {
                await _dataConnection.DeleteAsync(entity);
            });
        }

        public void BulkInsert<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            _commands.Add(async () =>
            {
                await _dataConnection.BulkCopyAsync(
                    new BulkCopyOptions() { KeepIdentity = true },
                    entities.RetrieveIdentity(_dataConnection, useSequenceName: false));
            });
        }

        public void BulkDelete<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            _commands.Add(async () =>
            {
                await _dataConnection.GetTable<TEntity>()
                    .Where(predicate)
                    .DeleteAsync();
            });
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (_transaction != null)
                        {
                            Rollback();
                        }
                    }
                    catch
                    {
                        // Swallow exceptions during dispose
                    }
                    finally
                    {
                        _dataConnection?.Dispose();
                        _dataConnection = null;
                        _transaction = null;
                        _commands.Clear();
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

}

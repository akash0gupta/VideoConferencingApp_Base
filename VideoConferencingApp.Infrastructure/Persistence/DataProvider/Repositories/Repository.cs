using LinqToDB;
using LinqToDB.Data;
using System;
using System.Linq.Expressions;
using VideoConferencingApp.Application.DTOs.Common;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Domain.Models;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider.Repositories
{
    public class Repository<TEntity> :IRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly BaseDataProvider _dataProvider;
        private readonly DataConnection? _transactionConnection;
       
        public Repository(BaseDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        // Constructor for transactional use (called by UnitOfWork)
        public Repository(DataConnection transactionConnection)
        {
            _transactionConnection = transactionConnection;
        }

        public IQueryable<TEntity> Table
        {
            get
            {
                // If this repository was created by UnitOfWork, use the transaction connection.
                if (_transactionConnection != null)
                {
                    return _transactionConnection.GetTable<TEntity>();
                }

                // Otherwise, use the standard provider (non-transactional)
                return _dataProvider.GetTable<TEntity>();
            }
        }

        public async Task<TEntity?> GetByIdAsync(long id)
        {
            return await Table
                .FirstOrDefaultAsync(e => Sql.Property<long>(e, "Id") == id);
                
        }

        public async Task<IList<TEntity>> GetByIdsAsync(long[] ids)
        {
            return await Table
                .Where(e => ids.Contains(Sql.Property<long>(e, "Id")))
                .ToListAsync();
        }

        public async Task<IList<TEntity>> GetAllAsync()
        {
            return await Table.ToListAsync();
        }

        public async Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await Table.Where(predicate).ToListAsync();
        }

        public async Task InsertAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _dataProvider.InsertEntityAsync(entity);
        }

        public async Task InsertRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            await _dataProvider.BulkInsertEntitiesAsync(entities);
        }

        public async Task UpdateAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _dataProvider.UpdateEntityAsync(entity);
        }

        public async Task<int> BulkUpdateAsync(Expression<Func<TEntity, bool>> predicate,Expression<Func<TEntity, TEntity>> updateExpression)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (updateExpression == null) throw new ArgumentNullException(nameof(updateExpression));
            return await Table
                .Where(predicate)
                .UpdateAsync(updateExpression);
        }


        public async Task DeleteAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _dataProvider.DeleteEntityAsync(entity);
        }

        public async Task DeleteByIdAsync(long id)
        {
            if (id == 0) throw new ArgumentNullException(nameof(id));
            var entity = await GetByIdAsync(id);
            if (entity != null)
                await DeleteAsync(entity);
        }

        public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, bool>> predicate)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            await _dataProvider.BulkDeleteEntitiesAsync(predicate);
        }

        public async Task<IPagedList<TEntity>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
        {
            var query = Table;

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            return new PagedList<TEntity>(items, pageIndex, pageSize, totalCount);
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return await Table.CountAsync(predicate);
        }
    }
}

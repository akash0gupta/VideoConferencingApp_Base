using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets the queryable table of entities.
        /// </summary>
        IQueryable<TEntity> Table { get; }

        /// <summary>
        /// Gets an entity by its identifier asynchronously.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>The entity or null if not found.</returns>
        Task<TEntity?> GetByIdAsync(long id);

        /// <summary>
        /// Gets a list of entities by their identifiers asynchronously.
        /// </summary>
        /// <param name="ids">Array of entity identifiers.</param>
        /// <returns>List of entities matching the given identifiers.</returns>
        Task<IList<TEntity>> GetByIdsAsync(long[] ids);

        /// <summary>
        /// Gets all entities asynchronously.
        /// </summary>
        /// <returns>List of all entities.</returns>
        Task<IList<TEntity>> GetAllAsync();

        /// <summary>
        /// Finds entities matching the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <returns>List of entities matching the filter.</returns>
        Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Inserts a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        Task InsertAsync(TEntity entity);

        /// <summary>
        /// Inserts multiple entities asynchronously.
        /// </summary>
        /// <param name="entities">The collection of entities to insert.</param>
        Task InsertRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Updates an existing entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        Task UpdateAsync(TEntity entity);

        /// <summary>
        /// Performs a bulk update on entities matching the predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The filter expression for selecting entities.</param>
        /// <param name="updateExpression">The update expression to apply.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> BulkUpdateAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> updateExpression);

        /// <summary>
        /// Deletes an entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Deletes an entity by its identifier asynchronously.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        Task DeleteByIdAsync(long id);

        /// <summary>
        /// Deletes multiple entities asynchronously based on a predicate.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="predicate">The filter expression for selecting entities to delete.</param>
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Gets a paged list of entities asynchronously with optional filtering and ordering.
        /// </summary>
        /// <param name="pageIndex">The zero-based page index.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="predicate">Optional filter expression.</param>
        /// <param name="orderBy">Optional ordering function.</param>
        /// <returns>Paged list of entities.</returns>
        Task<IPagedList<TEntity>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);
    }
}

namespace VideoConferencingApp.Domain.Interfaces
{
    /// <summary>
    /// Represents a paged list of items
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IPagedList<T> : IList<T>
    {
        /// <summary>
        /// Current page index (0-based)
        /// </summary>
        int PageIndex { get; }

        /// <summary>
        /// Page size
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Total number of items
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// Whether there are previous pages
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// Whether there are next pages
        /// </summary>
        bool HasNextPage { get; }
    }
}


using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.DTOs.Common
{
    public class PagedList<T> : List<T>,IPagedList<T>
    {
        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }

        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex + 1 < TotalPages;

        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            AddRange(source);
        }

        public static PagedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var totalCount = source.Count();
            var items = source.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, pageIndex, pageSize, totalCount);
        }
    }
}

namespace CloseExpAISolution.Application.DTOs.Response
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalResult { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}

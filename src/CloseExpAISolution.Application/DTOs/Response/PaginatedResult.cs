namespace CloseExpAISolution.Application.DTOs.Response
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalResult { get; set; }
        public int Rage { get; set; }
        public int PageSize {  get; set; }

    }
}

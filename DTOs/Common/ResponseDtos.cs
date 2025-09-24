namespace EventManagementAPI.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class PagedResponse<T> : ApiResponse<T>
    {
        public PaginationInfo Pagination { get; set; }
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }

    public class ValidationErrorResponse
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public object AttemptedValue { get; set; }
    }
}
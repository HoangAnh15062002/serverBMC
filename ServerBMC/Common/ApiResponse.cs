namespace ServerBMC.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public class PagedRequest
{
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDesc { get; set; } = false;

    public int Skip => (Math.Max(1, Page ?? 1) - 1) * Math.Clamp(PageSize ?? 20, 1, 200);
    public int Take => Math.Clamp(PageSize ?? 20, 1, 200);
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
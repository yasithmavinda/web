namespace TaskFlow.Application.Common.Models;

/// <summary>Standard API response wrapper — every endpoint returns this shape.</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public object? Errors { get; set; }
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, object? errors = null)
        => new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Created(T data, string message = "Created successfully")
        => new() { Success = true, Message = message, Data = data };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse OkNoData(string message = "Success")
        => new() { Success = true, Message = message };

    public static new ApiResponse Fail(string message, object? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

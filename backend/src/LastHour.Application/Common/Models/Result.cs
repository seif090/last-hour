namespace LastHour.Application.Common.Models;

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedList(IReadOnlyCollection<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}

public class Result<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; } = 200;

    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static Result<T> Failure(string error, int statusCode = 400) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };
}

public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; } = 200;

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(string error, int statusCode = 400) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };
}

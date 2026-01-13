namespace RenuMate.Common;

public sealed record PaginatedResponse<T> (
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
namespace Calcifer.Api.Rbac.DTOs
{
	public record PaginatedResponse<T>(
	List<T> Items, int TotalCount,
	int Page, int PageSize, int TotalPages
);
}

namespace Calcifer.Api.DTOs.Common
{
    public class CommonStatusDto
    {
		public int Id { get; set; }
		public string StatusName { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string Module { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public int SortOrder { get; set; }
	}
}

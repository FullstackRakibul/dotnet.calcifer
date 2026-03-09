namespace Calcifer.Api.DTOs.Common
{
    public class CommonStatusResponseDto
    {
        public int Id { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class CommonStatusRequestDto
    {
        public string StatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}

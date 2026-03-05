namespace Calcifer.Api.DTOs
{
    public class ClientTypeDto
    {
		// Request DTOs
		public class ClientTypeCreateRequest
		{
			public string CTypeName { get; set; }
			public string? Remarks { get; set; }
		}

		public class ClientTypeUpdateRequest
		{
			public string CTypeName { get; set; }
			public string? Remarks { get; set; }
		}
	}
}

namespace Calcifer.Api.DbContexts.Common
{
    public class TableOperationDetails : CommonStatus
    {
		public int Id { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; } = DateTime.Now;
        public string? DeletedBy { get; set; }
		public bool IsDeleted { get; set; }
		public int StatusId { get; set; }
		public CommonStatus Status { get; set; }
	}
}

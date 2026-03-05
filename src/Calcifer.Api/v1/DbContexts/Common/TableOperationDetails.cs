namespace Calcifer.Api.DbContexts.Common
{
    public class TableOperationDetails
    {
		public int StatusId { get; set; } 
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(6);
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public bool IsDeleted { get; set; }

    }
}

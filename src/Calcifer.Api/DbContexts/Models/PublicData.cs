using System.ComponentModel.DataAnnotations;

namespace Calcifer.Api.DbContexts.Models
{
    public class PublicData
    {
        [Key]
        public Guid Guid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

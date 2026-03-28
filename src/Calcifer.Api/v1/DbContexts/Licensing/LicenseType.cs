using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.Licensing
{
    [Table("LicenseTypes")]
    public class LicenseType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public int DurationDays { get; set; }

        public int DefaultMaxUsers { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }
    }
}

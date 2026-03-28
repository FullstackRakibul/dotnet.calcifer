using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.Licensing
{
    [Table("LicenseFeatures")]
    public class LicenseFeature
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int LicenseId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FeatureCode { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsEnabled { get; set; } = true;

        [ForeignKey("LicenseId")]
        public License License { get; set; } = null!;
    }
}

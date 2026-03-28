using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.Licensing
{
    [Table("LicenseActivations")]
    public class LicenseActivation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int LicenseId { get; set; }

        [MaxLength(100)]
        public string? MachineId { get; set; }

        [MaxLength(100)]
        public string? ActivatedByUserId { get; set; }

        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeactivatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("LicenseId")]
        public License License { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.Common
{
    [Table("CommonStatus")]
    public class CommonStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string StatusName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Module { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }
    }
}

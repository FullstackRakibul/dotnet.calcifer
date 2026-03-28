using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calcifer.Api.DbContexts.AuthModels
{
    public class ApplicationUser : IdentityUser
    {
        public Guid AppUserGuid { get; set; } = Guid.NewGuid();
        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string? Region { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int Status { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
   
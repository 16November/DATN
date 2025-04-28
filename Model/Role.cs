using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class Role : IdentityRole<Guid>
    {
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        // 1 Role có nhiều Users
        public ICollection<User>? Users { get; set; }
    }
}

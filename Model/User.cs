using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Model
{
    public class User : IdentityUser<Guid>
    {
        [Key]
        public Guid UserId { 
            get => Id;
            set => Id = value; }

        public UserInfo? UserInfo { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class UserInfo
    {
        [Key]
        public Guid UserInfoId { get; set; }

        [Required]
        public string MSSV { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public virtual User? User { get; set; }
    }
}

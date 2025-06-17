using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Request
{
    public class ChangePasswordRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
        public string Password { get; set; } =string.Empty;
    }
}

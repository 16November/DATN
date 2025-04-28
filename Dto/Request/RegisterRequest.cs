using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Request
{
    public class RegisterRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}

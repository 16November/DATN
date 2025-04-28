using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Request
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}

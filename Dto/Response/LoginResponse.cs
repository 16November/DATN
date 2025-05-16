namespace DoAnTotNghiep.Dto.Response
{
    public class LoginResponse
    {
        public string JwtToken { get; set; } = string.Empty;
        
        public string Role { get; set; } = string.Empty;

        public Guid UserId { get; set; } 
    }
}

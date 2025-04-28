namespace DoAnTotNghiep.Dto.Response
{
    public class LoginResponse
    {
        public string JwtToken { get; set; } = string.Empty;
        
        public string MSSV { get; set; } = string.Empty;

        public Guid UserId { get; set; } 
    }
}

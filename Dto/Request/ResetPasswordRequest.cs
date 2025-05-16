namespace DoAnTotNghiep.Dto.Request
{
    public class ResetPasswordRequest
    {
        public string UserName { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }
    }
}

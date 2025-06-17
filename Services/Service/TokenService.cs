using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.IService;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace DoAnTotNghiep.Service.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration configuration;
        public TokenService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public string CreateJwtToken(User user, List<string> roles)
        {
            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, user.UserName!));

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken
                (
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> SendEmail(string email, string subject, string body)
        {
            try
            {
                var emailSend = configuration.GetValue<string>("Email_Configuration:Email");
                var password = configuration.GetValue<string>("Password_Configuration:Password");
                var host = configuration.GetValue<string>("Host_Configuration:Host");
                var port = configuration.GetValue<int>("Port_Configuration:Port");

                var stmp = new SmtpClient(host, port);
                stmp.EnableSsl = true;
                stmp.UseDefaultCredentials = false;
                stmp.Credentials = new NetworkCredential(emailSend, password);

                var message = new MailMessage()
                {
                    From = new MailAddress(emailSend),
                    Subject = subject,
                    Body = body
                };
                message.To.Add(new MailAddress(email));
                await stmp.SendMailAsync(message);

                // Nếu không có lỗi, trả về true
                return true;
            }
            catch (Exception ex)
            {
                // Ghi lại thông tin lỗi để hỗ trợ debugging
                Console.WriteLine($"Error sending email: {ex.Message}");
                // Trả về false khi có lỗi
                return false;
            }
        }

    }
}

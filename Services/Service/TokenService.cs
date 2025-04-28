using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.IService;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

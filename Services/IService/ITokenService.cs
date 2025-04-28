using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface ITokenService
    {
        string CreateJwtToken(User user, List<string> roles);
    }
}

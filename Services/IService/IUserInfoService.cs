using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface IUserInfoService
    {
        Task AddUserInfo(UserInfo userInfo);

        Task UpdateUserInfo(Guid userId,UserInfo userInfo);

        Task DeleteUserInfo(Guid userId);

        Task<UserInfo> GetDetailUserInfo(Guid userId);
    }
}

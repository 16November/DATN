using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IUserInfoRepository
    {
        Task AddUserInfoAsync(UserInfo userInfo);

        Task UpdateUserInfoAsync(Guid userId, UserInfo userInfoUpdate);

        Task DeleteUserInfoASync(Guid userInfoId);

        Task<UserInfo> GetDetailUserInfoAsync(Guid userId);

        Task SaveChangesAsync();
    }
}

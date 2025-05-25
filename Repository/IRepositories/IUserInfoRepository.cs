using DoAnTotNghiep.Dto.Request;
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

        Task<List<UserInfo>> GetListUserInfo(List<RequestUserToExam> request);

        Task<UserInfo> GetUserInfo(RequestUserToExam request);
    }
}

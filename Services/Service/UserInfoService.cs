using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Services.IService;
using SequentialGuid;

namespace DoAnTotNghiep.Services.Service
{
    public class UserInfoService : IUserInfoService
    {
        private readonly IUserInfoRepository userInfoRepository;
        


        public UserInfoService(IUserInfoRepository userInfoRepository)
        {
            this.userInfoRepository = userInfoRepository;
            
        }

        public async Task AddUserInfo(UserInfo userInfo)
        {
            userInfo.UserInfoId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();
            await userInfoRepository.AddUserInfoAsync(userInfo);    
            await userInfoRepository.SaveChangesAsync();
        }

        public async Task UpdateUserInfo(Guid userId, UserInfo userInfo)
        {
            await userInfoRepository.UpdateUserInfoAsync(userId, userInfo);
            await userInfoRepository.SaveChangesAsync();
        }

        public async Task DeleteUserInfo(Guid userId)
        {
            await userInfoRepository.DeleteUserInfoASync(userId);
            await userInfoRepository.SaveChangesAsync();
        }

        public async Task<UserInfo> GetDetailUserInfo(Guid userId)
        {
            var userInfo = await userInfoRepository.GetDetailUserInfoAsync(userId);
            return userInfo;
        }
    }
}

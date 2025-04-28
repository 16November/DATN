using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Services.IService;

namespace DoAnTotNghiep.Services.Service
{
    public class UserInfoService : IUserInfoService
    {
        private readonly IUserInfoRepository userInfoRepository;
        private readonly DataContext dataContext;
        private readonly IMapper mapper;


        public UserInfoService(IUserInfoRepository userInfoRepository, DataContext dataContext
            ,IMapper mapper)
        {
            this.userInfoRepository = userInfoRepository;
            this.dataContext = dataContext;
            this.mapper = mapper;
        }

        public async Task AddUserInfo(UserInfo userInfo)
        {
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

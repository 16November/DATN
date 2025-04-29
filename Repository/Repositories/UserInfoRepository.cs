using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace DoAnTotNghiep.Repository.Repositories
{
    public class UserInfoRepository : IUserInfoRepository
    {
        private readonly DataContext dataContext;

        public UserInfoRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task AddUserInfoAsync(UserInfo userInfo)
        {
            await dataContext.UserInfos.AddAsync(userInfo);
        }

        public async Task UpdateUserInfoAsync(Guid userId, UserInfo userInfoUpdate)
        {
            var userInfo = await dataContext.UserInfos.FirstOrDefaultAsync(x => x.UserId == userId);

            if (userInfo == null)
            {
                throw new KeyNotFoundException("Not found UserInfo");
            }

            userInfo.MSSV = userInfoUpdate.MSSV;
            userInfo.FullName = userInfoUpdate.FullName;

        }

        public async Task DeleteUserInfoASync(Guid userInfoId)
        {
            var userInfo = await dataContext.UserInfos.FirstOrDefaultAsync(x => x.UserInfoId == userInfoId);

            if (userInfo == null)
            {
                throw new KeyNotFoundException("Not found data about ");
            }

            dataContext.Remove(userInfo);

        }

        public async Task<UserInfo> GetDetailUserInfoAsync(Guid userId)
        {
            var userInfo = await dataContext.UserInfos.FirstOrDefaultAsync(x => x.UserId == userId);

            if (userInfo == null)
            {
                throw new KeyNotFoundException("Not found UserInfo");
            }

            return userInfo;
        }
        public async Task SaveChangesAsync()
        {
            await dataContext.SaveChangesAsync();
        }
    }
}
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Linq;

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
            userInfo.Email = userInfoUpdate.Email;

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

        public async Task<List<UserInfo>> GetListUserInfo(List<RequestUserToExam> request)
        {
            var mssvList = request.Select(x => x.MSSV).ToList();
            var nameList = request.Select(x => x.FullName).ToList();
            var userInfos = await dataContext.UserInfos
                            .Where(x => mssvList.Contains(x.MSSV) && nameList.Contains(x.FullName))
                            .ToListAsync();
            if (userInfos.Count == 0)
            {
                throw new KeyNotFoundException("Not found UserInfo");
            }
            return userInfos;
        }

        public async Task<UserInfo> GetUserInfo(RequestUserToExam request)
        {
            var userInfo = await dataContext.UserInfos
                            .FirstOrDefaultAsync(x => x.MSSV == request.MSSV && x.FullName == request.FullName);
            if (userInfo == null)
            {
                throw new KeyNotFoundException("Not found UserInfo");
            }
            return userInfo;
        }
    }
}
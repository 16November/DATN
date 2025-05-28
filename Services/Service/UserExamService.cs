using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Repository.Repositories;
using DoAnTotNghiep.Services.IService;
using Microsoft.Extensions.Logging.Console;
using NetTopologySuite.IO;
using SequentialGuid;
using System.Threading.Tasks.Dataflow;

namespace DoAnTotNghiep.Services.Service
{
    public class UserExamService : IUserExamService
    {
        private readonly IUserExamRepository userExamRepository;
        private readonly IUserInfoRepository userInfoRepository;
        private readonly DataContext dataContext;
        private readonly IMapper mapper;

        public UserExamService(IUserExamRepository userExamRepository , IMapper mapper,
            IUserInfoRepository userInfoRepository,DataContext dataContext)
        {
            this.dataContext = dataContext;
            this.userInfoRepository = userInfoRepository;
            this.userExamRepository = userExamRepository;
            this.mapper = mapper;   
        }

        public async Task AddUserToExamById(UserExam userExam)
        {
            userExam.UserExamId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();

            await userExamRepository.AddUserToExamById(userExam);
            await userExamRepository.SaveChangesAsync();
        }

        public async Task DeleteUserExam(Guid examId, Guid userId)
        {
            await userExamRepository.DeleteUserFromExam(examId, userId);
            await userExamRepository.SaveChangesAsync();
        }

        public async Task UpdateStatus(Guid userId,Guid examId , bool IsStarted)
        {
            await userExamRepository.UpdateStatusAsync(userId, examId, IsStarted);
            await userExamRepository.SaveChangesAsync();
        }

        public async Task<double> UpdateSubmitedById(Guid examId, Guid userId)
        {
            var score = await userExamRepository.UpdateSubmitedById(examId, userId);
            await userExamRepository.SaveChangesAsync();
            return score;
        }

        public async Task<UserExamDto> GetDetailUserExam(Guid userExamId)
        {
            var userExam = await GetDetailUserExam(userExamId);

            var userExamDto = mapper.Map<UserExamDto>(userExam);

            return userExamDto;

        }

        public async Task<List<StudentExam>> GetListUserExam (Guid examId)
        {
            var userExams = await userExamRepository.GetListUserExam(examId);

            

            return userExams;
        }

        public async Task AddListUserToExam(List<RequestUserToExam> request, Guid examId)
        {
            var userInfos = await userInfoRepository.GetListUserInfo(request);
            var studentDict = userInfos.ToDictionary(x => x.MSSV);
            var userExams = new List<UserExam>();

            foreach(var item in request)
            {
                if(studentDict.TryGetValue(item.MSSV,out var userInfo))
                {
                    var userExam = new UserExam()
                    {
                        UserExamId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid(),
                        ExamId = examId,
                        UserId = userInfo.UserId,
                        IsStarted = false,
                        IsSubmitted = false,

                    };
                    userExams.Add(userExam);
                }
            }

            using var transacstion = await dataContext.Database.BeginTransactionAsync();
            try
            {
                await userExamRepository.AddListUserToExam(userExams);
                await transacstion.CommitAsync();
            }
            catch(Exception)
            {
                await transacstion.RollbackAsync();
            }
        }

        public async Task AddUserToExam(RequestUserToExam request, Guid examId)
        {
            var userInfo = await userInfoRepository.GetUserInfo(request);
            
            var userExam = new UserExam()
            {
                UserExamId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid(),
                ExamId = examId,
                UserId = userInfo.UserId,
                IsStarted = false,
                IsSubmitted = false,
            };

            await userExamRepository.AddUserToExamById(userExam);

            await userExamRepository.SaveChangesAsync();
        }

        public async Task<List<StudentExamInfo>> GetListStudentByExamId(Guid examId)
        {
            var studentList = await userExamRepository.getListStudent(examId);
            return studentList;
        }

    }
}

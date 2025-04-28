using AutoMapper;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Repository.Repositories;
using SequentialGuid;

namespace DoAnTotNghiep.Services.Service
{
    public class UserExamService
    {
        private readonly IUserExamRepository userExamRepository;
        private readonly IMapper mapper;

        public UserExamService(IUserExamRepository userExamRepository , IMapper mapper)
        {
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

        public async Task UpdateSubmitedById(Guid examId, Guid userId)
        {
            await userExamRepository.UpdateSubmitedById(examId, userId);
            await userExamRepository.SaveChangesAsync();
        }

        public async Task<UserExamDto> GetDetailUserExam(Guid userExamId)
        {
            var userExam = await GetDetailUserExam(userExamId);

            var userExamDto = mapper.Map<UserExamDto>(userExam);

            return userExamDto;

        }

        public async Task<List<UserExamDto>> GetListUserExam (Guid examId)
        {
            var userExams = await userExamRepository.GetListUserExam(examId);

            var userExamsDto = mapper.Map<List<UserExamDto>>(userExams);

            return userExamsDto;
        }

    }
}

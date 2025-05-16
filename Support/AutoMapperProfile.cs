using AutoMapper;
using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Support
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() 
        {
            //Exam
            CreateMap<RequestExam, Exam>().ReverseMap();
            CreateMap<Exam, ExamDto>().ReverseMap();

            //Question
            CreateMap<RequestQuestion,Question>()
                    .ForMember(dest => dest.Answers, opt => opt.Ignore());
            CreateMap<Question, QuestionDto>()
                .ForMember(dest => dest.AnswersDto,opt => opt.MapFrom(src => src.Answers));
            CreateMap<Question, QuestionUserDto>()
                .ForMember(dest => dest.AnswersUserDto, opt => opt.MapFrom(src => src.Answers));

            //Answer
            CreateMap<Answer, RequestAnswer>().ReverseMap();
            CreateMap<Answer, AnswerDto>().ReverseMap();
            CreateMap<Answer, AnswerUserDto>().ReverseMap();

            //UserAnswer
            CreateMap<UserAnswer, UserAnswerDto>().
                ForMember(dest => dest.AnswerDto, opt => opt.MapFrom(src => src.Answer));

            //UserExam
            CreateMap<UserExam, UserExamDto>()
                .ForMember(dest => dest.userInfoDto , opt => opt.MapFrom(src=> src.User!.UserInfo));

        }
    }
}

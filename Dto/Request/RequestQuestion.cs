using DoAnTotNghiep.Model;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Request
{
    public class RequestQuestion
    {
        public Guid QuestionId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public Guid ExamId { get; set; }

        public List<RequestAnswer>? Answers { get; set; }
    }
}

using DoAnTotNghiep.Model;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Response
{
    public class ExamDto
    {
        [Key]
        public Guid ExamId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; }

        [Range(1, 1000)]
        public int DurationInMinutes { get; set; } = 60; // mặc định 60 phút

        public bool IsPublished { get; set; } = false;

        public DateTime StartDay { get; set; }

        public int CountOfQuestions     { get; set; }
        public List<QuestionDto>? QuestionsDto { get; set; }
    }
}

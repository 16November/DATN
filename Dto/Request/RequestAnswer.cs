using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Request
{
    public class RequestAnswer
    {
        public Guid AnswerId  { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

        public Guid QuestionId { get; set; }
    }
}

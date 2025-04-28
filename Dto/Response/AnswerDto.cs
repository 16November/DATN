using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Response
{
    public class AnswerDto
    {
        public Guid AnswerId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

    }
}

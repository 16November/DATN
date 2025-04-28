using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Response
{
    public class AnswerUserDto
    {
        public Guid AnswerId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}

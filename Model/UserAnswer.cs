using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class UserAnswer
    {
        [Key]
        public Guid UserAnswerId { get; set; }

        public Guid AnswerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid UserId  { get; set; }

        public Guid QuestionId  { get; set; }

        public Question? Question { get; set; }


        public User? User { get; set; }

        public Answer? Answer { get; set; }
    }
}

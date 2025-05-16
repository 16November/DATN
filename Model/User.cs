using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Model
{
    public class User : IdentityUser<Guid>
    {
        [Key]
        public Guid UserId
        {
            get => Id;
            set => Id = value;
        }

        public virtual UserInfo? UserInfo { get; set; }
        public virtual ICollection<UserExam> UserExams { get; set; } = new List<UserExam>();
        public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
    }
}

using DoAnTotNghiep.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace DoAnTotNghiep.Data
{
    public class DataContext: IdentityDbContext<User,Role, Guid>
    {
        public DataContext(DbContextOptions<DataContext> dbContextOptions) : base(dbContextOptions) 
        { 
        }

        public DbSet<Exam> Exams { get; set; }  

        public DbSet<Question> Questions { get; set; }

        public DbSet<Answer> Answers { get; set; }

        public DbSet<UserAnswer> UserAnswers { get; set; }

        public DbSet<UserInfo> UserInfos { get; set; }

        public DbSet<UserExam> UserExams { get; set; }

        public DbSet<User> AppUsers {  get; set; }

        public DbSet<Role> AppRoles { get; set; }

    }
}

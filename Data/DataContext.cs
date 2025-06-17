using DoAnTotNghiep.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace DoAnTotNghiep.Data
{
    public class DataContext : IdentityDbContext<User, Role, Guid>
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

        public DbSet<User> AppUsers { get; set; }

        public DbSet<Role> AppRoles { get; set; }

        public DbSet<CheatingEvent> CheatingEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customize Identity table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            // User - UserInfo (1-1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserInfo)
                .WithOne(ui => ui.User)
                .HasForeignKey<UserInfo>(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Exam - Questions (1-n)
            modelBuilder.Entity<Exam>()
                .HasMany(e => e.Questions)
                .WithOne(q => q.Exam)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question - Answers (1-n)
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserAnswer relationships
            modelBuilder.Entity<UserAnswer>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserAnswers)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAnswer>()
                .HasOne(ua => ua.Question)
                .WithMany(q => q.UserAnswers)
                .HasForeignKey(ua => ua.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAnswer>()
                .HasOne(ua => ua.Answer)
                .WithMany(a => a.UserAnswers)
                .HasForeignKey(ua => ua.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserExam configuration
            modelBuilder.Entity<UserExam>()
                .HasKey(ue => ue.UserExamId);

            modelBuilder.Entity<UserExam>()
                .HasIndex(ue => new { ue.UserId, ue.ExamId })
                .IsUnique();

            modelBuilder.Entity<UserExam>()
                .HasOne(ue => ue.User)
                .WithMany(u => u.UserExams)
                .HasForeignKey(ue => ue.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserExam>()
                .HasOne(ue => ue.Exam)
                .WithMany(e => e.UserExams)
                .HasForeignKey(ue => ue.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Exam - CreatedBy
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Role - Users
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Restrict);

            // Additional configurations
            modelBuilder.Entity<UserAnswer>()
                .Property(ua => ua.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Exam>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Exam>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }



    }
}

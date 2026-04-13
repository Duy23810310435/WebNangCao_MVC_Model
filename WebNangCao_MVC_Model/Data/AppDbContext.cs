using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using WebNangCao_MVC_Model.Data.Fluent_API; // Import namespace chứa UserConfiguration
using WebNangCao_MVC_Model.Models;
namespace WebNangCao_MVC_Model.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        // --- KHAI BÁO CÁC BẢNG (TABLES) ---
        //Đang xem trên Code trên Mermaid Diagram
        public DbSet<User> Users { get; set; } // Tương ứng với bảng "Users" trong Database
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<ExamResultDetail> ExamResultDetails { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);

            // Set khóa chính kép cho bảng UserGroup
            // bảng UserGroup giúp liên kết nhiều-nhiều giữa User và Group, nên cần khóa chính kép để đảm bảo tính duy nhất của mỗi cặp UserId-GroupId
            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            // Cấu hình liên kết: 1 User có nhiều UserGroup
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId);

            // Cấu hình liên kết: 1 Group có nhiều UserGroup
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);


            //// CÁCH 1: Kích hoạt thủ công từng file trong thư mục FluentAPI (Dùng cách này để dễ kiểm soát)
            //modelBuilder.ApplyConfiguration(new UserConfiguration());

            //// CÁCH 2: Kích hoạt TẤT CẢ file cấu hình trong project (Nhanh, gọn)
            //// modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            ///
            // Dòng thần thánh này sẽ tự động tìm tất cả các class kế thừa IEntityTypeConfiguration
            // trong assembly hiện tại (bao gồm 3 file trong thư mục Fluent_API) và áp dụng chúng.
            modelBuilder.Entity<Exam>()
                .HasMany(e => e.Questions)
                .WithMany(q => q.Exams)
                .UsingEntity<Dictionary<string, object>>(
                    "ExamQuestion", // Phải khớp với tên bảng trung gian dưới PostgreSQL
                    j => j.HasOne<Question>().WithMany().HasForeignKey("QuestionId"),
                    j => j.HasOne<Exam>().WithMany().HasForeignKey("ExamId")
                );
        }
    }
}

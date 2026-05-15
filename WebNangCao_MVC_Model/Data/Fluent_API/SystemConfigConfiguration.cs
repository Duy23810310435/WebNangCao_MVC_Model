using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebNangCao_MVC_Model.Models; // Import thư mục Models vào

namespace WebNangCao_MVC_Model.Data.Fluent_API
{
    public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
    {
        public void Configure(EntityTypeBuilder<SystemConfig> builder)
        {
            // 1. Tên bảng và Khóa chính
            builder.ToTable("SystemConfigs");
            builder.HasKey(x => x.Id);

            // 2. Ép kiểu và giới hạn độ dài (Chuẩn hóa Database)
            builder.Property(x => x.SystemName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.SystemUrl).HasMaxLength(200);
            builder.Property(x => x.DefaultLanguage).HasMaxLength(50);
            builder.Property(x => x.Timezone).HasMaxLength(100);
            
            builder.Property(x => x.EmailProvider).HasMaxLength(50);
            builder.Property(x => x.SmtpHost).HasMaxLength(100);
            builder.Property(x => x.SmtpUser).HasMaxLength(100);
            builder.Property(x => x.SmtpPassword).HasMaxLength(255);

            // 3. Khóa ngoại truy vết
            // (Đại ca biết chỉ có 1 Admin, nhưng cứ để đây cho chuẩn form, lỡ sau này hệ thống phình to)
            builder.HasOne(x => x.UpdatedByUser)
                   .WithMany()
                   .HasForeignKey(x => x.UpdatedByUserId)
                   .OnDelete(DeleteBehavior.SetNull);

            // =======================================================
            // 4. BÍ KÍP SEED DATA: Tự động đẻ ra 1 dòng cấu hình mặc định
            // Khi chạy Update-Database, nó sẽ bơm luôn cục này vào DB
            // =======================================================
            builder.HasData(new SystemConfig
            {
                Id = 1,
                SystemName = "EduTest Pro",
                SystemUrl = "https://edutest.pro",
                DefaultLanguage = "vi-VN",
                Timezone = "Asia/Ho_Chi_Minh",
                
                EnableEmailNotification = true,
                EnableSmsNotification = false,
                EnablePushNotification = true,
                
                EmailProvider = "SMTP",
                SmtpHost = "smtp.gmail.com",
                SmtpPort = 587,
                SmtpUser = "noreply@edutest.pro",
                SmtpPassword = "", // Pass để rỗng, lúc nào Admin vào web thì gõ sau
                
                SessionTimeoutMinutes = 30,
                MinPasswordLength = 8,
                MaxFailedLogins = 5,
                Require2FA = false,
                ForcePasswordChange90Days = false,
                BlockUnknownIps = false,
                LogAllActivities = true,
                
                UpdatedAt = new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc),
                UpdatedByUserId = 1 // Vì em bảo chỉ có 1 Admin, ta giả định luôn ID của ông Vua này là 1
            });
        }
    }
}
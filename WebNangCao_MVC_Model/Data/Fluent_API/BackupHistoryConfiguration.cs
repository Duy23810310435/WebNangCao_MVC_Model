using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebNangCao_MVC_Model.Models;

namespace WebNangCao_MVC_Model.Data.Fluent_API
{
    public class BackupHistoryConfiguration : IEntityTypeConfiguration<BackupHistory>
    {
        public void Configure(EntityTypeBuilder<BackupHistory> builder)
        {
            builder.ToTable("BackupHistories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ErrorMessage).HasColumnType("text"); // Cho phép lưu dài thoải mái

            // Khóa ngoại: Ai thực hiện Backup
            builder.HasOne(x => x.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedByUserId)
                   .OnDelete(DeleteBehavior.SetNull); 
        }
    }
}
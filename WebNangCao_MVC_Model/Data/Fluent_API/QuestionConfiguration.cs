using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebNangCao_MVC_Model.Models;

namespace WebNangCao_MVC_Model.Data.Fluent_API
{
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("Questions");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Content)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(q => q.Difficulty)
                   .IsRequired()
                   .HasMaxLength(50);

            //builder.Property(q => q.ExamId)
            //       .IsRequired();

            //// Index cho FK
            //builder.HasIndex(q => q.ExamId);

            //// Quan hệ 1-N: Exam -> Questions
            //builder.HasOne(q => q.Exam)
            //       .WithMany(e => e.Questions)
            //       .HasForeignKey(q => q.ExamId)
            //       .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace WebNangCao_MVC_Model.Models
{
    public class Exam
    {
        public int Id { get; set; } // <--- Đảm bảo bạn có dòng này và viết đúng chữ Id
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;  // ADD THIS
        public bool IsActive { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }
        [Column("GroupId")]
        public int IdGroup { get; set; }

        // Bổ sung thêm dòng này để EF Core hiểu IdGroup dùng để móc nối với bảng Group
        [ForeignKey("IdGroup")]
        public Group? Group { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        // ... các thuộc tính khác
    }
}

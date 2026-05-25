using System.ComponentModel.DataAnnotations.Schema;

namespace WebNangCao_MVC_Model.Models
{
    public class Exam
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        
        public string Difficulty { get; set; } = "Trung bình"; // Gán mặc định là Trung bình cho các record cũ
        public bool IsActive { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        // ---> BỔ SUNG CHO ADMIN: Vẽ biểu đồ "Đề thi mới trong tuần"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

        // ---> BỔ SUNG CHO ADMIN: Điểm chuẩn để biết Học sinh đỗ hay trượt
        public double PassingScore { get; set; } = 5.0;
        public int Duration { get; set; }
        [Column("GroupId")]
        public int? IdGroup { get; set; }

        // Bổ sung thêm dòng này để EF Core hiểu IdGroup dùng để móc nối với bảng Group
        [ForeignKey("IdGroup")]
        public Group? Group { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        // ... các thuộc tính khác
        //do Thí sinh cũng có lựa chọn tạo đề thi cá nhân mà không cần phải tham gia vào một nhóm nào cả nên sẽ có thể có StudentId null
        public int? StudentId { get; set; }
        //phân biệt đề do GV tạo ra hay đề do SV tự tạo ra, nếu IsSelfCreated = true thì đây là đề do SV tự tạo, nếu IsSelfCreated = false thì đây là đề do GV giao
        public bool IsSelfCreated { get; set; } = false;

    }
}

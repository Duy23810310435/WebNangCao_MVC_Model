namespace WebNangCao_MVC_Model.Models
{
    public class ExamResult
    {
        public int Id { get; set; }

        // Cần khớp kiểu dữ liệu với Id của bảng User/Student (int hoặc string)
        public int StudentId { get; set; }

        public int ExamId { get; set; }
        public double Score { get; set; } // Điểm số
        public DateTime SubmitTime { get; set; }
        // ---> BỔ SUNG CHO ADMIN: Giám sát thời gian làm bài
        public DateTime StartTime { get; set; } // Lúc sinh viên bấm nút "Bắt đầu làm bài"
        
        // Thời gian thực tế (Giây). Thằng EF Core tính sẵn lưu vào đây cho Admin Query cho lẹ!
        public int TimeTakenSeconds { get; set; } 
        
        // ---> BỔ SUNG CHO ADMIN: Thống kê Tỷ lệ Đỗ/Trượt (Chỉ cần Query cột này là ra biểu đồ)
        public bool IsPassed { get; set; } 

        // ---> BỔ SUNG CHO ADMIN: Hệ thống chống gian lận (Anti-Cheat)
        public int TabSwitchCount { get; set; } = 0; // Học sinh nhảy sang tab khác tra Google mấy lần?
        public string IpAddress { get; set; } = string.Empty; // Lưu IP để bắt quả tang thi hộ!

        public User Student { get; set; }
        public Exam Exam { get; set; }
        public virtual ICollection<ExamResultDetail> ExamResultDetails { get; set; } = new List<ExamResultDetail>();
        // ... các thuộc tính khác
    }
}

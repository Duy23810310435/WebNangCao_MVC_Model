namespace WebNangCao_MVC_Model.ViewModels
{
    // Class tổng chứa toàn bộ dữ liệu của trang Dashboard của sinh viên
    public class StudentDashboardViewModel
    {
        // 4 ô thống kê dữ liệu động của giao diện Thí Sinh
        public int TotalExams { get; set; }
        public double AverageScore { get; set; }
        public int CompletedExams { get; set; }
        public int UpcomingExams { get; set; }

        // Danh sách bài thi hiển thị ra màn hình
        public List<ExamItemViewModel> Exams { get; set; } = new List<ExamItemViewModel>();
        public List<JoinedClassViewModel> JoinedClasses { get; set; }
    }

    // Class đại diện cho 1 thẻ bài thi trên màn hình
    public class ExamItemViewModel

    {
        public string DisplayStatus => Status switch
        {
            "Có thể làm" => "Có thể làm",
            "Sắp tới" => "Sắp tới",
            "Đã hoàn thành" => "Đã hoàn thành",
            _ => "Khác"
        };

        // Hàm tính toán màu sắc cho progress bar (có thể dùng trong View hoặc Helper)
        public string GetScoreColor => ScorePercentage >= 50 ? "#22c55e" : "#ef4444";
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty; // "Có thể làm", "Sắp tới", "Đã hoàn thành"

        // Các thuộc tính phụ (nếu Database có thì map vào, không thì để trống)
        public string SubjectName { get; set; } = "Môn học chung";
        public string Difficulty { get; set; } = "Cơ bản";
        public string GroupName { get; set; } = "Lớp của tôi";
        public int? IdGroup { get; set; }
        public DateTime EndTime { get; set; }  // Add this if needed
        public int SubmittedCount { get; set; }  // Add this for instructor dashboard

        // --- BỔ SUNG CÁC THUỘC TÍNH MỚI DÀNH CHO THANH TIẾN ĐỘ & ĐIỂM SỐ ---
        public int CompletedQuestions { get; set; }
        public bool IsResuming { get; set; }
        public double ScorePercentage { get; set; }
    }

    //Class đại diện cho 1 thẻ Lớp trong các nhóm lớp
    public class JoinedClassViewModel
    {
        public int IdGroup { get; set; }
        public string ClassName { get; set; } = string.Empty; //tên lớp
        public string TeacherName { get; set; } = string.Empty; //giảng viên của lớp
        public int ExamCount { get; set; } // Tổng số bài thi trong lớp này
    }
}
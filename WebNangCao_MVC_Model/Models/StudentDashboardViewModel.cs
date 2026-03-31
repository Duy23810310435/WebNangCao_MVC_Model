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
    }

    // Class đại diện cho 1 thẻ bài thi trên màn hình
    public class ExamItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty; // "Có thể làm", "Sắp tới", "Đã hoàn thành"

        // Các thuộc tính phụ (nếu Database của bạn có thì map vào, không thì để trống)
        public string SubjectName { get; set; } = "Môn học chung";
        public string Difficulty { get; set; } = "Cơ bản";
        public string GroupName { get; set; } = "Lớp của tôi";
        public int? IdGroup { get; set; }
        public DateTime EndTime { get; set; }  // Add this if needed
        public int SubmittedCount { get; set; }  // Add this for instructor dashboard
    }
}
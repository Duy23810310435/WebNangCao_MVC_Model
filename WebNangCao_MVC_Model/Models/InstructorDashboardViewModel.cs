namespace WebNangCao_MVC_Model.ViewModels
{
    public class InstructorDashboardViewModel
    {
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalExams { get; set; }
        public int CompletedExams { get; set; }
        public double AverageStudentScore { get; set; }
        public double PassRate { get; set; } // TỈ LỆ ĐỖ (%)
        public int ExamsThisWeek { get; set; } // BÀI THI TUẦN NÀY
        public int PendingExams { get; set; } // SỐ BÀI ĐANG CHỜ CHẤM

        public List<ClassItemViewModel> Classes { get; set; } = new List<ClassItemViewModel>();
        public List<ExamItemViewModel> RecentExams { get; set; } = new List<ExamItemViewModel>();
        public List<RecentGradingViewModel> RecentGradings { get; set; } = new List<RecentGradingViewModel>();
    }

    public class ClassItemViewModel
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class ExamManagementViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int IdGroup { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public int CompletedCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class ExamCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public int IdGroup { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ClassCreateViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ExamResultSummaryViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public List<StudentExamResultViewModel> StudentResults { get; set; } = new List<StudentExamResultViewModel>();
    }

    public class StudentExamResultViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public double Score { get; set; }
        public DateTime SubmitTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RecentGradingViewModel
    {
        public int ResultId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public double? Score { get; set; } // Null nếu chưa chấm
        public string SubmitTimeAgo { get; set; } = string.Empty; // VD: "2 giờ trước"
        public string Status { get; set; } = string.Empty; // "graded" hoặc "pending"
    }
}
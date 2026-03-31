using System.ComponentModel.DataAnnotations;

namespace WebNangCao_MVC_Model.ViewModels
{
    public class InstructorDashboardViewModel
    {
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; } = 0;
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
        public int TotalStudents { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "draft"; // draft, scheduled, active, paused, completed
        public string? Description { get; set; }
        public double? AverageScore { get; set; }
        public ExamDifficultyViewModel? Difficulty { get; set; }
        public ExamSettingsViewModel? Settings { get; set; }
    }

    public class ExamDifficultyViewModel
    {
        public int Easy { get; set; }
        public int Medium { get; set; }
        public int Hard { get; set; }
    }

    public class ExamSettingsViewModel
    {
        public bool ShuffleQuestions { get; set; }
        public bool ShuffleAnswers { get; set; }
        public bool ShowResultAfter { get; set; }
        public bool AllowReview { get; set; }
        public bool StrictMode { get; set; }
        public int MaxAttempts { get; set; } = 1;
    }

    public class ExamCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public int IdGroup { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; } = true;
        public List<ExamQuestionViewModel> Questions { get; set; } = new List<ExamQuestionViewModel>();
    }

    public class ExamQuestionViewModel
    {
        public string Content { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "medium";
        public List<ExamAnswerViewModel> Answers { get; set; } = new List<ExamAnswerViewModel>();
        public int CorrectAnswerIndex { get; set; } = 0;
    }

    public class ExamAnswerViewModel
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
    }

    public class ClassCreateViewModel
    {
        [Required(ErrorMessage = "Tên lớp học không được để trống")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên lớp học phải từ 3 đến 100 ký tự")]
        public string GroupName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả không được để trống")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 500 ký tự")]
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

    public class ExamSubmissionViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string SubmittedAt { get; set; } = string.Empty;
        public double Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int TimeTaken { get; set; }
        public string Status { get; set; } = "graded";
    }
}
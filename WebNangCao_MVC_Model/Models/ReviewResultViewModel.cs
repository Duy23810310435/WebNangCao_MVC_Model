namespace WebNangCao_MVC_Model.ViewModels 
{
    public class ReviewResultViewModel
    {
        public int ResultId { get; set; }
        public double Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<ReviewQuestionViewModel> Questions { get; set; } = new List<ReviewQuestionViewModel>();
        public int TestId { get; set; }
        public bool IsSelfCreated { get; set; } // Phân biệt: true = SV tự tạo, false = GV giao;
    }

    public class ReviewQuestionViewModel
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = string.Empty; // Nội dung câu hỏi
        public int? SelectedAnswerId { get; set; } // Đáp án sinh viên đã chọn (có thể null nếu bỏ trống)
        public bool IsCorrect { get; set; } // Câu này làm đúng hay sai
        public List<ReviewAnswerViewModel> Answers { get; set; } = new List<ReviewAnswerViewModel>();
    }

    public class ReviewAnswerViewModel
    {
        public int AnswerId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrectAnswer { get; set; } // Đây có phải đáp án đúng của hệ thống không
    }
}
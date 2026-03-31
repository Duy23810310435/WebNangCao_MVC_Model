using System.Collections.Generic;

namespace WebNangCao_MVC_Model.Models
{
    public class Question
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        // Foreign key
        //public int ExamId { get; set; }

        // Navigation property
        //public Exam? Exam { get; set; }

        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
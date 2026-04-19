//using System;

//namespace WebNangCao_MVC_Model.ViewModels
//{
//    public class ExamDisplayViewModel
//    {
//        // --- CÁC THÔNG TIN CƠ BẢN CỦA BÀI THI ---
//        public int Id { get; set; } // Hoặc string tuỳ vào database của bạn
//        public string Title { get; set; }
//        public string SubjectName { get; set; }
//        public string Status { get; set; }
//        public string Difficulty { get; set; }
//        public DateTime StartTime { get; set; }
//        public int Duration { get; set; }
//        public int TotalQuestions { get; set; }

//        // --- THÔNG TIN LỚP HỌC ---
//        public string GroupName { get; set; }
//        public string IdGroup { get; set; } // Hoặc int tuỳ kiểu dữ liệu mã lớp

//        // --- CÁC THUỘC TÍNH BỔ SUNG CHO THANH TIẾN ĐỘ & ĐIỂM SỐ ---
//        public int CompletedQuestions { get; set; }
//        public bool IsResuming { get; set; }
//        public double ScorePercentage { get; set; }
//    }
//}
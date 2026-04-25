namespace WebNangCao_MVC_Model.Models
{
    public class AdminDashboradViewModel
    {
        public int TotalActiveUsers {get; set;}
        public int TotalExams {get; set;}
        public int TotalExamsToday {get; set;}
        // Ép PostgreSQL tạo cột kiểu Decimal có tổng cộng 5 chữ số, trong đó 2 chữ số ở phần thập phân (VD: 100.00 hoặc 99.99)
        //Muốn hiển thị thông tin dưới dạng 88,88% (Kiểu vậy)
        public decimal SystemHealth { get; set; } = 0.00m;
        //CHỈ SỐ XU HƯỚNG 7 NGÀY (4 thẻ Phân tích hệ thống)
        public int WeeklyLogins { get; set; }
        public decimal WeeklyLoginsGrowth { get; set; } // VD: 12.0m (+12%)

        public int WeeklyExamsTaken { get; set; }
        public decimal WeeklyExamsTakenGrowth { get; set; } // VD: 8.0m (+8%)

        public int WeeklyNewExams { get; set; }
        public decimal WeeklyNewExamsGrowth { get; set; } // VD: 15.0m (+15%)

        public decimal WeeklyActiveRate { get; set; } // VD: 94.0m (94%)
        public decimal WeeklyActiveRateGrowth { get; set; } // VD: 3.0m (+3%)
        // 3. DANH SÁCH NHẬT KÝ HOẠT ĐỘNG (Activity Logs)
        public List<ActivityLog> RecentLogs { get; set; } = new List<ActivityLog>();
        // 2. DANH SÁCH NHỮNG NGƯỜI VỪA ĐĂNG KÝ (Cần duyệt)
        public List<User> PendingUsers { get; set; } = new List<User>();
    }
}
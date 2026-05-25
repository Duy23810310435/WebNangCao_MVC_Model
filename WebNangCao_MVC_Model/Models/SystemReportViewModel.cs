namespace WebNangCao_MVC_Model.ViewModels
{
    // 1. CÁI BÁT NHỎ: Chứa dữ liệu cho ĐÚNG 1 CỘT trong biểu đồ (VD: Dữ liệu của ngày Thứ 2)
    public class DailyActivityStat
    {
        public string DayName { get; set; } = string.Empty; // "T2", "T3", "T4"...
        public int LoginsCount { get; set; } // Độ dài thanh màu xanh dương
        public int ExamsCount { get; set; }  // Độ dài thanh màu xanh lá
        public double AvgOnlineHours { get; set; } // Con số 4.2h ở tuốt lùi bên phải
    }

    // 2. CÁI MÂM LỚN: Đại diện cho toàn bộ màn hình "Báo cáo chi tiết"
    public class SystemReportViewModel
    {
        // ==============================================
        // 4 THẺ TỔNG QUAN TRÊN CÙNG
        // ==============================================
        public int TotalUsers { get; set; }
        public int TotalExams { get; set; }
        public int TotalCompletedExams { get; set; }
        public double AvgOnlineTime { get; set; } // Hiển thị 4.5h

        // ==============================================
        // BIỂU ĐỒ HOẠT ĐỘNG TRONG TUẦN
        // Đây là một List chứa 7 cái "bát nhỏ" DailyActivityStat (Từ T2 đến CN)
        // ==============================================
        public List<DailyActivityStat> WeeklyChartData { get; set; } = new List<DailyActivityStat>();
    }
}
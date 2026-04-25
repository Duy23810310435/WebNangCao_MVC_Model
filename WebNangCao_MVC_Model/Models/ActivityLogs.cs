namespace WebNangCao_MVC_Model.Models
{
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        public int Id { get; set; }
        //Khoá ngoại
        public int UserId { get; set; }
        //Tạo liên kết (truy vấn) tới bảng User
        public User User { get; set; } = null!;
        public string ActionType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
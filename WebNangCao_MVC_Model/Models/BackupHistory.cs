namespace WebNangCao_MVC_Model.Models
{
    public class BackupHistory
    {
        public int Id { get; set; }
        
        // Tên file sao lưu (VD: backup_edutest_20260425_1530.sql)
        public string FileName { get; set; } = string.Empty;
        
        // Đường dẫn lưu file trên Server (VD: /var/backups/db/...)
        public string FilePath { get; set; } = string.Empty;
        
        // Dung lượng file để báo cáo (VD: 45000 KB)
        public long FileSizeKB { get; set; } 
        
        // Trạng thái: "Success", "Failed", "In Progress"
        public string Status { get; set; } = "Success"; 
        
        // Nếu lỗi thì ghi chú vào đây (VD: "Không đủ dung lượng ổ cứng")
        public string ErrorMessage { get; set; } = string.Empty;

        // Thời gian thực hiện
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Truy vết: Ai là người bấm nút Backup? (Hoặc null nếu hệ thống tự động chạy lúc 2h sáng)
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
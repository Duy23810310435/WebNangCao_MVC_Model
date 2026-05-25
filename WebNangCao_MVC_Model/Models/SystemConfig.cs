namespace WebNangCao_MVC_Model.Models 
{
    public class SystemConfig
    {
        public int Id { get; set; }

        // ==========================================
        // TAB 1: CHUNG
        // ==========================================
        public string SystemName { get; set; } = "EduTest Pro";
        public string SystemUrl { get; set; } = "https://edutest.pro";
        public string DefaultLanguage { get; set; } = "vi-VN";
        public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
        
        public bool EnableEmailNotification { get; set; } = true;
        public bool EnableSmsNotification { get; set; } = false;
        public bool EnablePushNotification { get; set; } = true;

        // ==========================================
        // TAB 2: EMAIL
        // ==========================================
        public string EmailProvider { get; set; } = "SMTP";
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = "noreply@edutest.pro";
        public string SmtpPassword { get; set; } = string.Empty; 

        // ==========================================
        // TAB 3: BẢO MẬT
        // ==========================================
        public int SessionTimeoutMinutes { get; set; } = 30; 
        public int MinPasswordLength { get; set; } = 8; 
        public int MaxFailedLogins { get; set; } = 5; 
        public bool Require2FA { get; set; } = false; 
        public bool ForcePasswordChange90Days { get; set; } = false; 
        public bool BlockUnknownIps { get; set; } = false; 
        public bool LogAllActivities { get; set; } = true; 

        // ==========================================
        // TRACKING (Chỉ 1 Admin vẫn nên giữ để chuẩn hóa DB)
        // ==========================================
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedByUserId { get; set; } 
        public User? UpdatedByUser { get; set; }
    }
}
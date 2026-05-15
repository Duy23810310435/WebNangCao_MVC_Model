namespace WebNangCao_MVC_Model.Models 
{
    public class SystemConfigDto
    {
        // Nhận dữ liệu Tab Chung
        public string SystemName { get; set; }
        public string SystemUrl { get; set; }
        public string DefaultLanguage { get; set; }
        public string Timezone { get; set; }
        public bool EnableEmailNotification { get; set; }
        public bool EnableSmsNotification { get; set; }
        public bool EnablePushNotification { get; set; }

        // Nhận dữ liệu Tab Email
        public string EmailProvider { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }

        // Nhận dữ liệu Tab Bảo mật
        public int SessionTimeoutMinutes { get; set; }
        public int MinPasswordLength { get; set; }
        public int MaxFailedLogins { get; set; }
        public bool Require2FA { get; set; }
        public bool ForcePasswordChange90Days { get; set; }
        public bool BlockUnknownIps { get; set; }
        public bool LogAllActivities { get; set; }
    }
}
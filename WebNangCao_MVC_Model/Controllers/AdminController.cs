using Microsoft.EntityFrameworkCore;
using WebNangCao_MVC_Model.Data;
using WebNangCao_MVC_Model.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace WebNangCao_MVC_Model.Controllers
{
    //Phân quyền (chỉ có admin mới được vào phần này)
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        //Chìa khoá nội bộ của công ti Admin, chỉ có Admin được quyền truy cập
        private readonly AppDbContext _context;
        //DI (Dependancy Injection) để chọc thẳng vào database
        public AdminController(AppDbContext context) {
            _context = context;
        }
        //Action hiển thị màn hình Dashboard
        public async Task<IActionResult> Index() {
            var viewModel = new AdminDashboardViewModel();
            //Múc dữ liệu lên Dashboard
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-7);
            //Đếm user đang hoạt động
            viewModel.TotalActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            //Đếm tổng số đề thi
            viewModel.TotalExams = await _context.Exams.CountAsync();
            //Đếm đê thi tạo trong hôm nay
            viewModel.TotalExamsToday = await _context.Exams.CountAsync(e => e.CreatedAt >= today);
            // 1. Khai báo danh sách các Role em muốn lọc
var targetRoles = new[] { "student", "teacher" };
// 1. Lấy danh sách 5 người ĐANG CHỜ DUYỆT (Để Admin bấm nút "Duyệt")
var pendingUsersTask = await _context.Users
    .AsNoTracking() // Dặn EF Core: "Lấy lên để xem thôi, đừng theo dõi tao!"
    .Where(u => !u.IsActive && targetRoles.Contains(u.Role))
    .OrderByDescending(u => u.CreatedAt)
    .Select(u => new  // Chỉ gắp đúng 5 miếng thịt này, xương xẩu (PasswordHash) bỏ lại
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    })
    .Take(5)
    .ToListAsync();

// 2. Lấy danh sách 5 người ĐANG HOẠT ĐỘNG (Để Admin theo dõi)
// Chỗ này ta nên tạo thêm một biến List<User> ActiveUsers trong ViewModel nhé
viewModel.ActiveUsers = await _context.Users
    .Where(u => targetRoles.Contains(u.Role) && u.IsActive == true)
    .OrderByDescending(u => u.LastLoginAt) // CỰC KỲ QUAN TRỌNG: Ai vừa mới online thì hiện lên đầu
    .Take(5)
    .ToListAsync();
    // Lấy 10 log hoạt động gần nhất
    viewModel.RecentLogs = await _context.ActivityLogs
                .Include(l => l.User) 
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();
    viewModel.SystemHealthStatus = new SystemStatusDto
            {
                IsWebServerOnline = true, // Code chạy đến đây tức là Server đang sống
                IsDatabaseOnline = await _context.Database.CanConnectAsync(), // Ping thẳng xuống Postgres
                StorageUsagePercentage = 65.5m, // Cái này tạm hardcode, thực tế phải đọc từ DriveInfo
                BackupStatus = "Active"
            };

            // (Các chỉ số % tăng trưởng Weekly tạm thời để số 0 hoặc Hardcode, 
            // ta sẽ viết 1 hàm riêng xử lý logic tính toán phức tạp đó sau để Controller không bị rác)
            viewModel.WeeklyLogins = 120;
            viewModel.WeeklyLoginsGrowth = 12.5m;

            // 3. BƯNG MÂM RA CHO KHÁCH (View)
            return View(viewModel);
        }
        [AllowAnonymous]
        [HttpGet("api/Admin/DashboardStats")]
        public async Task<IActionResult> GetDashboardStatsForApp()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var targetRoles = new[] { "student", "teacher" };

                // Múc dữ liệu (Code y hệt bên Web, cực kỳ nhàn)
                var totalActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
                var totalExams = await _context.Exams.CountAsync();
                var totalExamsToday = await _context.Exams.CountAsync(e => e.CreatedAt >= today);

                var pendingUsers = await _context.Users
                    .Where(u => targetRoles.Contains(u.Role) && u.IsActive == false)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.CreatedAt }) // CẮT BỚT DATA THỪA!
                    .ToListAsync();

                var activeUsers = await _context.Users
                    .Where(u => targetRoles.Contains(u.Role) && u.IsActive == true)
                    .OrderByDescending(u => u.LastLoginAt)
                    .Take(5)
                    .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.LastLoginAt }) // CẮT BỚT DATA THỪA!
                    .ToListAsync();

                // ⚠️ LƯU Ý TỬ HUYỆT (JSON OBJECT CYCLE): 
                // Khi Include User vào Log, nếu để nguyên Model bắn ra JSON rất dễ bị lỗi vòng lặp.
                // Giải pháp: Dùng Select để tạo Object ẩn danh (Anonymous Object) chỉ lấy đúng thứ React Native cần!
                var recentLogs = await _context.ActivityLogs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(10)
                    .Select(l => new 
                    {
                        l.Id, // Giả sử model em có trường này (VD: "Đăng nhập", "Nộp bài")
                        l.Timestamp,
                        UserName = l.User != null ? l.User.FullName : "System" // Lôi đúng cái tên ra thôi
                    })
                    .ToListAsync();

                var systemHealth = new 
                {
                    IsWebServerOnline = true,
                    IsDatabaseOnline = await _context.Database.CanConnectAsync(),
                    StorageUsagePercentage = 65.5m,
                    BackupStatus = "Active"
                };

                // BƯNG MÂM JSON RA CHO AXIOS NHẬN HÀNG!
                return Ok(new
                {
                    success = true,
                    message = "Lấy dữ liệu Dashboard thành công!",
                    data = new 
                    {
                        stats = new {
                            totalActiveUsers = totalActiveUsers,
                            totalExams = totalExams,
                            totalExamsToday = totalExamsToday,
                            weeklyLogins = 120,
                            weeklyLoginsGrowth = 12.5m
                        },
                        users = new {
                            pending = pendingUsers,
                            active = activeUsers
                        },
                        logs = recentLogs,
                        health = systemHealth
                    }
                });
            }
            catch (Exception ex)
            {
                // Bắt lỗi cực mạnh, lỡ Database ngỏm thì React Native vẫn nhận được thông báo lỗi tử tế
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }
        [HttpPost("api/Admin/UpdateSystemConfig")]
public async Task<IActionResult> UpdateSystemConfig([FromBody] SystemConfigDto model)
{
    try
    {
        // 1. Tìm cấu hình hiện tại (Mặc định hệ thống chỉ có 1 dòng cấu hình duy nhất)
        var config = await _context.SystemConfigs.FirstOrDefaultAsync();
        
        if (config == null) 
        {
            config = new SystemConfig();
            _context.SystemConfigs.Add(config);
        }

        // 2. Sang tên đổi chủ từ DTO sang Model thật
        // Tab Chung
        config.SystemName = model.SystemName;
        config.SystemUrl = model.SystemUrl;
        config.DefaultLanguage = model.DefaultLanguage;
        config.Timezone = model.Timezone;
        config.EnableEmailNotification = model.EnableEmailNotification;
        config.EnableSmsNotification = model.EnableSmsNotification;
        config.EnablePushNotification = model.EnablePushNotification;

        // Tab Email
        config.EmailProvider = model.EmailProvider;
        config.SmtpHost = model.SmtpHost;
        config.SmtpPort = model.SmtpPort;
        config.SmtpUser = model.SmtpUser;
        if (!string.IsNullOrEmpty(model.SmtpPassword)) {
            config.SmtpPassword = model.SmtpPassword; // Chỉ cập nhật nếu người dùng có gõ pass mới
        }

        // Tab Bảo mật
        config.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
        config.MinPasswordLength = model.MinPasswordLength;
        config.MaxFailedLogins = model.MaxFailedLogins;
        config.Require2FA = model.Require2FA;
        config.ForcePasswordChange90Days = model.ForcePasswordChange90Days;
        config.BlockUnknownIps = model.BlockUnknownIps;
        config.LogAllActivities = model.LogAllActivities;

        // 3. Đánh dấu thời gian & Lưu lại
        config.UpdatedAt = DateTime.UtcNow;
        // config.UpdatedByUserId = ... (Nếu em có làm xác thực đăng nhập thì lấy ID gán vào đây)

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã lưu toàn bộ cấu hình hệ thống!" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
    }
}
    }
}
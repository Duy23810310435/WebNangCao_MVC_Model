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
    // Phân quyền (chỉ có admin mới được vào phần này)
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // Chìa khoá nội bộ của công ti Admin, chỉ có Admin được quyền truy cập
        private readonly AppDbContext _context;
        
        // DI (Dependancy Injection) để chọc thẳng vào database
        public AdminController(AppDbContext context) 
        {
            _context = context;
        }

        // =========================================================
        // 1. TRẢ VỀ GIAO DIỆN DASHBOARD CHO ADMIN TRÊN WEB
        // =========================================================
        public async Task<IActionResult> Index() 
        {
            var viewModel = new AdminDashboardViewModel();
            var today = DateTime.UtcNow.Date;
            var targetRoles = new[] { "student", "instructor" };

            // Đếm các chỉ số tổng quan
            viewModel.TotalActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            viewModel.TotalExams = await _context.Exams.CountAsync();
            viewModel.TotalExamsToday = await _context.Exams.CountAsync(e => e.CreatedAt >= today);

            // 1. ĐỔ DATA CHO DANH SÁCH CHỜ DUYỆT (Gán thẳng vào viewModel.PendingUsers)
            viewModel.PendingUsers = await _context.Users
                .Where(u => !u.IsActive && targetRoles.Contains(u.Role))
                .OrderByDescending(u => u.CreatedAt) // Mới nhất lên đầu
                .Take(5)
                .ToListAsync();

            // 2. ĐỔ DATA CHO DANH SÁCH ĐANG HOẠT ĐỘNG (Gán thẳng vào viewModel.ActiveUsers)
            viewModel.ActiveUsers = await _context.Users
    // Ép Role trong DB biến thành chữ thường hết để so sánh cho chắc cốp!
    .Where(u => u.Role != null && targetRoles.Contains(u.Role.ToLower().Trim()) && u.IsActive == true)
    .OrderByDescending(u => u.CreatedAt)
    .Take(5)
    .ToListAsync();

            // 3. ĐỔ DATA CHO LOG HOẠT ĐỘNG
            viewModel.RecentLogs = await _context.ActivityLogs
                .Include(l => l.User) 
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();

            // 4. Sức khỏe hệ thống
            viewModel.SystemHealthStatus = new SystemStatusDto
            {
                IsWebServerOnline = true, 
                IsDatabaseOnline = await _context.Database.CanConnectAsync(), 
                StorageUsagePercentage = 65.5m, 
                BackupStatus = "Active"
            };

            viewModel.WeeklyLogins = 120;
            viewModel.WeeklyLoginsGrowth = 12.5m;
            // 🔴 MÚC CẤU HÌNH TỪ DB LÊN (Nếu rỗng thì tạo nháp 1 cái để khỏi báo lỗi null)
            viewModel.CurrentConfig = await _context.SystemConfigs.FirstOrDefaultAsync() ?? new SystemConfig();

            return View(viewModel); // Bưng mâm Data đã nhồi đầy ắp ra cho View xơi!
        }

        // =========================================================
        // 2. API CUNG CẤP DATA CHO MOBILE APP (REACT NATIVE)
        // =========================================================
        [AllowAnonymous] // Cho phép App gọi không cần Cookie của Web
        [HttpGet("api/Admin/DashboardStats")]
        public async Task<IActionResult> GetDashboardStatsForApp()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var targetRoles = new[] { "student", "teacher" };

                var totalActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
                var totalExams = await _context.Exams.CountAsync();
                var totalExamsToday = await _context.Exams.CountAsync(e => e.CreatedAt >= today);

                var pendingUsers = await _context.Users
                    .Where(u => targetRoles.Contains(u.Role) && u.IsActive == false)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.CreatedAt }) 
                    .ToListAsync();

                var activeUsers = await _context.Users
                    .Where(u => targetRoles.Contains(u.Role) && u.IsActive == true)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.LastLoginAt }) 
                    .ToListAsync();

                // Lọc dữ liệu chống vòng lặp JSON
                var recentLogs = await _context.ActivityLogs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(10)
                    .Select(l => new 
                    {
                        l.Id, 
                        l.Timestamp,
                        UserName = l.User != null ? l.User.FullName : "System" 
                    })
                    .ToListAsync();

                var systemHealth = new 
                {
                    IsWebServerOnline = true,
                    IsDatabaseOnline = await _context.Database.CanConnectAsync(),
                    StorageUsagePercentage = 65.5m,
                    BackupStatus = "Active"
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy dữ liệu Dashboard thành công!",
                    data = new 
                    {
                        stats = new { totalActiveUsers, totalExams, totalExamsToday, weeklyLogins = 120, weeklyLoginsGrowth = 12.5m },
                        users = new { pending = pendingUsers, active = activeUsers },
                        logs = recentLogs,
                        health = systemHealth
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }

        // =========================================================
        // 3. API NHẬN FETCH TỪ FRONTEND ĐỂ LƯU CẤU HÌNH VÀO DB
        // =========================================================
        [HttpPost("api/Admin/UpdateSystemConfig")]
        // Đã sửa SystemConfigDto thành SystemConfig để map trực tiếp vào Model của DB
        public async Task<IActionResult> UpdateSystemConfig([FromBody] SystemConfig model)
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

                // 2. Sang tên đổi chủ từ gói JSON (model) sang DB (config)
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
                    config.SmtpPassword = model.SmtpPassword; 
                }

                // Tab Bảo mật (CÁI NÀY LOGIN SẼ LẤY RA XÀI NGAY LẬP TỨC NÈ)
                config.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
                config.MinPasswordLength = model.MinPasswordLength;
                config.MaxFailedLogins = model.MaxFailedLogins;
                config.Require2FA = model.Require2FA;
                config.ForcePasswordChange90Days = model.ForcePasswordChange90Days;
                config.BlockUnknownIps = model.BlockUnknownIps;
                config.LogAllActivities = model.LogAllActivities;

                // 3. Đánh dấu thời gian & Lưu lại
                config.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // 4. Trả về cho JS hiện thông báo xanh lá cây
                return Ok(new { success = true, message = "Đã lưu toàn bộ cấu hình hệ thống!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }
        // 1. TẠO CÁI HỘP ĐỂ HỨNG DATA TỪ MODAL GỬI LÊN
        public class AddUserDto
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public string Password { get; set; }
        }

        // 2. HÀM API XỬ LÝ LƯU NGƯỜI DÙNG VÀO DB
        [HttpPost("api/Admin/AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserDto model)
        {
            try
            {
                // Kiểm tra xem Email đã bị ai dùng chưa?
                bool isExist = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (isExist)
                {
                    return BadRequest(new { success = false, message = "Email này đã tồn tại trong hệ thống!" });
                }

                // Chế tạo Username tự động từ Email (VD: nguyen.a@gmail -> nguyen.a)
                string generatedUsername = model.Email.Split('@')[0];

                // Băm mật khẩu (Dùng BCrypt y hệt bên Register)
                string hashPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Khởi tạo thực thể User mới
                var newUser = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Username = generatedUsername,
                    PasswordHash = hashPassword,
                    Role = model.Role, // "student" hoặc "teacher"
                    IsActive = true, // Vì là Admin tự tay thêm nên cho duyệt luôn (Active = true)
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã thêm người dùng mới thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }
        // =========================================================
        // 4. API DUYỆT TÀI KHOẢN (ĐỔI TRẠNG THÁI ISACTIVE)
        // =========================================================
        [HttpPost("api/Admin/ApproveUser/{id}")]
        public async Task<IActionResult> ApproveUser(int id) 
        {
            try
            {
                // 1. Lôi cổ thằng user từ DB lên
                var user = await _context.Users.FindAsync(id);
                
                if (user == null) 
                {
                    return NotFound(new { success = false, message = "Không tìm thấy người dùng này!" });
                }

                if (user.IsActive)
                {
                    return BadRequest(new { success = false, message = "Tài khoản này đã được duyệt từ trước rồi!" });
                }

                // 2. Kích hoạt bùa phép
                user.IsActive = true;
                // user.UpdatedAt = DateTime.UtcNow; // (Bỏ comment nếu bảng User của em có cột này)

                // 3. Lưu xuống Database
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Đã duyệt thành công tài khoản: {user.FullName}!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }
        // =========================================================
        // 5. API XÓA NGƯỜI DÙNG (DELETE)
        // =========================================================
        [HttpDelete("api/Admin/DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy người dùng này!" });
                }

                // Chặn không cho Admin tự xóa chính mình (Tránh trường hợp tự bóp dái)
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == id.ToString())
                {
                    return BadRequest(new { success = false, message = "Không thể tự tiễn bản thân ra chuồng gà được!" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã xóa vĩnh viễn tài khoản khỏi hệ thống!" });
            }
            catch (Exception ex)
            {
                // Lỗi này thường do vướng khóa ngoại (Foreign Key) nếu User đã làm bài thi
                return StatusCode(500, new { success = false, message = "Lỗi Server (Có thể do vướng dữ liệu liên quan): " + ex.Message });
            }
        }
        // =========================================================
        // 6. API SỬA NGƯỜI DÙNG (CẬP NHẬT)
        // =========================================================
        public class EditUserDto
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
        }

        // Nhịp 1: API lấy thông tin cũ của người dùng ném ra Form
        [HttpGet("api/Admin/GetUser/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy người dùng!" });

            return Ok(new { 
                success = true, 
                data = new { user.Id, user.FullName, user.Email, user.Role, user.IsActive } 
            });
        }

        // Nhịp 2: API hứng cục Data mới để lưu đè xuống DB
        [HttpPost("api/Admin/EditUser")]
        public async Task<IActionResult> EditUser([FromBody] EditUserDto model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.Id);
                if (user == null) return NotFound(new { success = false, message = "Tài khoản không tồn tại!" });

                // Kiểm tra xem đổi Email có bị trùng với thằng khác không?
                if (user.Email != model.Email)
                {
                    bool isExist = await _context.Users.AnyAsync(u => u.Email == model.Email);
                    if (isExist) return BadRequest(new { success = false, message = "Email này đã có người khác sử dụng!" });
                }

                // Cập nhật thông tin
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Role = model.Role;
                user.IsActive = model.IsActive; // Admin có quyền khóa/mở khóa ở đây luôn

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }
        // =========================================================
        // 7. API SAO LƯU DỮ LIỆU (UC-08: BACKUP)
        // =========================================================
        [HttpGet("api/Admin/BackupData")]
        public async Task<IActionResult> BackupData()
        {
            try
            {
                // 1. Múc toàn bộ data quan trọng từ Database lên
                // Lưu ý: Dùng AsNoTracking() để EF Core đọc cho nhanh, không cần theo dõi trạng thái
                var users = await _context.Users.AsNoTracking().ToListAsync();
                var configs = await _context.SystemConfigs.AsNoTracking().ToListAsync();
                var activityLogs = await _context.ActivityLogs.AsNoTracking().ToListAsync();
                
                // Nếu em có thêm bảng Exams (Đề thi), Groups (Lớp học) thì múc nốt lên đây
                // var exams = await _context.Exams.AsNoTracking().ToListAsync();

                // 2. Gói tất cả vào một cái hộp to
                var backupPackage = new
                {
                    BackupDate = DateTime.UtcNow,
                    Version = "1.0",
                    Data = new
                    {
                        Users = users,
                        Configs = configs,
                        Logs = activityLogs
                        // Exams = exams
                    }
                };

                // 3. Nén cái hộp đó thành chuỗi JSON
                var jsonString = System.Text.Json.JsonSerializer.Serialize(backupPackage, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true // Format cho file JSON thụt lề đẹp mắt, dễ đọc
                });

                // 4. Biến chuỗi JSON thành cục Bytes để tải về
                var fileBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                string fileName = $"EduTest_Backup_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.json";

                // 5. Trả file về cho trình duyệt tự tải xuống!
                return File(fileBytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi khi sao lưu dữ liệu: " + ex.Message);
            }
        }
        public class DatabaseConfigDto
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string DbName { get; set; }
            public string User { get; set; }
        }
        [HttpPost("api/Admin/TestDbConnection")]
public async Task<IActionResult> TestDbConnection([FromBody] DatabaseConfigDto model)
{
    // Tạo chuỗi kết nối giả định từ dữ liệu Admin vừa nhập
    string testConnString = $"Host={model.Host};Port={model.Port};Database={model.DbName};Username={model.User};Password=TỰ_NHẬP_PASS";
    
    try {
        using var connection = new Npgsql.NpgsqlConnection(testConnString);
        await connection.OpenAsync();
        return Ok(new { success = true, message = "Kết nối đến Database mới thành công!" });
    }
    catch (Exception ex) {
        return BadRequest(new { success = false, message = "Kết nối thất bại: " + ex.Message });
    }
}
    }
}
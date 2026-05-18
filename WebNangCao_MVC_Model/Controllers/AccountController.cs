using FluentValidation;
using FluentValidation.Results; // Dùng để hứng kết quả trả về từ Validator
using Microsoft.AspNetCore.Mvc;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.Data; //để gọi AppDBContext
using System.Linq; //Để dùng FirstOrDefault, Any
using System.Security.Claims; //Dùng để lấy thông tin UserId từ Claims khi đã đăng nhập
using Microsoft.AspNetCore.Authentication; //Dùng để gọi SignInAsync, SignOutAsync khi đăng nhập/đăng xuất
using Microsoft.AspNetCore.Authentication.Cookies; //Dùng để gọi CookieAuthenticationDefaults
using Microsoft.EntityFrameworkCore;

namespace WebNangCao_MVC_Model.Controllers
{
    public class AccountController : Controller
    {
        // 1. KHAI BÁO CÁC DEPENDENCY (DỊCH VỤ CẦN DÙNG TỪ HỆ THỐNG)
        private readonly IValidator<LoginViewModel> _loginValidator;
        private readonly IValidator<RegisterViewModel> _registerValidator;
        private readonly AppDbContext _context;

        //khai báo DB ConText
        // _context là kế thừa từ AppDbContext, có nhiệm vụ kết nối và thao tác với Database PostgreSQL

        //var user = _context.Users.FirstOrDefault(u => (u.Username == model.Login.UsernameOrEmail ||
        // u.Email == model.Login.UsernameOrEmail)
        //&& u.PasswordHash == model.Login.Password);
        // ----->_context.Users: truy cập vào bảng Users trong Database thông qua DbSet<User> Users đã khai báo trong AppDbContext
        // ----->FirstOrDefault: tìm kiếm bản ghi đầu tiên thỏa mãn


        //bơm(Inject) DB ConText vào Constructor

        // 2. CONSTRUCTOR INJECTION
        // Hệ thống (thông qua file Program.cs) sẽ tự động "bơm" (inject) 2 cái Validator vào đây
        // 2 hàm loginValidator và registerValidator này sẽ được khởi tạo sẵn tại thư mục Validators, và được đăng ký trong Program.cs rồi nên ta chỉ việc hứng vào đây là xài được luôn
        public AccountController(
            IValidator<LoginViewModel> loginValidator,
            IValidator<RegisterViewModel> registerValidator,
            AppDbContext context
            )
        {
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
            _context = context; //gắn vào biến toàn cục của Controller
        }

        // KHU VỰC HIỂN THỊ GIAO DIỆN (GET)

        // Hàm này chạy khi người dùng gõ /Account/Index hoặc vào trang đăng nhập
        // Có tham số mặc định là activeTab = "login" để lúc nào mới vào cũng hiện tab Đăng nhập
        public async Task<IActionResult> Index(string activeTab = "login")
        {
            var model = new AuthViewModels();
            model.ActiveTab = activeTab; // Gán trạng thái để View biết nên mở Tab nào
            // --- CHÈN THÊM LOGIC LẤY CẤU HÌNH ---
            var config = await _context.SystemConfigs.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.MinPassLength = config?.MinPasswordLength ?? 6;
            return View(model);
        }

        // Nếu người dùng lỡ gõ trực tiếp /Account/Login lên thanh địa chỉ
        // Ta đẩy họ về lại hàm Index ở trên kèm theo tham số báo hiệu mở tab "login"
        [HttpGet]
        public IActionResult Login()
        {
            return RedirectToAction("Index", new { activeTab = "login" });
        }


        // KHU VỰC XỬ LÝ DỮ LIỆU GỬI LÊN (POST)

        [HttpPost]
        public async Task<IActionResult> Login(AuthViewModels model, string Role)
        {
            var config = await _context.SystemConfigs.AsNoTracking().FirstOrDefaultAsync();
// Dùng đúng tên trường trong Model của em, nếu null thì mặc định 30 phút
int timeoutMinutes = config?.SessionTimeoutMinutes ?? 30;
// Tạo thẻ Cookie với thời hạn sống được lấy từ Config
int maxFailedLogins = config?.MaxFailedLogins ?? 5; // Lấy số 5 từ giao diện bảo mật của em
            // BƯỚC 1: KIỂM TRA LỖI NHẬP LIỆU (VALIDATION)
            ValidationResult result = _loginValidator.Validate(model.Login);
            if (!result.IsValid)
            {
                // Nếu có lỗi (để trống, sai định dạng...)
                foreach (var error in result.Errors)
                {
                    // Từ khóa $"Login.{error.PropertyName}" giúp C# map đúng vào thẻ <span asp-validation-for="Login..."> bên HTML
                    ModelState.AddModelError($"Login.{error.PropertyName}", error.ErrorMessage);
                }

                model.ActiveTab = "login"; // Giữ nguyên tab Đăng nhập
                return View("Index", model); // Trả lại giao diện kèm thông báo lỗi
            }

            //Bước 2: truy vấn PsstGreSQL để đăng nhập
            //tìm user có username hoặc email khớp với dữ liệu nhập vào
            //so sánh mật khẩu trong database và khi người dùng nhập vào
            var user = _context.Users.FirstOrDefault(u => (u.Username == model.Login.UsernameOrEmail || u.Email == model.Login.UsernameOrEmail));
            //nếu tìm thấy tài khoản hợp lệ
            if (user != null)
            {
                bool isPasswordCorrect = false;
                //  CHỐT CHẶN 1: KIỂM TRA XEM TÀI KHOẢN CÓ ĐANG BỊ KHÓA KHÔNG?
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            // Tính xem còn bao nhiêu phút nữa thì được thả
            var timeLeft = user.LockoutEnd.Value - DateTime.UtcNow;
            int minutesLeft = (int)Math.Ceiling(timeLeft.TotalMinutes);

            ModelState.AddModelError("Login.Password", $"Tài khoản đã bị khóa do nhập sai quá nhiều lần. Vui lòng thử lại sau {minutesLeft} phút.");
            model.ActiveTab = "login";
            return View("Index", model);
        }
                // 1. KIỂM TRA: Nếu mật khẩu trong DB KHÔNG bắt đầu bằng "$" (tức là chưa được mã hóa bằng BCrypt)
                if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$"))
                {
                    // 2. So sánh trực tiếp chữ thường (Plain text)
                    if (model.Login.Password == user.PasswordHash)
                    {
                        isPasswordCorrect = true; // Đúng mật khẩu

                        // 3. LẶNG LẼ NÂNG CẤP MẬT KHẨU: Băm mật khẩu này và lưu đè lại vào Database
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Login.Password);
                        _context.SaveChanges(); // Lưu thay đổi xuống DB
                    }
                }
                else
                {
                    // 4. Nếu mật khẩu đã có định dạng BCrypt (Bắt đầu bằng "$") -> Kiểm tra bình thường
                    isPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.Login.Password, user.PasswordHash);
                }
                if (!isPasswordCorrect) 
                {
                    user.FailedLoginAttempts += 1; // Cộng thêm 1 lần sai

            // Nếu vượt quá hoặc bằng số lần cho phép trong config (số 5)
            if (user.FailedLoginAttempts >= maxFailedLogins)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15); // Khóa cụ nó lại 15 phút
                _context.SaveChanges(); // Lưu xuống DB luôn

                ModelState.AddModelError("Login.Password", $"Bạn đã nhập sai {user.FailedLoginAttempts} lần. Tài khoản bị khóa 15 phút!");
            }
            else
            {
                _context.SaveChanges(); // Lưu số lần sai vào DB để nhịp sau đếm tiếp
                int attemptsLeft = maxFailedLogins - user.FailedLoginAttempts;
                ModelState.AddModelError("Login.Password", $"Mật khẩu không chính xác. Bạn còn {attemptsLeft} lần thử.");
            }

            model.ActiveTab = "login";
            return View("Index", model);
        }

        //  THỨ 2: NẾU NHẬP ĐÚNG MẬT KHẨU THÀNH CÔNG
        // Reset ngay lập tức các chỉ số sai phạm về 0 để mở xích tài khoản
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        
        // Lặng lẽ nâng cấp mật khẩu nếu là plaintext (Đoạn cũ của em giữ nguyên)
        
        _context.SaveChanges(); // Lưu lại trạng thái sạch sẽ xuống DB
                   
                
                // --- CHÈN THÊM LOGIC CỦA ADMIN: KIỂM TRA ĐÃ ĐƯỢC DUYỆT CHƯA ---
                if (user.IsActive == false && user.Role?.ToLower() != "admin")
                {
                    ModelState.AddModelError("Login.Password", "Tài khoản của bạn đang chờ Admin duyệt. Vui lòng quay lại sau!");
                    model.ActiveTab = "login";
                    return View("Index", model);
                }
                //so sánh Role trên giao diện Front-End với Role lưu trong Database (user.Role)
                if (user.Role.ToLower() != Role.ToLower())
                {
                    //nếu không khớp với Role thì đẩy ra lỗi
                    ModelState.AddModelError("Login.Password", "Tài khoản của bạn không thuộc vai trò này. Vui lòng chọn đúng vai trò");
                    model.ActiveTab = "login";
                    return View("Index", model);
                }
                //lấy Role trực tiếp từ Database của user đó
                string currentRole = user.Role ?? "student"; //mặc định là student
                //GIẢI PHÁP CHỐNG HACKER: newUser.Role = "student"; // LUÔN LUÔN là student, Admin phải được set tay trong DB
                //tạo thẻ căn cước (Claims lưu vào COOKIE)
                var claims =new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), //Lưu UserId để sau này còn truy xuất dữ liệu theo UserId
                    new Claim(ClaimTypes.Name, user.FullName), //Lưu FullName để hiển thị trên giao diện
                    new Claim(ClaimTypes.Role, currentRole) //Lưu Role để phân quyền truy cập các trang sau này
                };
                //đăng nhập và lưu Cookie xuống trình duyệt
                var clamsIdentity=new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, //nhớ đăng nhập (F5 không bị văng về login)
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(timeoutMinutes)
                };
                //Đăng nhập và lưu Cookie xuống trình duyệt
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(clamsIdentity), authProperties);
                if (currentRole == "student")
                {
                    //Controller="Student", hàm trong controller = "Dashboard"
                    return RedirectToAction("Dashboard", "Student");
                }
                else if (currentRole == "instructor")
                {
                    //Controller="Instructor", hàm trong controller = "Dashboard"
                    return RedirectToAction("Dashboard", "Instructor");
                }
                else if (currentRole == "Admin") // Đã thêm "else" và dùng chữ thường
{
    return RedirectToAction("Index", "Admin"); 
}
else 
{
    // CHỐT CHẶN AN TOÀN CUỐI CÙNG: 
    // Nếu Role trong DB là một cái tên lạ hoắc nào đó (bị lỗi data), 
    // thì đá nó về trang chủ, tuyệt đối không được rớt xuống lỗi "Sai mật khẩu" bên dưới!
    return RedirectToAction("Index", "Home"); 
}
            }
            //nếu user==null(không tìm thấy trong DB)
            ModelState.AddModelError("login.Password", "Tài khoản hoặc mật khẩu không chính xác.");
            model.ActiveTab = "login";
            return View("Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(AuthViewModels model)
        {

            // BƯỚC 1: KIỂM TRA LỖI FORM ĐĂNG KÝ
            ValidationResult result = _registerValidator.Validate(model.Register);
            // --- CHÈN THÊM LOGIC KIỂM TRA LUẬT CỦA ADMIN ---
            var config = await _context.SystemConfigs.AsNoTracking().FirstOrDefaultAsync();
            int minPass = config?.MinPasswordLength ?? 6;
            ViewBag.MinPassLength = minPass; // Nạp sẵn phòng khi bị lỗi trả về View
            if (model.Register.Password != null && model.Register.Password.Length < minPass)
            {
                ModelState.AddModelError("Register.Password", $"Hệ thống yêu cầu mật khẩu tối thiểu {minPass} ký tự.");
                // Dùng kỹ thuật bùa chú ép cái result.IsValid = false để nó rớt xuống block dưới
                result.Errors.Add(new ValidationFailure("Password", "Lỗi độ dài")); 
            }
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    if(error.PropertyName != "Password") // Tránh in trùng lỗi mật khẩu mình vừa tự add// Tương tự, map lỗi vào thuộc tính Register (VD: Register.Name, Register.Email)
                    ModelState.AddModelError($"Register.{error.PropertyName}", error.ErrorMessage);
                }

                model.ActiveTab = "register"; // Giữ người dùng ở lại tab Đăng ký để họ sửa lỗi
                return View("Index", model);
            }

            //Bước 4: thêm tài khoản mới vào PostGreSQL
            bool isExist = _context.Users.Any(u => u.Username == model.Register.Username || u.Email == model.Register.Email);
            if (isExist)
            {
                ModelState.AddModelError("Register.Username", "Tên đăng nhập hoặc Email đã tồn tại!");
                model.ActiveTab = "register";
                return View("Index", model);
            }
            // Tạo đối tượng User mới từ dữ liệu form gửi lên
            //Băm dữ liệu trước khi đưa vào database
            string hashPassword = BCrypt.Net.BCrypt.HashPassword(model.Register.Password);
            var newUser = new User
            {
                FullName = model.Register.Name,
                Username = model.Register.Username,
                Email = model.Register.Email, 
                PasswordHash = hashPassword,
                Role = model.Register.Role ?? "student",
                IsActive = false
            };
            //nếu chưa có tài khoản nào thì sẽ tạo và lưu dữ liệu vào Database
            // Lưu xuống Database
            _context.Users.Add(newUser);
            _context.SaveChanges();
            //tự động gắn sinh viên vào lớp học đầu tiên dưới dạng mặc định
            if (newUser.Role == "student")
            {
                // Tìm lớp học đầu tiên (Lớp Tiếng Anh IT K12 do Seed data tạo)
                var defaultGroup = _context.Groups.FirstOrDefault();

                if (defaultGroup != null)
                {
                    var userGroup = new UserGroup
                    {
                        UserId = newUser.Id, // EF Core đã tự sinh ID cho newUser ở hàm SaveChangesAsync phía trên
                        GroupId = defaultGroup.Id,
                        JoinedAt = DateTime.UtcNow
                    };

                    _context.UserGroups.Add(userGroup);
                    await _context.SaveChangesAsync(); // Lưu bảng trung gian vào DB
                    // 🔴 CHÈN ĐÚNG 6 DÒNG NÀY VÀO ĐÂY - KHÔNG XOÁ HOẶC SỬA BẤT KỲ CHỮ NÀO CỦA CODE CŨ!
            if (newUser.IsActive == false)
            {
                ModelState.AddModelError("Login.UsernameOrEmail", "Đăng ký thành công! Vui lòng chờ Admin phê duyệt tài khoản để đăng nhập.");
                model.ActiveTab = "login"; // Ép giao diện quay về tab Đăng nhập
                return View("Index", model); // Đẩy về trang chủ luôn, không cho chạy xuống dưới nữa!
            }
                }
            }

            //tạo thẻ căn cước (Claims lưu vào COOKIE) sau khi đăng ký xong
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()), //Lưu UserId để sau này còn truy xuất dữ liệu theo UserId
                new Claim(ClaimTypes.Name, newUser.FullName), //Lưu FullName để hiển thị trên giao diện
                new Claim(ClaimTypes.Role, newUser.Role) //Lưu Role để phân quyền truy cập các trang sau này
            };
            //đăng nhập và lưu Cookie xuống trình duyệt
            var clamsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true //nhớ đăng nhập (F5 không bị văng về login)
            };
            //Đăng nhập và lưu Cookie xuống trình duyệt
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(clamsIdentity), authProperties);
            // Đăng ký xong, điều hướng vào thẳng Dashboard tương ứng
            if (newUser.Role == "student")
            {
                return RedirectToAction("Dashboard", "Student");
            }
            else
            {
                return RedirectToAction("Dashboard", "Instructor");
            }
        }
        //Thủ tục khi Đăng xuất tài khoản và tiêu hủy Cookie
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // 1. LỆNH XÓA COOKIE
            // Lệnh này ra chỉ thị cho trình duyệt: "Hãy xóa sạch chiếc Cookie xác thực của trang web này đi"
            //Không có cookie tức là không cho phép truy cập lại vào trang, và không cho phép thao tác gì thêm nữa khi "BACK"
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Sau khi thẻ Cookie đã bị hủy, ta mới điều hướng người dùng về lại màn hình Đăng nhập (Tab Login)
            return RedirectToAction("Index", "Account");
        }
    }
}
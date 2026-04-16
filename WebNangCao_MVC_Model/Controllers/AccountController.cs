using FluentValidation;
using FluentValidation.Results; // Dùng để hứng kết quả trả về từ Validator
using Microsoft.AspNetCore.Mvc;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.Data; //để gọi AppDBContext
using System.Linq; //Để dùng FirstOrDefault, Any
using System.Security.Claims; //Dùng để lấy thông tin UserId từ Claims khi đã đăng nhập
using Microsoft.AspNetCore.Authentication; //Dùng để gọi SignInAsync, SignOutAsync khi đăng nhập/đăng xuất
using Microsoft.AspNetCore.Authentication.Cookies; //Dùng để gọi CookieAuthenticationDefaults

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
        public IActionResult Index(string activeTab = "login")
        {
            var model = new AuthViewModels();
            model.ActiveTab = activeTab; // Gán trạng thái để View biết nên mở Tab nào
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
                    ModelState.AddModelError("Login.Password", "Tài khoản hoặc mật khẩu không chính xác.");
                    model.ActiveTab = "login";
                    return View("Index", model);
                }
                //so sánh Role trên giao diện Front-End với Role lưu trong Database (user.Role)
                if (user.Role != Role)
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
                    IsPersistent = true //nhớ đăng nhập (F5 không bị văng về login)
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

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    // Tương tự, map lỗi vào thuộc tính Register (VD: Register.Name, Register.Email)
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
                Role = model.Register.Role ?? "student"
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
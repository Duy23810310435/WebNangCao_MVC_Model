using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;

namespace WebNangCao_MVC_KiemThu
{
    [TestFixture]
    public class DangNhapDangKyTest
    {
        // Đối tượng điều khiển trình duyệt (WebDriver) dùng để mô phỏng hành vi người dùng trên UI
        private IWebDriver _driver;

        // URL gốc của hệ thống cần kiểm thử (chạy local)
        private const string BaseUrl = "https://localhost:7000";

        [SetUp]
        public void Setup()
        {
            // Khởi tạo trình duyệt Chrome
            _driver = new ChromeDriver();

            // Phóng to cửa sổ để tránh lỗi không click được element do responsive UI
            _driver.Manage().Window.Maximize();

            // Thiết lập thời gian chờ ngầm định:
            // Khi tìm element, Selenium sẽ chờ tối đa 10 giây trước khi ném lỗi
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        //
        // KHU VỰC 1: KIỂM THỬ LUỒNG CHUẨN (HAPPY PATH)
        // Mục tiêu:
        // - Xác minh các chức năng cốt lõi hoạt động đúng theo thiết kế nghiệp vụ
        // - Đây là các test nền tảng, nếu fail thì không cần kiểm thử sâu hơn
        // 

        /// <summary>
        /// KỊCH BẢN 1: Đăng nhập hợp lệ và truy cập chức năng làm bài thi
        /// 
        /// MỤC TIÊU KIỂM THỬ:
        /// - Xác nhận luồng nghiệp vụ chính của hệ thống (End-to-End Flow)
        /// - Bao gồm: Login → Dashboard → Truy cập phòng thi
        /// 
        /// PHÂN TÍCH KỸ THUẬT:
        /// - Gửi request đăng nhập thông qua form HTML
        /// - Hệ thống backend xác thực thông tin → tạo session/cookie
        /// - Redirect về Dashboard nếu thành công
        /// - Click nút "Bắt đầu làm bài" → điều hướng sang giao diện thi
        /// 
        /// KẾT QUẢ KỲ VỌNG:
        /// - URL chứa "/Student/Dashboard" sau khi login
        /// - URL chứa "/TestAttempt/GiaoDienLamBai" sau khi bắt đầu thi
        /// 
        /// Ý NGHĨA:
        /// - Đây là test quan trọng nhất (Critical Path)
        /// - Nếu thất bại → hệ thống coi như không sử dụng được
        /// </summary>
        [Test]
        public void Test01_Student_LoginAndAccessExam()
        {
            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");

            // Nhập email/tài khoản
            _driver.FindElement(By.Id("Login_UsernameOrEmail")).SendKeys("Nguyendinhduy257@gmail.com");

            // Nhập mật khẩu
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");

            // Submit form đăng nhập
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            // Explicit wait: chờ đến khi URL chuyển sang Dashboard
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            // Tìm nút bắt đầu làm bài thông qua XPath (dựa vào class + text)
            var btnBatDau = _driver.FindElement(By.XPath("//a[contains(@class, 'btn-primary-action') and contains(., 'Bắt đầu làm bài')]"));
            btnBatDau.Click();

            // Chờ điều hướng sang trang làm bài thi
            wait.Until(d => d.Url.Contains("/TestAttempt/GiaoDienLamBai"));

            // Kiểm tra kết quả cuối cùng
            Assert.IsTrue(_driver.Url.Contains("/TestAttempt/GiaoDienLamBai"),
                "LỖI CHỨC NĂNG: Không thể truy cập giao diện làm bài thi.");
        }

        /// <summary>
        /// KỊCH BẢN 2: Đăng ký tài khoản với Role = Instructor
        /// 
        /// MỤC TIÊU KIỂM THỬ:
        /// - Xác minh hệ thống hỗ trợ đa vai trò (Role-based system)
        /// - Kiểm tra việc chọn Role từ UI có được backend xử lý đúng hay không
        /// 
        /// PHÂN TÍCH KỸ THUẬT:
        /// - Role được chọn thông qua JavaScript (không phải input trực tiếp)
        /// - Hàm selectRole(...) sẽ gán giá trị vào input hidden
        /// - Backend đọc giá trị này khi submit form
        /// 
        /// KẾT QUẢ KỲ VỌNG:
        /// - Tài khoản được tạo thành công
        /// - Hệ thống tự động redirect sang "/Instructor/Dashboard"
        /// 
        /// RỦI RO:
        /// - Nếu backend không validate Role → có thể bị giả mạo (test ở kịch bản 3)
        /// </summary>
        [Test]
        public void Test02_Instructor_RegisterSuccessfully()
        {
            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=register");

            _driver.FindElement(By.Id("Register_Name")).SendKeys("Giảng Viên Test");
            _driver.FindElement(By.Id("Register_Email")).SendKeys("gv_test_123@gmail.com");
            _driver.FindElement(By.Id("Register_Username")).SendKeys("gv_test_123");
            _driver.FindElement(By.Id("Register_Password")).SendKeys("123456");
            _driver.FindElement(By.Id("Register_ConfirmPassword")).SendKeys("123456");

            // Thực thi JavaScript để chọn Role Instructor (giả lập thao tác UI)
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("selectRole('instructor', 'Giảng viên', 'briefcase')");

            _driver.FindElement(By.CssSelector("#tab-content-register .btn-submit")).Click();

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // Kiểm tra điều hướng thành công
            bool isRedirected = wait.Until(d => d.Url.Contains("/Instructor/Dashboard"));

            Assert.IsTrue(isRedirected,
                "LỖI CHỨC NĂNG: Đăng ký Instructor thất bại hoặc không điều hướng đúng.");
        }

        // 
        // KHU VỰC 2: KIỂM THỬ BẢO MẬT (SECURITY TESTING)
        // Mục tiêu:
        // - Phát hiện các lỗ hổng do backend tin tưởng dữ liệu từ client
        // - Mô phỏng hành vi tấn công thực tế (hacker mindset)
        // 

        /// <summary>
        /// KỊCH BẢN 3: Tấn công leo thang đặc quyền (Privilege Escalation)
        /// 
        /// MỤC TIÊU:
        /// - Kiểm tra backend có validate Role hay không
        /// 
        /// NGUYÊN LÝ TẤN CÔNG:
        /// - Role được lưu trong input hidden → có thể bị sửa bằng DevTools (F12)
        /// - Hacker thay giá trị thành "admin" trước khi submit
        /// 
        /// VẤN ĐỀ BACKEND:
        /// - Backend sử dụng trực tiếp giá trị từ client:
        ///   newUser.Role = model.Register.Role ?? "student";
        /// - Không có whitelist hoặc kiểm tra quyền
        /// 
        /// KẾT QUẢ KỲ VỌNG:
        /// - Backend phải từ chối hoặc override về role an toàn
        /// 
        /// HẬU QUẢ NẾU FAIL:
        /// - Người dùng thường có thể tạo tài khoản Admin
        /// - Toàn bộ hệ thống bị kiểm soát
        /// </summary>
        [Test]
        public void Test03_Attack_PrivilegeEscalation_Admin()
        {
            _driver.Navigate().GoToUrl(BaseUrl + "/Account/Index?activeTab=register");

            _driver.FindElement(By.Id("Register_Name")).SendKeys("Hacker Leo Quyền");
            _driver.FindElement(By.Id("Register_Email")).SendKeys($"hacker_{Guid.NewGuid()}@test.com");
            _driver.FindElement(By.Id("Register_Username")).SendKeys($"hacker_{Guid.NewGuid().ToString().Substring(0, 5)}");
            _driver.FindElement(By.Id("Register_Password")).SendKeys("123456");
            _driver.FindElement(By.Id("Register_ConfirmPassword")).SendKeys("123456");

            // TẤN CÔNG: sửa trực tiếp DOM để thay đổi role
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("document.getElementById('input-role-register').value = 'admin';");

            _driver.FindElement(By.CssSelector("#tab-content-register .btn-submit")).Click();

            Thread.Sleep(2000);

            // Kiểm tra xem có bị chuyển vào khu vực Admin không
            bool isPrivilegeEscalated = _driver.Url.Contains("Admin");

            Assert.IsFalse(isPrivilegeEscalated,
                "LỖI BẢO MẬT NGHIÊM TRỌNG: Cho phép leo thang đặc quyền thành Admin.");
        }

        /// <summary>
        /// KỊCH BẢN 4: Rò rỉ thông tin thông qua thông báo lỗi (Information Disclosure)
        /// 
        /// MỤC TIÊU:
        /// - Kiểm tra hệ thống có vô tình tiết lộ thông tin nhạy cảm không
        /// 
        /// NGUYÊN LÝ:
        /// - Khi đăng nhập sai Role, hệ thống trả về thông báo cụ thể:
        ///   "Tài khoản không thuộc vai trò này"
        /// 
        /// VẤN ĐỀ:
        /// - Hacker có thể suy ra:
        ///   + Email tồn tại trong hệ thống
        ///   + Vai trò thực tế của tài khoản
        /// 
        /// KẾT QUẢ KỲ VỌNG:
        /// - Chỉ hiển thị thông báo chung:
        ///   "Sai tài khoản hoặc mật khẩu"
        /// 
        /// NGUY CƠ:
        /// - Hỗ trợ brute-force và reconnaissance attack
        /// </summary>
        [Test]
        public void Test04_Attack_RoleSpoofing_InfoDisclosure()
        {
            _driver.Navigate().GoToUrl(BaseUrl + "/Account/Index?activeTab=login");

            _driver.FindElement(By.Id("Login_UsernameOrEmail")).SendKeys("DuyGV@gmail.com");
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");

            // Cố tình chọn sai Role
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("selectRole('student', 'Học viên', 'user')");

            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            // Lấy nội dung thông báo lỗi
            var errorMsg = _driver.FindElement(By.ClassName("validation-summary")).Text;

            Assert.IsFalse(errorMsg.Contains("không thuộc vai trò này"),
                "LỖI RÒ RỈ THÔNG TIN: Thông báo lỗi tiết lộ sự tồn tại của tài khoản.");
        }

        // 
        // KHU VỰC 3: VALIDATION & DATA TESTING
        // Mục tiêu:
        // - Kiểm tra xử lý dữ liệu đầu vào
        // - Phát hiện lỗi XSS, overflow, duplicate
        //

        /// <summary>
        /// KỊCH BẢN 5: Kiểm tra XSS và giới hạn độ dài dữ liệu
        /// 
        /// MỤC TIÊU:
        /// - Phát hiện lỗ hổng XSS (Cross-Site Scripting)
        /// - Kiểm tra thiếu ràng buộc độ dài (MaximumLength)
        /// 
        /// NGUYÊN LÝ:
        /// - Inject mã độc vào input (script, img onerror)
        /// - Gửi chuỗi quá dài để gây lỗi DB
        /// 
        /// KẾT QUẢ KỲ VỌNG:
        /// - Dữ liệu độc hại bị chặn
        /// - Không xảy ra lỗi 500
        /// 
        /// NGUY CƠ:
        /// - XSS → chiếm session Admin
        /// - Overflow → crash hệ thống
        /// </summary>
        [TestCase("<script>alert('XSS')</script>", "xss1@test.com", "xss_user", "Mã độc XSS trong Tên")]
        [TestCase("Normal Name", "xss_email@test.com", "<img src=x onerror=alert(1)>", "Mã độc XSS trong Username")]
        [TestCase("Tên Rất Dài...(>500 ký tự)", "long@test.com", "long_user", "Tràn bộ nhớ do không có MaxLength")]
        public void Test05_Validation_XSS_And_MaxLength(string name, string email, string username, string description)
        {
            _driver.Navigate().GoToUrl(BaseUrl + "/Account/Index?activeTab=register");

            if (name.Contains(">500")) name = new string('A', 501);

            _driver.FindElement(By.Id("Register_Name")).SendKeys(name);
            _driver.FindElement(By.Id("Register_Email")).SendKeys(email);
            _driver.FindElement(By.Id("Register_Username")).SendKeys(username);
            _driver.FindElement(By.Id("Register_Password")).SendKeys("123456");
            _driver.FindElement(By.Id("Register_ConfirmPassword")).SendKeys("123456");

            _driver.FindElement(By.CssSelector("#tab-content-register .btn-submit")).Click();
            Thread.Sleep(1000);

            // Kiểm tra lỗi hệ thống
            bool isSystemCrashed = _driver.PageSource.Contains("Exception") || _driver.PageSource.Contains("500");

            Assert.IsFalse(isSystemCrashed,
                $"CRASH DB [{description}]: Lỗi do thiếu giới hạn độ dài.");

            // Kiểm tra việc lưu dữ liệu độc hại
            bool isRegisteredSuccessfully = _driver.Url.Contains("/Dashboard");

            Assert.IsFalse(isRegisteredSuccessfully,
                $"LỖI XSS [{description}]: Dữ liệu độc hại đã được lưu.");
        }

        /// <summary>
        /// KỊCH BẢN 6: Lách kiểm tra trùng lặp dữ liệu
        /// 
        /// MỤC TIÊU:
        /// - Kiểm tra logic chống duplicate (Email/Username)
        /// 
        /// NGUYÊN LÝ:
        /// - PostgreSQL phân biệt chữ hoa/thường
        /// - Chuỗi có thể chứa khoảng trắng ở đầu/cuối
        /// 
        /// VẤN ĐỀ:
        /// - Nếu không dùng .Trim().ToLower() → bypass dễ dàng
        /// 
        /// KẾT QUẢ KỲ VỌNG:
        /// - Hệ thống phát hiện trùng lặp và báo lỗi
        /// 
        /// NGUY CƠ:
        /// - Sinh ra nhiều tài khoản clone
        /// - Phá vỡ tính toàn vẹn dữ liệu
        /// </summary>
        [TestCase("DUY@gmail.com", "clone_user1", "Bypass bằng chữ HOA ở Email")]
        [TestCase("duy@gmail.com  ", "clone_user2", "Bypass bằng Khoảng Trắng ở Email")]
        [TestCase("new1@test.com", "DUYADMIN", "Bypass bằng chữ HOA ở Username")]
        [TestCase("new2@test.com", "duyadmin  ", "Bypass bằng Khoảng Trắng ở Username")]
        public void Test06_Validation_DuplicateBypass(string testEmail, string testUsername, string description)
        {
            _driver.Navigate().GoToUrl(BaseUrl + "/Account/Index?activeTab=register");

            _driver.FindElement(By.Id("Register_Name")).SendKeys("Kẻ mạo danh");
            _driver.FindElement(By.Id("Register_Email")).SendKeys(testEmail);
            _driver.FindElement(By.Id("Register_Username")).SendKeys(testUsername);
            _driver.FindElement(By.Id("Register_Password")).SendKeys("123456");
            _driver.FindElement(By.Id("Register_ConfirmPassword")).SendKeys("123456");

            _driver.FindElement(By.CssSelector("#tab-content-register .btn-submit")).Click();
            Thread.Sleep(1000);

            bool isRedirectedToDashboard = _driver.Url.Contains("/Dashboard");

            Assert.IsFalse(isRedirectedToDashboard,
                $"LỖI DUPLICATE [{description}]: Cho phép tạo tài khoản trùng lặp.");
        }

        [TearDown]
        public void Teardown()
        {
            // Đóng toàn bộ trình duyệt
            _driver.Quit();

            // Giải phóng tài nguyên WebDriver
            _driver.Dispose();
        }
    }
}
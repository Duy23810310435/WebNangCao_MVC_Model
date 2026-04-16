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
        private IWebDriver _driver;
        private const string BaseUrl = "https://localhost:7000";

        [SetUp]
        public void Setup()
        {
            _driver = new ChromeDriver();
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        // ==========================================
        // KHU VỰC 1: KIỂM THỬ LUỒNG CHUẨN (HAPPY PATH)
        // Mục đích chung: Đảm bảo các chức năng cốt lõi nhất không bị hỏng trước khi test bảo mật.
        // ==========================================
        //1 và 2 là kịch bản đăng nhập sạch
        /// <summary>
        /// KỊCH BẢN 1: Học viên đăng nhập và vào phòng thi hợp lệ.
        /// - Vì sao/Mục đích: Đảm bảo chức năng quan trọng nhất của hệ thống (thi trực tuyến) đang hoạt động bình thường. 
        /// Nếu test này fail, toàn bộ hệ thống coi như sập, không cần test tiếp các lỗi phức tạp khác.
        /// - Dự đoán: Trình duyệt chuyển hướng thành công sang Dashboard và sau đó vào được URL làm bài thi.
        /// - Kết quả thực tế (nếu Pass): Hệ thống hoạt động đúng thiết kế chuẩn.
        /// </summary>
        [Test]
        public void Test01_Student_LoginAndAccessExam()
        {
            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");
            _driver.FindElement(By.Id("Login_UsernameOrEmail")).SendKeys("Nguyendinhduy257@gmail.com");
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            var btnBatDau = _driver.FindElement(By.XPath("//a[contains(@class, 'btn-primary-action') and contains(., 'Bắt đầu làm bài')]"));
            btnBatDau.Click();

            wait.Until(d => d.Url.Contains("/TestAttempt/GiaoDienLamBai"));
            Assert.IsTrue(_driver.Url.Contains("/TestAttempt/GiaoDienLamBai"), "LỖI CHỨC NĂNG: Sinh viên không thể vào giao diện làm bài.");
        }

        /// <summary>
        /// KỊCH BẢN 2: Đăng ký tài khoản Giảng viên hợp lệ.
        /// - Vì sao/Mục đích: Yêu cầu nghiệp vụ cho phép người dùng tự chọn làm Student hoặc Instructor ở màn hình đăng ký.
        /// Cần xác minh luồng chọn Role "Instructor" trên UI hoạt động đúng.
        /// - Dự đoán: Đăng ký thành công và bị điều hướng sang trang /Instructor/Dashboard.
        /// - Kết quả thực tế: Hoạt động đúng thiết kế.
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

            // Chọn Role Giảng viên qua thao tác click UI (JS giả lập)
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("selectRole('instructor', 'Giảng viên', 'briefcase')");

            _driver.FindElement(By.CssSelector("#tab-content-register .btn-submit")).Click();

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            bool isRedirected = wait.Until(d => d.Url.Contains("/Instructor/Dashboard"));
            Assert.IsTrue(isRedirected, "LỖI CHỨC NĂNG: Không thể đăng ký tài khoản Instructor dù nghiệp vụ cho phép.");
        }

        // ==========================================
        // KHU VỰC 2: KIỂM THỬ BẢO MẬT & LOGIC (SECURITY)
        // Mục đích chung: Tấn công vào các sơ hở trong tư duy lập trình Backend.
        // ==========================================

        /// <summary>
        /// KỊCH BẢN 3: Tấn công leo thang đặc quyền (Privilege Escalation).
        /// - Vì sao/Mục đích: Đánh giá xem Backend có tin tưởng mù quáng vào dữ liệu gửi từ Frontend hay không.
        /// Ở đây, Role được gửi lên qua một thẻ <input type="hidden">. Kẻ xấu có thể dùng F12 (DevTools) để sửa thành 'admin'.
        /// - Dự đoán (Expected): Backend phải kiểm tra lại biến Role. Nếu là 'admin', phải chặn lại hoặc tự động ép về 'student'.
        /// - Kết quả thực tế (Actual): Dòng code `newUser.Role = model.Register.Role ?? "student";` trong Controller lấy trực tiếp giá trị bị thao túng. Kẻ tấn công tạo thành công tài khoản Admin.
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

            // TẤN CÔNG: Ép giá trị role ẩn thành admin bằng Javascript (Mô phỏng F12 của Hacker)
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("document.getElementById('input-role-register').value = 'admin';");

            _driver.FindElement(By.CssSelector("#tab-content-register .btn-submit")).Click();

            Thread.Sleep(2000);
            bool isPrivilegeEscalated = _driver.Url.Contains("Admin");

            Assert.IsFalse(isPrivilegeEscalated,
                " LỖI BẢO MẬT NGHIÊM TRỌNG (Privilege Escalation): " +
                "Hệ thống đã cho phép Hacker tự phong làm Admin thông qua F12!");
        }

        /// <summary>
        /// KỊCH BẢN 4: Thám tử vai trò - Rò rỉ thông tin (Information Disclosure).
        /// - Vì sao/Mục đích: Hacker thường rà quét (Brute-force) để tìm kiếm các Email là Admin/Giáo viên nhằm mục tiêu hack sau này.
        /// - Dự đoán (Expected): Dù sai mật khẩu hay sai Role, hệ thống chỉ nên báo 1 câu duy nhất: "Sai tài khoản hoặc mật khẩu".
        /// - Kết quả thực tế (Actual): Controller báo "Tài khoản của bạn không thuộc vai trò này". Câu này vô tình TỐ CÁO với Hacker rằng Email này ĐÃ TỒN TẠI trên hệ thống và nó có chức vụ cao hơn Student.
        /// </summary>
        [Test]
        public void Test04_Attack_RoleSpoofing_InfoDisclosure()
        {
            _driver.Navigate().GoToUrl(BaseUrl + "/Account/Index?activeTab=login");
            _driver.FindElement(By.Id("Login_UsernameOrEmail")).SendKeys("DuyGV@gmail.com"); // Giả sử đây là mail Admin
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");

            // Chọn sai Role (chọn Student thay vì Giáo Viên)
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("selectRole('student', 'Học viên', 'user')");
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            var errorMsg = _driver.FindElement(By.ClassName("validation-summary")).Text;
            Assert.IsFalse(errorMsg.Contains("không thuộc vai trò này"),
                " LỖI RÒ RỈ THÔNG TIN: Báo lỗi sai chức vụ đã giúp Hacker biết được Email có tồn tại trên DB.");
        }

        // ==========================================
        // KHU VỰC 3: KIỂM THỬ VALIDATION VÀ DATA-DRIVEN
        // Mục đích chung: Thử nghiệm các ranh giới dữ liệu (Boundary) và các ký tự đặc biệt.
        // ==========================================

        /// <summary>
        /// KỊCH BẢN 5: Bơm mã độc XSS và Tràn bộ nhớ (Thiếu MaximumLength).
        /// - Vì sao/Mục đích: FluentValidation hiện tại quá sơ sài, chỉ có NotEmpty(). Hệ thống chưa có cơ chế dọn dẹp mã độc.
        /// - Dự đoán (Expected): Ký tự `<script>` phải bị chặn không cho lưu. Tên quá dài (500 chữ) phải báo lỗi đỏ trên form chứ không được sập Web.
        /// - Kết quả thực tế (Actual): XSS lọt vào DB thành công, sẵn sàng tấn công Admin. Tên dài thì gây ra lỗi 500 (Server Exception) vì quá sức chứa của cột Varchar trong PostgreSQL.
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

            // Bắt lỗi hệ thống sập
            bool isSystemCrashed = _driver.PageSource.Contains("Exception") || _driver.PageSource.Contains("500");
            Assert.IsFalse(isSystemCrashed, $" CRASH DB [{description}]: Lỗi 500 Server do FluentValidator thiếu .MaximumLength().");

            // Bắt lỗi lưu mã độc
            bool isRegisteredSuccessfully = _driver.Url.Contains("/Dashboard");
            Assert.IsFalse(isRegisteredSuccessfully,
                $" LỖI BẢO MẬT XSS [{description}]: Dữ liệu chứa mã độc đã lọt qua Validator và lưu xuống DB.");
        }

        /// <summary>
        /// KỊCH BẢN 6: Lách luật chống trùng lặp dữ liệu (Duplicate Bypass).
        /// - Vì sao/Mục đích: PostgreSQL mặc định là Case-Sensitive (Phân biệt chữ HOA và chữ thường). Nếu Controller dùng toán tử '==' để kiểm tra thì 'duy' khác 'DUY'.
        /// - Dự đoán (Expected): Form sẽ hiển thị chữ đỏ "Email/Username đã tồn tại" khi người dùng cố tình viết hoa hoặc thêm dấu cách.
        /// - Kết quả thực tế (Actual): Hệ thống bị lừa. Nó cho phép tạo tài khoản thành công, sinh ra một đống tài khoản Rác/Clone trong Database.
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
                $"LỖI LOGIC [{description}]: DB PostgreSQL không hiểu chữ HOA/Thường giống nhau, sinh ra tài khoản Clone. Cần thêm hàm .Trim().ToLower() vào Backend!");
        }

        [TearDown]
        public void Teardown()
        {
            _driver.Quit();
            _driver.Dispose();
        }
    }
}
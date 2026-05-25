using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Npgsql;
using NUnit.Framework;
using System.Linq;
using System;
using System.Threading;

namespace WebNangCao_MVC_KiemThu
{
    [TestFixture]
    public class KiemThuQuaTrinhLamBaiThi
    {
        // WebDriver: Thành phần điều khiển trình duyệt và mô phỏng hành vi người dùng
        private IWebDriver _driver;

        // URL gốc của hệ thống kiểm thử
        private const string BaseUrl = "https://localhost:7000";

        // Email tài khoản kiểm thử cố định
        private const string TestEmail = "Nguyendinhduy257@gmail.com";

        // CẤU HÌNH HỆ THỐNG TRƯỚC VÀ SAU KHI CHẠY TOÀN BỘ BỘ TEST

        // Khởi tạo một lần duy nhất trước khi bắt đầu bộ test
        // Mục tiêu: Đưa dữ liệu cơ sở dữ liệu về trạng thái sạch
        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            ResetTestData(TestEmail);
        }

        // Giải phóng một lần duy nhất sau khi kết thúc toàn bộ bộ test
        // Mục tiêu: Dọn dẹp dữ liệu dư thừa và đóng các tài nguyên hệ thống
        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            ResetTestData(TestEmail);
            TestContext.WriteLine("Hoàn tất dọn dẹp dữ liệu sau khi kết thúc bộ test.");
        }

        // CẤU HÌNH TRÌNH DUYỆT TRƯỚC VÀ SAU MỖI TEST CASE

        [SetUp]
        public void SetUp()
        {
            // Thiết lập tùy chọn cho trình duyệt Chrome
            ChromeOptions options = new ChromeOptions();

            // Tắt thông báo hệ thống để tránh gây nhiễu quá trình kiểm thử
            options.AddArgument("--disable-notifications");

            // Bỏ qua các lỗi về chứng chỉ SSL khi chạy môi trường nội bộ (Local)
            options.AddArgument("--ignore-certificate-errors");

            _driver = new ChromeDriver(options);

            // Tối ưu hóa kích thước cửa sổ để đảm bảo các phần tử giao diện hiển thị đầy đủ
            _driver.Manage().Window.Maximize();

            // Thiết lập thời gian chờ ngầm định tối thiểu cho các phần tử
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        }

        [TearDown]
        public void TearDown()
        {
            // Đóng trình duyệt và giải phóng bộ nhớ sau mỗi kịch bản test
            if (_driver != null)
            {
                _driver.Quit();
                _driver.Dispose();
            }
        }

        // HÀM HỖ TRỢ: ĐĂNG NHẬP VÀ ĐIỀU HƯỚNG VÀO PHÒNG THI

        /// <summary>
        /// Logic tái sử dụng cho các kịch bản yêu cầu trạng thái đang làm bài thi
        /// Luồng đi: Đăng nhập -> Dashboard -> Tìm bài thi -> Bắt đầu làm bài
        /// Kiểm tra: Đảm bảo đồng hồ đếm ngược hoạt động để xác nhận JavaScript đã khởi tạo thành công
        /// </summary>
        private void Setup_LoginAndGoToExam(string email = TestEmail, string pass = "12345678")
        {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");

            // Điền thông tin đăng nhập
            var txtUsername = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Login_UsernameOrEmail")));
            txtUsername.Clear();
            txtUsername.SendKeys(email);

            _driver.FindElement(By.Id("Login_Password")).SendKeys(pass);

            // Thực hiện đăng nhập
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            // Chờ đợi điều hướng về trang bảng điều khiển của sinh viên
            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            Thread.Sleep(1000); // Chờ giao diện ổn định

            // Xác định nút "Bắt đầu làm bài" thông qua tính năng phân trang
            var btnBatDau = FindVisibleElementAcrossPages(By.XPath("//a[contains(@class, 'btn-primary-action') and contains(., 'Bắt đầu làm bài')]"));

            Assert.IsNotNull(btnBatDau, "Không tìm thấy nút 'Bắt đầu làm bài' trên giao diện.");

            btnBatDau.Click();

            // Xác nhận trạng thái bắt đầu thi thông qua đồng hồ đếm ngược
            wait.Until(d => {
                try
                {
                    var timer = d.FindElement(By.Id("countdownDisplay"));
                    return timer.Displayed && timer.Text != "00:00";
                }
                catch { return false; }
            });
        }

        // HÀM HỖ TRỢ: XỬ LÝ DỮ LIỆU TRỰC TIẾP TRONG DATABASE

        /// <summary>
        /// Xóa dữ liệu lịch sử thi của tài khoản kiểm thử trong PostgreSQL
        /// Đảm bảo tính độc lập giữa các lần chạy test và tránh lỗi ràng buộc dữ liệu cũ
        /// Xóa theo thứ tự: Chi tiết kết quả -> Kết quả tổng quát
        /// </summary>
        private void ResetTestData(string email)
        {
            string connectionString = "Host=localhost;Port=5432;Database=EduTestDB;Username=postgres;Password=1234;SSL Mode=Disable;";
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        DELETE FROM ""ExamResultDetails"" 
                        WHERE ""ExamResultId"" IN (
                            SELECT ""Id"" FROM ""ExamResults"" 
                            WHERE ""StudentId"" = (
                                SELECT ""Id"" FROM ""Users"" WHERE ""Email"" = @Email
                            )
                        );

                        DELETE FROM ""ExamResults"" 
                        WHERE ""StudentId"" = (
                            SELECT ""Id"" FROM ""Users"" WHERE ""Email"" = @Email
                        );";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi nhận lỗi kết nối nhưng không làm dừng bộ test
                TestContext.WriteLine($"[Lỗi Database]: {ex.Message}");
            }
        }

        // CÁC KỊCH BẢN KIỂM THỬ (TEST CASES)

        /// <summary>
        /// KỊCH BẢN 01: Truy cập giao diện làm bài
        /// Kiểm tra tính chính xác của URL và sự hoạt động của bộ đếm thời gian
        /// </summary>
        [Test]
        public void Test01_Student_LoginAndAccessExam()
        {
            Setup_LoginAndGoToExam();

            Assert.IsTrue(_driver.Url.Contains("/TestAttempt/GiaoDienLamBai"),
                "URL hiện tại không khớp với giao diện làm bài thi.");

            var timeText = _driver.FindElement(By.Id("countdownDisplay")).Text;

            TestContext.WriteLine($"Thời gian ghi nhận: {timeText}");

            Assert.AreNotEqual("00:00", timeText, "Bộ đếm thời gian không hoạt động.");
        }

        /// <summary>
        /// KỊCH BẢN 02: Tính năng phím tắt (Hotkeys)
        /// Kiểm tra việc chọn đáp án qua phím số (1-4) và đánh dấu câu hỏi qua phím F
        /// </summary>
        [Test]
        public void Test02_Hotkeys_Select_Flag_Clear()
        {
            Setup_LoginAndGoToExam();

            Actions action = new Actions(_driver);

            // Giả lập nhấn phím số 1 để chọn đáp án A
            action.SendKeys("1").Perform();
            Thread.Sleep(1000);

            var firstRadio = _driver.FindElement(By.CssSelector(".answer-option input[type='radio']"));

            Assert.IsTrue(firstRadio.Selected, "Tính năng phím tắt chọn đáp án gặp lỗi.");

            // Giả lập nhấn phím F để gắn cờ đánh dấu
            action.SendKeys("f").Perform();
            Thread.Sleep(500);

            var gridItem1 = _driver.FindElement(By.CssSelector(".grid-item"));

            Assert.IsTrue(gridItem1.GetAttribute("class").Contains("flagged"),
                "Tính năng phím tắt đánh dấu câu hỏi gặp lỗi.");
        }

        /// <summary>
        /// KỊCH BẢN 03: Chống gian lận (Anti-cheat)
        /// Kiểm tra việc tự động nộp bài sau khi phát hiện 3 lần chuyển tab/cửa sổ
        /// </summary>
        [Test]
        public void Test03_AntiCheat_AutoSubmit_After_3_Violations()
        {
            Setup_LoginAndGoToExam();

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;

            for (int i = 1; i <= 3; i++)
            {
                // Mô phỏng sự kiện ẩn trình duyệt (Chuyển tab)
                js.ExecuteScript("Object.defineProperty(document, 'visibilityState', {value: 'hidden', writable: true});");
                js.ExecuteScript("document.dispatchEvent(new Event('visibilitychange'));");

                // Xác nhận thông báo cảnh báo từ hệ thống
                wait.Until(ExpectedConditions.AlertIsPresent());
                _driver.SwitchTo().Alert().Accept();

                // Trả về trạng thái hiển thị
                js.ExecuteScript("Object.defineProperty(document, 'visibilityState', {value: 'visible', writable: true});");

                Thread.Sleep(500);
            }

            // Kiểm tra sự xuất hiện của Modal kết quả sau khi vi phạm quá số lần quy định
            var resultModal = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("resultModal")));

            Assert.IsTrue(resultModal.Displayed,
                "Hệ thống không tự động nộp bài khi sinh viên vi phạm quy chế chuyển tab.");
        }

        /// <summary>
        /// KỊCH BẢN 04: Nộp bài thủ công
        /// Kiểm tra quy trình mở Modal xác nhận, thực hiện nộp và hiển thị bảng điểm
        /// </summary>
        [Test]
        public void Test04_Manual_Submit_And_Check_Result()
        {
            Setup_LoginAndGoToExam();
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // Kích hoạt Modal nộp bài
            var btnSubmit = _driver.FindElement(By.ClassName("btn-submit"));
            btnSubmit.Click();

            // Xác nhận nộp bài trong Modal
            var btnConfirm = wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("btn-confirm-submit")));

            // Sử dụng JavaScript Click để tránh lỗi các phần tử Modal đè lên nhau
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", btnConfirm);

            // Kiểm tra hiển thị kết quả sau khi nộp
            var resultModal = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("resultModal")));
            Assert.IsTrue(resultModal.Displayed, "Giao diện kết quả không hiển thị sau khi nộp bài.");

            // Kiểm tra giá trị điểm số
            var scoreElement = _driver.FindElement(By.Id("result-score"));
            string score = scoreElement.Text;

            TestContext.WriteLine($"Điểm số ghi nhận: {score}");
            Assert.IsNotEmpty(score, "Thông tin điểm số bị trống.");
        }

        /// <summary>
        /// KỊCH BẢN 05: Kiểm soát quyền làm bài
        /// Đảm bảo sinh viên không thể tái thực hiện bài thi đã hoàn thành
        /// </summary>
        [Test]
        public void Test05_Student_CannotRetakeCompletedExam()
        {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");

            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Login_UsernameOrEmail")))
                .SendKeys("Nguyendinhduy257@gmail.com");

            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            // Điều hướng sang danh sách bài thi đã hoàn thành
            var tabHoanThanh = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-tab='Đã hoàn thành']")));
            tabHoanThanh.Click();

            Thread.Sleep(1000);

            // Tìm nút trạng thái đã vô hiệu hóa
            var targetButton = FindVisibleElementAcrossPages(By.XPath("//button[contains(@class, 'btn-disabled') and contains(., 'Đã hoàn thành')]"));

            Assert.IsNotNull(targetButton, "Không tìm thấy bài thi ở trạng thái đã hoàn thành.");

            Assert.IsFalse(targetButton.Enabled,
                "Hệ thống cho phép tương tác với bài thi đã kết thúc.");
        }

        /// <summary>
        /// KỊCH BẢN 06: Bảo toàn trạng thái làm bài
        /// Kiểm tra việc lưu trữ đáp án khi sinh viên rời trang và quay lại
        /// </summary>
        [Test]
        public void Test06_ExamStatePreserved_When_NavigatingAwayAndReturning()
        {
            Setup_LoginAndGoToExam();

            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;

            // Thực hiện chọn đáp án cho câu hỏi đầu tiên
            var firstRadio = _driver.FindElement(By.CssSelector("#question-block-1 .answer-option input[type='radio']"));
            js.ExecuteScript("arguments[0].click();", firstRadio);

            string examUrl = _driver.Url;

            // Rời khỏi trang làm bài thi
            _driver.Navigate().GoToUrl($"{BaseUrl}/Student/Dashboard");
            Thread.Sleep(2000);

            // Quay trở lại trang làm bài thi
            _driver.Navigate().GoToUrl(examUrl);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("countdownDisplay")).Displayed);

            // Kiểm tra đáp án đã chọn có được giữ nguyên hay không
            Assert.IsTrue(_driver.FindElement(By.CssSelector("#question-block-1 input[type='radio']")).Selected,
                "Trạng thái làm bài không được hệ thống bảo toàn.");
        }

        /// <summary>
        /// KỊCH BẢN 07: Khóa tương tác khi xác nhận nộp bài
        /// Ngăn chặn việc thay đổi đáp án thông qua phím tắt khi Modal xác nhận đang hiển thị
        /// </summary>
        [Test]
        public void Test07_Vulnerability_CannotChangeAnswer_When_SubmitModal_IsOpen()
        {
            Setup_LoginAndGoToExam();

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // Mở Modal nộp bài
            _driver.FindElement(By.ClassName("btn-submit")).Click();

            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("submitModal")));

            // Thử nghiệm thay đổi đáp án bằng phím tắt
            new Actions(_driver).SendKeys("1").Perform();
            Thread.Sleep(500);

            var firstRadio = _driver.FindElement(By.CssSelector(".answer-option input[type='radio']"));

            Assert.IsFalse(firstRadio.Selected,
                "Sinh viên vẫn có thể thay đổi nội dung bài làm khi đang mở cửa sổ xác nhận nộp bài.");
        }

        /// <summary>
        /// KỊCH BẢN 08: Bảo mật thời gian bắt đầu
        /// Ngăn chặn truy cập vào bài thi khi chưa tới giờ quy định
        /// </summary>
        [Test]
        public void Test08_Security_CannotAccessExamBeforeStartTime()
        {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");

            _driver.FindElement(By.Id("Login_UsernameOrEmail")).SendKeys("Nguyendinhduy257@gmail.com");
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            // Tìm kiếm bài thi có nhãn chưa tới giờ
            var btnJoin = _driver.FindElements(By.XPath("//button[contains(@class, 'btn-disabled') and contains(., 'Chưa tới giờ thi')]"))
                                 .FirstOrDefault();

            Assert.IsNotNull(btnJoin, "Không tìm thấy bài thi dự kiến trong tương lai.");

            Assert.IsFalse(btnJoin.Enabled,
                "Nút vào thi vẫn cho phép tương tác dù chưa tới giờ thi quy định.");
        }

        /// <summary>
        /// KỊCH BẢN 09: Thao túng thời gian phía Client
        /// Kiểm tra tính bảo mật của bộ đếm thời gian khi sinh viên xóa bộ nhớ trình duyệt và làm mới trang
        /// </summary>
        [Test]
        public void Test09_Vulnerability_ClientSide_Time_Manipulation()
        {
            Setup_LoginAndGoToExam();

            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            string timeStrBefore = _driver.FindElement(By.Id("countdownDisplay")).Text;
            int timeBefore = ConvertTimeToSeconds(timeStrBefore);

            Thread.Sleep(5000); // Đợi thời gian trôi qua 5 giây

            // Thực hiện hành vi xóa bộ nhớ và tải lại trang để giả lập tấn công reset thời gian
            js.ExecuteScript("localStorage.clear(); sessionStorage.clear();");
            _driver.Navigate().Refresh();

            wait.Until(d => {
                string text = d.FindElement(By.Id("countdownDisplay")).Text;
                return !string.IsNullOrEmpty(text) && text != "00:00";
            });

            string timeStrAfter = _driver.FindElement(By.Id("countdownDisplay")).Text;
            int timeAfter = ConvertTimeToSeconds(timeStrAfter);

            // Thời gian sau khi tải lại trang phải ít hơn hoặc bằng thời gian cũ trừ đi thời gian chờ
            Assert.LessOrEqual(timeAfter, timeBefore - 5,
                "Bộ đếm thời gian bị reset về giá trị ban đầu - Hệ thống tồn tại lỗ hổng phía Client.");
        }

        // HÀM TIỆN ÍCH HỖ TRỢ SELENIUM

        // Chuyển đổi định dạng văn bản thời gian (MM:SS hoặc HH:MM:SS) sang tổng số giây
        private int ConvertTimeToSeconds(string timeString)
        {
            if (string.IsNullOrEmpty(timeString) || timeString == "0") return 0;

            var parts = timeString.Trim().Split(':').Select(int.Parse).ToArray();

            if (parts.Length == 2) return parts[0] * 60 + parts[1];
            if (parts.Length == 3) return parts[0] * 3600 + parts[1] * 60 + parts[2];

            return 0;
        }

        // Thực hiện tìm kiếm phần tử xuyên qua các trang trong hệ thống có phân trang (Pagination)
        private IWebElement FindVisibleElementAcrossPages(By targetLocator)
        {
            while (true)
            {
                var elements = _driver.FindElements(targetLocator);

                var visibleElement = elements.FirstOrDefault(e => e.Displayed);

                if (visibleElement != null)
                {
                    // Cuộn màn hình đến vị trí phần tử để đảm bảo khả năng tương tác
                    ((IJavaScriptExecutor)_driver)
                        .ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", visibleElement);

                    return visibleElement;
                }

                try
                {
                    var nextBtn = _driver.FindElement(By.Id("nextPage"));

                    // Dừng tìm kiếm nếu nút "Trang sau" bị vô hiệu hóa
                    if (nextBtn.GetAttribute("disabled") != null) break;

                    nextBtn.Click();

                    Thread.Sleep(1000); // Chờ tải dữ liệu trang mới
                }
                catch
                {
                    break;
                }
            }

            return _driver.FindElements(targetLocator).FirstOrDefault(e => e.Displayed);
        }
    }
}
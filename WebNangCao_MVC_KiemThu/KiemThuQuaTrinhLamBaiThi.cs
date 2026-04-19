using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Npgsql;
using NUnit.Framework;
using System.Linq;
using System;

namespace WebNangCao_MVC_KiemThu
{
    [TestFixture]
    public class KiemThuQuaTrinhLamBaiThi
    {
        private IWebDriver _driver;
        private const string BaseUrl = "https://localhost:7000";
        private const string TestEmail = "Nguyendinhduy257@gmail.com";

        // Chạy 1 lần duy nhất trước khi tất cả các Test bắt đầu
        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            ResetTestData(TestEmail);
        }

        // Chạy 1 lần duy nhất sau khi tất cả các Test đã chạy xong
        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            ResetTestData(TestEmail);
            TestContext.WriteLine("Đã dọn dẹp dữ liệu sau khi hoàn tất bộ test.");
        }

        [SetUp]
        public void SetUp()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-notifications");
            options.AddArgument("--ignore-certificate-errors");

            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        }

        [TearDown]
        public void TearDown()
        {
            if (_driver != null)
            {
                _driver.Quit();
                _driver.Dispose();
            }
        }

        // --- CÁC PHƯƠNG THỨC HỖ TRỢ ---

        private void Setup_LoginAndGoToExam(string email = TestEmail, string pass = "12345678")
        {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");

            var txtUsername = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Login_UsernameOrEmail")));
            txtUsername.Clear();
            txtUsername.SendKeys(email);
            _driver.FindElement(By.Id("Login_Password")).SendKeys(pass);
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            wait.Until(d => d.Url.Contains("/Student/Dashboard"));
            Thread.Sleep(1000);

            var btnBatDau = FindVisibleElementAcrossPages(By.XPath("//a[contains(@class, 'btn-primary-action') and contains(., 'Bắt đầu làm bài')]"));
            Assert.IsNotNull(btnBatDau, "Không tìm thấy nút 'Bắt đầu làm bài'.");
            btnBatDau.Click();

            wait.Until(d => {
                try
                {
                    var timer = d.FindElement(By.Id("countdownDisplay"));
                    return timer.Displayed && timer.Text != "00:00";
                }
                catch { return false; }
            });
        }

        private void ResetTestData(string email)
        {
            string connectionString = "Host=localhost;Port=5432;Database=EduTestDB;Username=postgres;Password=1234;SSL Mode=Disable;";
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        DELETE FROM ""ExamResultDetails"" WHERE ""ExamResultId"" IN (SELECT ""Id"" FROM ""ExamResults"" WHERE ""StudentId"" = (SELECT ""Id"" FROM ""Users"" WHERE ""Email"" = @Email));
                        DELETE FROM ""ExamResults"" WHERE ""StudentId"" = (SELECT ""Id"" FROM ""Users"" WHERE ""Email"" = @Email);";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { TestContext.WriteLine($"[DB Error]: {ex.Message}"); }
        }

        [Test]
        public void Test01_Student_LoginAndAccessExam()
        {
            Setup_LoginAndGoToExam();
            Assert.IsTrue(_driver.Url.Contains("/TestAttempt/GiaoDienLamBai"), "Không vào đúng URL làm bài.");
            var timeText = _driver.FindElement(By.Id("countdownDisplay")).Text;
            TestContext.WriteLine($"Thời gian hiện tại: {timeText}");
            Assert.AreNotEqual("00:00", timeText);
        }

        [Test]
        public void Test02_Hotkeys_Select_Flag_Clear()
        {
            Setup_LoginAndGoToExam();
            Actions action = new Actions(_driver);

            // Nhấn phím 1 để chọn đáp án
            action.SendKeys("1").Perform();
            Thread.Sleep(1000);
            var firstRadio = _driver.FindElement(By.CssSelector(".answer-option input[type='radio']"));
            Assert.IsTrue(firstRadio.Selected, "Phím '1' không hoạt động.");

            // Nhấn F để gắn cờ
            action.SendKeys("f").Perform();
            Thread.Sleep(500);
            var gridItem1 = _driver.FindElement(By.CssSelector(".grid-item"));
            Assert.IsTrue(gridItem1.GetAttribute("class").Contains("flagged"), "Phím 'F' không hoạt động.");
        }

        [Test]
        public void Test03_AntiCheat_AutoSubmit_After_3_Violations()
        {
            Setup_LoginAndGoToExam();
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;

            for (int i = 1; i <= 3; i++)
            {
                // Giả lập chuyển Tab
                js.ExecuteScript("Object.defineProperty(document, 'visibilityState', {value: 'hidden', writable: true});");
                js.ExecuteScript("document.dispatchEvent(new Event('visibilitychange'));");

                wait.Until(ExpectedConditions.AlertIsPresent());
                _driver.SwitchTo().Alert().Accept(); // Đóng cảnh báo vi phạm

                js.ExecuteScript("Object.defineProperty(document, 'visibilityState', {value: 'visible', writable: true});");
                Thread.Sleep(500);
            }

            var resultModal = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("resultModal")));
            Assert.IsTrue(resultModal.Displayed, "Không tự động nộp bài khi vi phạm 3 lần.");
        }

        [Test]
        public void Test05_Student_CannotRetakeCompletedExam()
        {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Login_UsernameOrEmail"))).SendKeys("Nguyendinhduy257@gmail.com");
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            // Chuyển Tab
            var tabHoanThanh = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-tab='Đã hoàn thành']")));
            tabHoanThanh.Click();
            Thread.Sleep(1000);

            var targetButton = FindVisibleElementAcrossPages(By.XPath("//button[contains(@class, 'btn-disabled') and contains(., 'Đã hoàn thành')]"));
            Assert.IsNotNull(targetButton, "Không thấy nút Đã hoàn thành.");
            Assert.IsFalse(targetButton.Enabled);
        }

        [Test]
        public void Test06_ExamStatePreserved_When_NavigatingAwayAndReturning()
        {
            Setup_LoginAndGoToExam();
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;

            // Chọn đáp án câu 1
            var firstRadio = _driver.FindElement(By.CssSelector("#question-block-1 .answer-option input[type='radio']"));
            js.ExecuteScript("arguments[0].click();", firstRadio);

            string examUrl = _driver.Url;
            _driver.Navigate().GoToUrl($"{BaseUrl}/Student/Dashboard");
            Thread.Sleep(2000);

            _driver.Navigate().GoToUrl(examUrl);
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("countdownDisplay")).Displayed);

            Assert.IsTrue(_driver.FindElement(By.CssSelector("#question-block-1 input[type='radio']")).Selected, "Dữ liệu không được lưu khi quay lại.");
        }

        [Test]
        public void Test07_Vulnerability_CannotChangeAnswer_When_SubmitModal_IsOpen()
        {
            Setup_LoginAndGoToExam();
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            _driver.FindElement(By.ClassName("btn-submit")).Click();
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("submitModal")));

            // Thử nhấn phím 1
            new Actions(_driver).SendKeys("1").Perform();
            Thread.Sleep(500);

            var firstRadio = _driver.FindElement(By.CssSelector(".answer-option input[type='radio']"));
            Assert.IsFalse(firstRadio.Selected, "Hacker vẫn đổi được đáp án khi đang mở Modal nộp bài!");
        }
        //kIỂM THỬ BẢO MẬT: KHÔNG TRUY CẬP ĐƯỢC BÀI THI TRƯỚC GIỜ MỞ
        [Test]
        public void Test08_Security_CannotAccessExamBeforeStartTime()
        {
            // Giả lập: Đăng nhập với tư cách học sinh
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _driver.Navigate().GoToUrl($"{BaseUrl}/Account?activeTab=login");

            // Đăng nhập
            _driver.FindElement(By.Id("Login_UsernameOrEmail")).SendKeys("Nguyendinhduy257@gmail.com");
            _driver.FindElement(By.Id("Login_Password")).SendKeys("12345678");
            _driver.FindElement(By.CssSelector("#tab-content-login .btn-submit")).Click();

            wait.Until(d => d.Url.Contains("/Student/Dashboard"));

            // Tìm bài thi có giờ mở (Start Time) trong tương lai
            // Bạn cần tìm một bài thi mà trạng thái của nút là "Chưa đến giờ" hoặc bị disabled
            var btnJoin = _driver.FindElements(By.XPath("//button[contains(@class, 'btn-disabled') and contains(., 'Chưa tới giờ thi')]"))
                                 .FirstOrDefault();

            Assert.IsNotNull(btnJoin, "Không tìm thấy bài thi nào đang ở trạng thái 'Chưa bắt đầu'.");

            // Kiểm tra tính năng bảo mật: Nút không được phép bấm
            Assert.IsFalse(btnJoin.Enabled, "LỖI BẢO MẬT: Nút tham gia bài thi vẫn ở trạng thái Enabled dù chưa đến giờ thi!");

            TestContext.WriteLine("Test08: Hệ thống đã chặn thành công truy cập trước giờ thi.");
        }
        // Kiểm thử bảo mật: Thời gian làm bài bị reset khi xóa Cache hoặc LocalStorage (Client-side Time Manipulation)
        [Test]
        public void Test09_Vulnerability_ClientSide_Time_Manipulation()
        {
            Setup_LoginAndGoToExam();
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // 1. Lấy thời gian ban đầu
            string timeStrBefore = _driver.FindElement(By.Id("countdownDisplay")).Text;
            int timeBefore = ConvertTimeToSeconds(timeStrBefore);
            TestContext.WriteLine($"Thời gian trước khi xóa Cache: {timeBefore}s");

            // 2. Đợi 5 giây (Thời gian thực tế phải giảm 5s)
            Thread.Sleep(5000);

            // 3. Tấn công: Xóa LocalStorage và Refresh
            js.ExecuteScript("localStorage.clear(); sessionStorage.clear();");
            _driver.Navigate().Refresh();

            // 4. Chờ đồng hồ hiển thị lại
            wait.Until(d => {
                string text = d.FindElement(By.Id("countdownDisplay")).Text;
                return !string.IsNullOrEmpty(text) && text != "00:00";
            });

            // 5. Lấy thời gian sau khi reload
            string timeStrAfter = _driver.FindElement(By.Id("countdownDisplay")).Text;
            int timeAfter = ConvertTimeToSeconds(timeStrAfter);
            TestContext.WriteLine($"Thời gian sau khi xóa Cache: {timeAfter}s");

            // 6. Logic kiểm tra lỗ hổng:
            // Nếu hệ thống an toàn, timeAfter phải <= (timeBefore - 5 giây thực tế)
            // Nếu timeAfter > (timeBefore - 5), nghĩa là thời gian đã bị reset về mốc ban đầu (Hack thành công)

            int expectedTime = timeBefore - 5;
            bool isVulnerable = timeAfter > expectedTime;
            // Assert để xác nhận hệ thống có lỗ hổng hay không
            Assert.LessOrEqual(timeAfter, timeBefore - 5,
            $"LỖI BẢO MẬT: Thời gian không đếm ngược (hoặc bị reset)! Trước: {timeBefore}s, Sau: {timeAfter}s");
        }

        // --- HELPER METHODS ---

        private int ConvertTimeToSeconds(string timeString)
        {
            if (string.IsNullOrEmpty(timeString) || timeString == "0") return 0;
            var parts = timeString.Trim().Split(':').Select(int.Parse).ToArray();
            if (parts.Length == 2) return parts[0] * 60 + parts[1];
            if (parts.Length == 3) return parts[0] * 3600 + parts[1] * 60 + parts[2];
            return 0;
        }

        private IWebElement FindVisibleElementAcrossPages(By targetLocator)
        {
            while (true)
            {
                var elements = _driver.FindElements(targetLocator);
                var visibleElement = elements.FirstOrDefault(e => e.Displayed);
                if (visibleElement != null)
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", visibleElement);
                    return visibleElement;
                }
                try
                {
                    var nextBtn = _driver.FindElement(By.Id("nextPage"));
                    if (nextBtn.GetAttribute("disabled") != null) break;
                    nextBtn.Click();
                    Thread.Sleep(1000);
                }
                catch { break; }
            }
            //return null;
            return _driver.FindElements(targetLocator).FirstOrDefault(e => e.Displayed);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao_MVC_Model.Data;

namespace WebNangCao_MVC_Model.Controllers
{
    public class HomeController : Controller
    {
        // Action Index: Xử lý khi người dùng truy cập trang chủ ("/")
        // Tương đương với việc render component <LandingPage /> trong React
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Action GetStarted: Xử lý logic khi bấm nút "Bắt đầu ngay"
        // Thay thế cho prop onGetStarted trong React
        public IActionResult GetStarted()
        {
            // Tham số 1: Tên Action (Hàm) -> Phải là "Index"
            // Tham số 2: Tên Controller -> "Account"
            // Tham số 3: Dữ liệu truyền đi (Query String)
            return RedirectToAction("Index", "Account", new { activeTab = "login" });
        }
        private readonly AppDbContext _context;

        // Thêm Constructor này
        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        // Action mới cho nút Xem Demo
        public async Task<IActionResult> DemoExam()
        {
            // Truy vấn lấy đề thi Demo dựa theo tên đã Seed trong Database
            var demoExam = await _context.Exams
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Answers) // Lấy luôn cả các đáp án của từng câu hỏi
                .FirstOrDefaultAsync(e => e.Title == "Bài kiểm tra Tiếng Anh B2");

            if (demoExam == null)
            {
                // Nếu chưa có dữ liệu Seed, báo lỗi nhẹ nhàng
                return Content("Dữ liệu Demo chưa được nạp. Vui lòng gọi hàm Seed() trước.");
            }

            return View(demoExam); // Đẩy dữ liệu sang giao diện
        }
        [HttpPost]
        public async Task<IActionResult> SubmitDemo(IFormCollection form)
        {
            // Lấy lại đúng đề thi Demo
            var demoExam = await _context.Exams
                .Include(e => e.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.Title == "Bài kiểm tra Tiếng Anh B2");

            if (demoExam == null) return RedirectToAction("Index");

            int correctAnswers = 0;
            // Lưu tạm các lựa chọn của khách vào Dictionary (QuestionId -> SelectedAnswerId)
            var userChoices = new Dictionary<int, int>();

            foreach (var q in demoExam.Questions)
            {
                var inputName = $"question_{q.Id}";
                if (form.ContainsKey(inputName) && int.TryParse(form[inputName], out int selectedId))
                {
                    userChoices[q.Id] = selectedId;
                    // Kiểm tra xem ID đáp án người dùng chọn có đúng không
                    if (q.Answers.Any(a => a.Id == selectedId && a.IsCorrect))
                    {
                        correctAnswers++;
                    }
                }
                else
                {
                    userChoices[q.Id] = 0; // Chưa trả lời
                }
            }

            // Gói dữ liệu gửi sang trang Review (Không lưu vào Database)
            ViewBag.Score = Math.Round((double)correctAnswers / demoExam.Questions.Count * 10, 1);
            ViewBag.CorrectAnswers = correctAnswers;
            ViewBag.UserChoices = userChoices;

            return View("DemoReview", demoExam);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebNangCao_MVC_Model.Data;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.ViewModels;

public class TestAttemptController : Controller
{
    // Thay DbContext bằng AppDbContext (tên thực tế của bạn) để truy cập trực tiếp DbSet
    private readonly AppDbContext _context;

    public TestAttemptController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /TestAttempt/GiaoDienLamBai/1
    public async Task<IActionResult> GiaoDienLamBai(int id = 1) // Để mặc định là 1 để dễ test
    {
        // Phải lôi dữ liệu ra thì View mới có cái để hiển thị
        var exam = await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
        {
            return NotFound("Không tìm thấy bài thi này!");
        }

        return View(exam); // Truyền model vào đây
    }
    //async: Cho phép phương thức chạy bất đồng bộ, giúp cải thiện hiệu suất khi truy xuất dữ liệu từ cơ sở dữ liệu.
    //async cho phép dùng await khi gọi DB
    //[FromBody]: lấy dữ liệu sao cho khớp với "body: JSON.stringify({ ExamId:1, UserAnswers:answer})
    [ValidateAntiForgeryToken] // Bảo vệ chống lại tấn công CSRF
    // CSRF: tấn công giả mạo yêu cầu từ người dùng đã xác thực
    //kẻ tấn công lừa người dùng vẫn đang có token/ cookie hợp lệ qua link độc hại, gửi yêu cầu trái phép đến server
    [HttpPost]
    public async Task<IActionResult> SubmitExam([FromBody] SubmitExamModel model)
    {
        if (model == null || model.ExamId == 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        // _context.Questions: truy vấn bảng Questionx
        // Include(q => q.Answers): lấy luôn dữ liệu liên quan từ bảng Answers để so sánh đáp án
        // Where(q => q.ExamId == model.ExamId): lọc ra các câu hỏi thuộc bài thi đang làm
        // ToListAsync(): thực thi truy vấn và trả về kết quả dưới dạng danh sách bất đồng bộ

        //1. lấy dữ liệu bài thi từ DB
        var questions = await _context.Questions
            .Include(q => q.Answers)
            //.Where(q => q.ExamId == model.ExamId)
            .Where(q => q.Exams.Any(e => e.Id == model.ExamId))
            .ToListAsync();

        int correctCount = 0; //số câu đúng
        int totalQuestions = questions.Count; //trên tổng số câu tất cả
        //khai báo 3 biến đếm theo độ khó đã đúng
        int correctEasy = 0;
        int correctMedium = 0;
        int correctHard = 0;
        //vòng lặp duyệt từng câu trả lời người dùng gửi lên
        foreach (var userAns in model.UserAnswers)
        {
            //FirstOrDefault: trả về phần tử đầu tiên
            //tìm câu hỏi có ID trùng với câu người dùng làm
            var question = questions.FirstOrDefault(q => q.Id == userAns.QuestionId);
            if (question != null)
            {
                //kiểm tra đáp án đúng
                //Any: trả về true nếu có ít nhất 1 phần tử trong tập
                // so sánh ID câu trả lời người dùng chọn với ID câu trả lời đúng trong cơ sở dữ liệu
                // Nếu có thì tăng số câu đúng +1
                var isCorrect = question.Answers.Any(a => a.Id == userAns.SelectedAnswerId && a.IsCorrect);
                if (isCorrect)
                {
                    correctCount++; //tăng tổng số câu đúng

                    //phân loại và đếm theo độ khó
                    // so sánh có phân biệt chữ hoa chữ thường
                    if (question.Difficulty == "Dễ")
                    {
                        correctEasy++;
                    }
                    else if (question.Difficulty == "Trung bình")
                    {
                        correctMedium++;
                    }
                    else if (question.Difficulty == "Khó")
                    {
                        correctHard++;
                    }
                }
            }
        }
        //tính điểm cuối dùng
        //công thức (đúng/tổng câu) * 10
        //làm tròn 2 chữ số
        double score = totalQuestions > 0 ? Math.Round((double)correctCount / totalQuestions * 10, 2) : 0.0;
        //Lưu kết quả vào DB
        // 1. Lấy ID của sinh viên đang nộp bài (từ Cookie đăng nhập)
        var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        int studentId = string.IsNullOrEmpty(userIdString) ? 0 : int.Parse(userIdString);

        // 2. Khởi tạo đối tượng cha
        var examResult = new ExamResult
        {
            StudentId = studentId,
            ExamId = model.ExamId,
            Score = score,
            SubmitTime = DateTime.UtcNow,
            ExamResultDetails = new List<ExamResultDetail>() // Khởi tạo list
        };

        // 3. Duyệt vòng lặp và add vào list của examResult
        foreach (var userAns in model.UserAnswers)
        {
            examResult.ExamResultDetails.Add(new ExamResultDetail
            {
                QuestionId = userAns.QuestionId,
                SelectedAnswerId = userAns.SelectedAnswerId
                // EF sẽ tự động điền ID sau khi lưu
            });
        }

        // 4. Chỉ cần Add thằng cha và Save một lần duy nhất
        _context.ExamResults.Add(examResult);
        await _context.SaveChangesAsync(); // QUAN TRỌNG: Lệnh này chạy xong thì PostgreSQL mới sinh ra ID thật cho examResult!
        //trả về trang JSON với thông tin điểm số, số câu đúng, tổng câu và thông điệp chúc mừng
        return Json(new
        {
            success = true,
            correctCount = correctCount,
            totalQuestions = totalQuestions,
            score = score,
            // Truyền thêm 3 biến này xuống Frontend
            correctEasy = correctEasy,
            correctMedium = correctMedium,
            correctHard = correctHard,
            //lấy ID thật từ DB sau khi SaveChange
            resultId = examResult.Id,
            message = "Chúc mừng bạn đã hoàn thành bài thi!"
        });
    }
    // HÀM XỬ LÝ TRANG XEM LẠI CHI TIẾT
    [HttpGet]
    public async Task<IActionResult> ReviewResult(int resultId)
    {
        // 1. Lấy kết quả tổng quát
        var result = await _context.ExamResults.FindAsync(resultId);
        if (result == null) return NotFound("Không tìm thấy kết quả!");

        // 2. Lấy chi tiết các câu đã làm (Để biết user đã chọn đáp án nào)
        var userDetails = await _context.ExamResultDetails
            .Where(d => d.ExamResultId == resultId)
            .ToDictionaryAsync(d => d.QuestionId, d => d.SelectedAnswerId);

        // 3. Lấy danh sách câu hỏi và đáp án của đề thi
        var questions = await _context.Questions
            .Include(q => q.Answers)
            //.Where(q => q.ExamId == result.ExamId)
            .Where(q => q.Exams.Any(e => e.Id == result.ExamId))
            .ToListAsync();

        // 4. Đổ dữ liệu vào ViewModel
        var viewModel = new ReviewResultViewModel
        {
            ResultId = result.Id,
            Score = result.Score,
            Questions = questions.Select(q => new ReviewQuestionViewModel
            {
                QuestionId = q.Id,
                Content = q.Content,
                // Lấy ID đáp án đã chọn từ Dictionary userDetails (nếu không chọn thì trả về 0)
                SelectedAnswerId = userDetails.ContainsKey(q.Id) ? userDetails[q.Id] : 0,

                Answers = q.Answers.Select(a => new ReviewAnswerViewModel
                {
                    AnswerId = a.Id,
                    Content = a.Content,
                    IsCorrectAnswer = a.IsCorrect
                }).ToList()
            }).ToList()
        };

        return View(viewModel);
    }
}
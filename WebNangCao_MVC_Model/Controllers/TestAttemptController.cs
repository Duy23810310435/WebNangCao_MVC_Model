using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebNangCao_MVC_Model.Data;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.ViewModels;

public class TestAttemptController : Controller
{
    private readonly AppDbContext _context;

    public TestAttemptController(AppDbContext context)
    {
        _context = context;
    }

    //
    // 1. TẠO PHIÊN THI MỚI (DÀNH CHO BÀI TỰ TẠO TỪ THÍ SINH - ĐÂY LÀ BÀI THI CÁ NHÂN CỦA THÍ SINH)
    // 
    [HttpGet]
    public async Task<IActionResult> CreateTestSession(int examId, bool shuffleQ, bool shuffleA, bool antiCheat)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int studentId = string.IsNullOrEmpty(userIdString) ? 0 : int.Parse(userIdString);

            // 1. Lấy danh sách câu hỏi từ bài thi gốc (examId = 20)
            var studentQuestions = await _context.Questions
                .Include(q => q.Answers) // Quan trọng: Phải lấy cả đáp án để bài thi có dữ liệu
                .Where(q => q.Exams.Any(e => e.Id == examId))
                .ToListAsync();

            if (studentQuestions == null || !studentQuestions.Any())
            {
                // Nếu vào đây là do không tìm thấy câu hỏi nào thuộc examId=20
                return RedirectToAction("Dashboard", "Student");
            }

            // 2. BƯỚC THẦN THÁNH: Đánh dấu câu hỏi là "CŨ"
            foreach (var q in studentQuestions)
            {
                // Attach báo cho EF Core: "Câu hỏi này đã có trong DB, đừng INSERT mới"
                _context.Questions.Attach(q);
                _context.Entry(q).State = EntityState.Unchanged;
            }

            // 3. Tạo bài Exam mới (Bản sao cho sinh viên làm)
            var newExam = new Exam
            {
                Title = "Tự ôn tập - " + DateTime.Now.ToString("HH:mm dd/MM"),
                SubjectName = "Luyện tập cá nhân",
                IsSelfCreated = true,
                StudentId = studentId,
                IsActive = true,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(1),
                Duration = studentQuestions.Count * 2,
                Questions = studentQuestions // EF sẽ chỉ tạo liên kết trong bảng trung gian ExamQuestion
            };

            _context.Exams.Add(newExam);

            // Dòng này sẽ thực hiện 2 việc:
            // - Insert 1 dòng vào bảng Exams
            // - Insert N dòng vào bảng trung gian ExamQuestion (Dùng ID câu hỏi cũ)
            await _context.SaveChangesAsync();

            // 4. Vào phòng thi
            return RedirectToAction("GiaoDienLamBai", new
            {
                testId = newExam.Id, // Dùng ID của bài thi VỪA TẠO
                mode = "self_created",
                antiCheat = antiCheat,
                shuffleQ = shuffleQ,
                shuffleA = shuffleA
            });
        }
        catch (Exception ex)
        {
            // TẠM THỜI: Đừng Redirect để xem lỗi thật sự là gì
            // Sau khi chạy ngon thì hãy mở lại dòng Redirect
            //return Content("Lỗi lưu Database: " + ex.InnerException?.Message ?? ex.Message);

            return RedirectToAction("Dashboard", "Student");
        }
    }
    // 
    // 2. GIAO DIỆN LÀM BÀI 
    // 
    [HttpGet]
    public async Task<IActionResult> GiaoDienLamBai(int id, string mode = "", bool antiCheat = false, bool shuffleQ = false, bool shuffleA = false)
    {
        var exam = await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
        {
            return NotFound("Không tìm thấy bài thi này!");
        }

        // Đẩy cấu hình sang View bằng ViewBag để JS ở Frontend bắt được và xử lý
        ViewBag.Mode = mode;
        ViewBag.AntiCheat = antiCheat;
        ViewBag.ShuffleQ = shuffleQ;
        ViewBag.ShuffleA = shuffleA;

        return View(exam);
    }

    // 
    // 3. NỘP BÀI THI 
    // 
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> SubmitExam([FromBody] SubmitExamModel model)
    {
        if (model == null || model.ExamId == 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        var questions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.Exams.Any(e => e.Id == model.ExamId))
            .ToListAsync();

        int correctCount = 0;
        int totalQuestions = questions.Count;
        int correctEasy = 0;
        int correctMedium = 0;
        int correctHard = 0;

        foreach (var userAns in model.UserAnswers)
        {
            var question = questions.FirstOrDefault(q => q.Id == userAns.QuestionId);
            if (question != null)
            {
                var isCorrect = question.Answers.Any(a => a.Id == userAns.SelectedAnswerId && a.IsCorrect);
                if (isCorrect)
                {
                    correctCount++;

                    if (question.Difficulty == "Dễ") correctEasy++;
                    else if (question.Difficulty == "Trung bình") correctMedium++;
                    else if (question.Difficulty == "Khó") correctHard++;
                }
            }
        }

        double score = totalQuestions > 0 ? Math.Round((double)correctCount / totalQuestions * 10, 2) : 0.0;

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int studentId = string.IsNullOrEmpty(userIdString) ? 0 : int.Parse(userIdString);

        var examResult = new ExamResult
        {
            StudentId = studentId,
            ExamId = model.ExamId,
            Score = score,
            SubmitTime = DateTime.UtcNow,
            ExamResultDetails = new List<ExamResultDetail>()
        };

        foreach (var userAns in model.UserAnswers)
        {
            examResult.ExamResultDetails.Add(new ExamResultDetail
            {
                QuestionId = userAns.QuestionId,
                SelectedAnswerId = userAns.SelectedAnswerId
            });
        }

        _context.ExamResults.Add(examResult);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            correctCount = correctCount,
            totalQuestions = totalQuestions,
            score = score,
            correctEasy = correctEasy,
            correctMedium = correctMedium,
            correctHard = correctHard,
            resultId = examResult.Id,
            message = "Chúc mừng bạn đã hoàn thành bài thi!"
        });
    }

    //
    // 4. XEM LẠI KẾT QUẢ CHI TIẾT (Đã truyền thêm IsSelfCreated và TestId)
    // 
    [HttpGet]
    public async Task<IActionResult> ReviewResult(int resultId)
    {
        var result = await _context.ExamResults.FindAsync(resultId);
        if (result == null) return NotFound("Không tìm thấy kết quả!");

        var userDetails = await _context.ExamResultDetails
            .Where(d => d.ExamResultId == resultId)
            .ToDictionaryAsync(d => d.QuestionId, d => d.SelectedAnswerId);

        var questions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.Exams.Any(e => e.Id == result.ExamId))
            .ToListAsync();

        // Lấy thông tin bài Exam để xác định nguồn gốc (Ai tạo)
        var exam = await _context.Exams.FindAsync(result.ExamId);

        // Mẹo: Cần thuộc tính IsSelfCreated trong bảng Exams để xác định. 
        // Ở đây tôi dùng biến giả lập, bạn hãy map với cột thực tế trong DB nhé.
        bool isSelfCreated = true; // Sửa thành: exam.IsSelfCreated (nếu có cột này)

        var viewModel = new ReviewResultViewModel
        {
            ResultId = result.Id,
            Score = (int)result.Score, // Ép kiểu tùy theo ViewModel của bạn
            TestId = result.ExamId,    // <-- TRUYỀN ID BÀI THI LÊN VIEW
            IsSelfCreated = isSelfCreated, // <-- TRUYỀN CỜ PHÂN BIỆT LÊN VIEW
            Questions = questions.Select(q => new ReviewQuestionViewModel
            {
                QuestionId = q.Id,
                Content = q.Content,
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

    // 
    // 5. XÓA BÀI THI VÀ CÂU HỎI (API gọi từ Frontend)
    //
    [HttpPost]
    public async Task<IActionResult> DeleteTestAndQuestions(int testId)
    {
        try
        {
            // Lấy bài thi kèm theo các câu hỏi của nó
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == testId);

            if (exam != null)
            {
                // Xóa toàn bộ câu hỏi (EF Core sẽ tự động xóa đáp án nếu cấu hình Cascade Delete)
                if (exam.Questions != null && exam.Questions.Any())
                {
                    _context.Questions.RemoveRange(exam.Questions);
                }

                // Xóa bài thi
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy bài thi trong Database." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
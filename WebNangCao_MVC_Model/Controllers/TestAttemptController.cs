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
    // 1. TẠO PHIÊN THI MỚI 
    // 
    [HttpGet]
    public async Task<IActionResult> CreateTestSession(int examId, bool shuffleQ, bool shuffleA, bool antiCheat)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int studentId = string.IsNullOrEmpty(userIdString) ? 0 : int.Parse(userIdString);

            var studentQuestions = await _context.Questions
                .Include(q => q.Answers)
                .Where(q => q.Exams.Any(e => e.Id == examId))
                .ToListAsync();

            if (studentQuestions == null || !studentQuestions.Any())
            {
                return RedirectToAction("Dashboard", "Student");
            }

            foreach (var q in studentQuestions)
            {
                _context.Questions.Attach(q);
                _context.Entry(q).State = EntityState.Unchanged;
            }

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
                Questions = studentQuestions
            };

            _context.Exams.Add(newExam);
            await _context.SaveChangesAsync();

            return RedirectToAction("GiaoDienLamBai", new
            {
                id = newExam.Id,
                mode = "self_created",
                antiCheat = antiCheat,
                shuffleQ = shuffleQ,
                shuffleA = shuffleA
            });
        }
        catch (Exception ex)
        {
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

        if (exam == null) return NotFound("Không tìm thấy bài thi này!");

        ViewBag.Mode = mode;
        ViewBag.AntiCheat = antiCheat;
        ViewBag.ShuffleQ = shuffleQ;
        ViewBag.ShuffleA = shuffleA;

        return View(exam);
    }

    // 
    // 3. NỘP BÀI THI (Phân loại xử lý lưu trữ)
    // 
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> SubmitExam([FromBody] SubmitExamModel model)
    {
        if (model == null || model.ExamId == 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        var examInfo = await _context.Exams.FirstOrDefaultAsync(e => e.Id == model.ExamId);
        if (examInfo == null) return Json(new { success = false, message = "Không tìm thấy bài thi" });

        bool isSelfCreated = examInfo.IsSelfCreated;

        var questions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.Exams.Any(e => e.Id == model.ExamId))
            .ToListAsync();

        int correctCount = 0;
        int totalQuestions = questions.Count;
        int correctEasy = 0, correctMedium = 0, correctHard = 0;

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

        int resultId = 0;
        string note = "";

        if (!isSelfCreated)
        {
            // 3.1 BÀI THI CỦA LỚP: Lưu vào Database vĩnh viễn
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
            resultId = examResult.Id;
            note = "Kết quả đã được ghi nhận vào lịch sử.";
        }
        else
        {
            // 3.2 BÀI THI CÁ NHÂN: KHÔNG lưu vào DB. Dùng bộ nhớ TempData tạm thời
            note = "Bài tự ôn tập, kết quả không lưu vào lịch sử để tránh rác thông số trang chủ.";
            TempData["PersonalAnswers_" + model.ExamId] = System.Text.Json.JsonSerializer.Serialize(model.UserAnswers);

            // Không xóa bản sao Đề thi ở đây nữa, giữ lại để hàm ReviewPersonalResult có thể gọi lấy câu hỏi lên!
        }

        return Json(new
        {
            success = true,
            correctCount = correctCount,
            totalQuestions = totalQuestions,
            score = score,
            correctEasy = correctEasy,
            correctMedium = correctMedium,
            correctHard = correctHard,
            resultId = resultId,
            examId = model.ExamId,          // Trả về ID đề thi để JS xử lý
            isSelfCreated = isSelfCreated,  // Trả cờ nhận biết cho JS
            message = note
        });
    }

    // 
    // 4. XEM LẠI KẾT QUẢ CHI TIẾT (BÀI THI LỚP)
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

        var exam = await _context.Exams.FindAsync(result.ExamId);

        var viewModel = new ReviewResultViewModel
        {
            ResultId = result.Id,
            Score = (int)result.Score,
            TestId = result.ExamId,
            IsSelfCreated = false,
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
    // 4.1 XEM LẠI KẾT QUẢ ĐẶC BIỆT DÀNH CHO BÀI THI CÁ NHÂN (DÙNG TEMPDATA)
    // 
    [HttpGet]
    public async Task<IActionResult> ReviewPersonalResult(int examId)
    {
        var exam = await _context.Exams.FindAsync(examId);
        if (exam == null) return NotFound("Đề thi này không còn tồn tại!");

        var questions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.Exams.Any(e => e.Id == examId))
            .ToListAsync();

        var userDetails = new Dictionary<int, int>();
        var tempAnswers = TempData["PersonalAnswers_" + examId] as string;

        if (!string.IsNullOrEmpty(tempAnswers))
        {
            var parsedAnswers = System.Text.Json.JsonSerializer.Deserialize<List<TempUserAnswer>>(tempAnswers);
            if (parsedAnswers != null)
            {
                userDetails = parsedAnswers.ToDictionary(a => a.QuestionId, a => a.SelectedAnswerId);
            }
            TempData.Keep("PersonalAnswers_" + examId); // Giữ bộ nhớ nếu thí sinh F5 tải lại trang
        }

        // Tính lại điểm
        int correctCount = 0;
        foreach (var q in questions)
        {
            if (userDetails.TryGetValue(q.Id, out int selectedId))
            {
                if (q.Answers.Any(a => a.Id == selectedId && a.IsCorrect)) correctCount++;
            }
        }
        int score = questions.Count > 0 ? (int)Math.Round((double)correctCount / questions.Count * 10) : 0;

        var viewModel = new ReviewResultViewModel
        {
            ResultId = 0, // Không có ResultID trong DB
            Score = score,
            TestId = examId,
            IsSelfCreated = true,
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

        // Render ra cùng View "ReviewResult" như bài thi bình thường
        return View("ReviewResult", viewModel);
    }

    // Class phụ trợ để hứng dữ liệu từ TempData
    public class TempUserAnswer
    {
        public int QuestionId { get; set; }
        public int SelectedAnswerId { get; set; }
    }

    // 
    // 5. XÓA BÀI THI VÀ CÂU HỎI
    //
    [HttpPost]
    public async Task<IActionResult> DeleteTestAndQuestions(int testId)
    {
        try
        {
            var exam = await _context.Exams.Include(e => e.Questions).FirstOrDefaultAsync(e => e.Id == testId);
            if (exam != null)
            {
                if (exam.Questions != null && exam.Questions.Any())
                {
                    _context.Questions.RemoveRange(exam.Questions);
                }
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
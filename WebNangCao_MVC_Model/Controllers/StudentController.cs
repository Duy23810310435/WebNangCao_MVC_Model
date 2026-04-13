using MathNet.Numerics.Distributions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Security.Claims;
using WebNangCao_MVC_Model.Data;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.ViewModels;

namespace WebNangCao_MVC_Model.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DASHBOARD - HIỂN THỊ TRANG CHỦ THÍ SINH
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Account");
            }

            int studentId = int.Parse(userIdString);

            // --- MỚI: Lấy ID đề thi cá nhân mới nhất để phục vụ nút bấm ở Modal ---
            var latestExam = await _context.Exams
                .Where(e => e.StudentId == studentId && e.IdGroup == null)
                .OrderByDescending(e => e.Id)
                .Select(e => new { e.Id, e.Title })
                .FirstOrDefaultAsync();

            ViewBag.LatestExamId = latestExam?.Id;
            ViewBag.LatestExamTitle = latestExam?.Title;
            // -------------------------------------------------------------------

            var joinedClassesData = await _context.UserGroups
                .Where(ug => ug.UserId == studentId)
                .Include(ug => ug.Group)
                    .ThenInclude(g => g.Teacher)
                .Select(ug => new JoinedClassViewModel
                {
                    IdGroup = ug.Group.Id,
                    ClassName = ug.Group.GroupName,
                    TeacherName = ug.Group.Teacher != null ? ug.Group.Teacher.FullName : "Chưa phân công",
                    ExamCount = _context.Exams.Count(e => e.IdGroup == ug.Group.Id)
                })
                .ToListAsync();

            var studentGroupIds = joinedClassesData.Select(c => c.IdGroup).ToList();

            // Lấy các bài thi đang hoạt động (Active)
            var rawExams = await _context.Exams
                .Include(e => e.Questions)
                .Where(e => e.IsActive && (studentGroupIds.Contains(e.IdGroup ?? 0) || e.StudentId == studentId))
                .ToListAsync();

            var userResults = await _context.ExamResults
                .Where(r => r.StudentId == studentId)
                .ToListAsync();
            var completedExamIds = userResults.Select(r => r.ExamId).ToList();

            var examListVM = new List<ExamItemViewModel>();
            var currentTime = DateTime.UtcNow;

            foreach (var exam in rawExams)
            {
                string status = "";
                // Xác định xem đây có phải bài thi cá nhân (không thuộc group nào) hay không
                bool isPersonalExam = (exam.IdGroup == null);

                // Nếu KHÔNG phải bài cá nhân và đã làm rồi -> Đã hoàn thành
                if (!isPersonalExam && completedExamIds.Contains(exam.Id))
                {
                    status = "Đã hoàn thành";
                }
                else if (currentTime < exam.StartTime)
                {
                    status = "Sắp tới";
                }
                else if (currentTime >= exam.StartTime && currentTime <= exam.EndTime)
                {
                    status = "Có thể làm";
                }
                else
                {
                    // Quá hạn EndTime. Nếu là bài cá nhân, người dùng có thể vẫn cho phép "Có thể làm"
                    status = isPersonalExam ? "Có thể làm" : "Đã hoàn thành";
                }

                string dominantDifficulty = "Chưa xác định";
                if (exam.Questions != null && exam.Questions.Any())
                {
                    var topDifficulty = exam.Questions
                        .Where(q => !string.IsNullOrWhiteSpace(q.Difficulty))
                        .GroupBy(q => q.Difficulty)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(topDifficulty))
                    {
                        string normalizedTop = topDifficulty.ToLower().Trim();
                        switch (normalizedTop)
                        {
                            case "dễ": case "easy": case "de": dominantDifficulty = "Dễ"; break;
                            case "trung bình": case "medium": case "trung binh": dominantDifficulty = "Trung bình"; break;
                            case "khó": case "hard": case "kho": dominantDifficulty = "Khó"; break;
                            default: dominantDifficulty = "Chưa xác định"; break;
                        }
                    }
                }

                string subjectName = exam.IdGroup.HasValue
                    ? joinedClassesData.FirstOrDefault(c => c.IdGroup == exam.IdGroup)?.ClassName ?? "Môn học chung"
                    : "Đề thi tự ôn luyện (Cá nhân)";

                examListVM.Add(new ExamItemViewModel
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    StartTime = exam.StartTime,
                    Duration = exam.Duration,
                    TotalQuestions = exam.Questions?.Count ?? 0,
                    Status = status,
                    IdGroup = exam.IdGroup ?? 0,
                    SubjectName = subjectName,
                    Difficulty = dominantDifficulty
                });
            }

            var model = new StudentDashboardViewModel
            {
                TotalExams = rawExams.Count,
                CompletedExams = userResults.Count,
                UpcomingExams = examListVM.Count(e => e.Status == "Sắp tới"),
                AverageScore = userResults.Any() ? Math.Round(userResults.Average(r => r.Score), 1) : 0,
                Exams = examListVM.OrderBy(e => e.StartTime).ToList(),
                JoinedClasses = joinedClassesData
            };

            return View(model);
        }

        // ==========================================
        // 2. JOIN CLASS - XỬ LÝ KHI BẤM "THAM GIA LỚP"
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> JoinClass(int groupId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Index", "Account");

            int studentId = int.Parse(userIdString);

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy lớp học với mã '{groupId}'.";
                return RedirectToAction(nameof(Dashboard));
            }

            var alreadyJoined = await _context.UserGroups
                .AnyAsync(ug => ug.UserId == studentId && ug.GroupId == groupId);

            if (alreadyJoined)
            {
                TempData["ErrorMessage"] = "Bạn đã tham gia lớp học này rồi!";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.UserGroups.Add(new UserGroup { UserId = studentId, GroupId = groupId, JoinedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tham gia lớp học thành công.";
            return RedirectToAction(nameof(Dashboard));
        }

        // ==========================================
        // 3. UPLOAD EXCEL - TẠO ĐỀ THI CÁ NHÂN 
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> UploadPersonalExam(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, errors = new[] { new { dong = 0, loi = "Không tìm thấy file." } } });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? studentId = int.TryParse(userIdString, out int parsedId) ? parsedId : null;

            var validQuestionsToInsert = new List<Question>();
            var errors = new List<object>();

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;
                IWorkbook workbook = Path.GetExtension(excelFile.FileName).ToLower() == ".xls"
                    ? new HSSFWorkbook(stream) : new XSSFWorkbook(stream);

                ISheet sheet = workbook.GetSheetAt(0);
                DataFormatter formatter = new DataFormatter();
                var currentBlock = new List<(int RowIndex, string Text)>();
                int emptyRows = 0;

                async Task ValidateBlock()
                {
                    if (currentBlock.Count < 3)
                    {
                        errors.Add(new { dong = currentBlock[0].RowIndex + 1, loi = "Thiếu đáp án." });
                        return;
                    }
                    var q = new Question
                    {
                        Content = currentBlock[0].Text.Trim(),
                        Difficulty = "Chưa phân loại",
                        Answers = new List<Answer>()
                    };
                    bool hasCorrect = false;
                    for (int j = 1; j < currentBlock.Count; j++)
                    {
                        string txt = currentBlock[j].Text.Trim();
                        if (txt.StartsWith("1."))
                        {
                            hasCorrect = true;
                            q.Answers.Add(new Answer { Content = txt.Substring(2).Trim(), IsCorrect = true });
                        }
                        else if (txt.StartsWith("0."))
                        {
                            q.Answers.Add(new Answer { Content = txt.Substring(2).Trim(), IsCorrect = false });
                        }
                    }
                    if (hasCorrect) validQuestionsToInsert.Add(q);
                    else errors.Add(new { dong = currentBlock[0].RowIndex + 1, loi = "Thiếu đáp án đúng." });
                }

                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    var val = formatter.FormatCellValue(row?.GetCell(0)).Trim();
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        emptyRows++;
                        if (emptyRows >= 2 && currentBlock.Any()) { await ValidateBlock(); currentBlock.Clear(); }
                    }
                    else { emptyRows = 0; currentBlock.Add((i, val)); }
                }
                if (currentBlock.Any()) await ValidateBlock();
            }

            if (errors.Any()) return Json(new { success = false, errors = errors });

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var newExam = new Exam
                    {
                        // Title là chuỗi (string)
                        Title = $"Đề cá nhân_{DateTime.Now:ddMMyy_HHmm}",
                        SubjectName = "Ôn tập tự do",
                        Difficulty = "Trung bình",
                        IsActive = false,

                        //DB PostGreSQL chỉ nhận loại thời gian là UtcNow
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow.AddYears(1),

                        Duration = 60,
                        StudentId = studentId,
                        Questions = validQuestionsToInsert
                    };
                    _context.Exams.Add(newExam);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, examId = newExam.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    // Moi lỗi thật sự từ InnerException (nếu có)
                    string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                    return Json(new { success = false, errors = new[] { new { dong = 0, loi = "Lỗi Database: " + realError } } });
                }
            }
        }

        // ==========================================
        // 4. GIAO DIỆN PHÂN LOẠI CÂU HỎI (ĐÃ ĐƯỢC CHIA TÁCH)
        // ==========================================

        // 4.1. Trang Kho Câu Hỏi (View: ClassifyExam.cshtml)
        [HttpGet]
        public async Task<IActionResult> ClassifyExam(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return NotFound("Không tìm thấy bộ đề thi này!");

            return View(exam);
        }

        // 4.2. Trang Bàn Kéo Thả (View: PhanLoaiCauHoi.cshtml)
        [HttpGet]
        public async Task<IActionResult> PhanLoaiCauHoi(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return NotFound("Không tìm thấy bộ đề thi này!");

            return View(exam);
        }

        // ==========================================
        // 5. API LƯU DỮ LIỆU KÉO THẢ )
        // ==========================================

        // CLASS Model dùng để hứng dữ liệu AJAX
        public class QuestionClassificationViewModel
        {
            public int QuestionId { get; set; }
            public string Difficulty { get; set; }
        }

        // Thêm Action để lưu câu hỏi đã phân loại độ khó từ: "PhanLoaiCauHoi.cshtml" xuống DataBase
        // [FromBody]: Nó tự động map cái chuỗi JSON từ JS thành một List<C# Object>
        [HttpPost]
        public async Task<IActionResult> SaveClassification([FromBody] List<QuestionClassificationViewModel> classificationData)
        {
            if (classificationData == null || !classificationData.Any())
            {
                return Json(new { success = false, message = "Không có dữ liệu phân loại" });
            }

            try
            {
                // lấy danh sách Id các câu hỏi cần cập nhật
                var QuestionIds = classificationData.Select(c => c.QuestionId).ToList();

                // lấy các câu hỏi từ DataBase lên (chỉ lấy những câu hỏi trong danh sách)
                var questionsToUpdate = await _context.Questions
                    .Where(q => QuestionIds.Contains(q.Id))
                    .ToListAsync();

                // cập nhật độ khó cho từng câu được gắn là "Chưa phân loại" trên DB
                foreach (var q in questionsToUpdate)
                {
                    var updateInfo = classificationData.FirstOrDefault(c => c.QuestionId == q.Id);
                    if (updateInfo != null)
                    {
                        q.Difficulty = updateInfo.Difficulty;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã lưu phân loại độ khó thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu: " + ex.Message });
            }
        }
        //Logic xóa câu hỏi chưa được phân loại độ khó 
        public class DeleteQuestionRequest
        {
            public List<int> Ids { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteQuestions([FromBody] DeleteQuestionRequest request)
        {
            if (request == null || request.Ids == null || !request.Ids.Any())
            {
                return Json(new { success = false, message = "Không có câu hỏi nào được chọn." });
            }

            try
            {
                // 1. Tìm các câu hỏi cần xóa
                var questionsToDelete = _context.Questions
                    .Where(q => request.Ids.Contains(q.Id))
                    .ToList();

                if (!questionsToDelete.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy dữ liệu trong hệ thống." });
                }

                // 2. Xóa các câu hỏi
                //xóa cả bảng Questions lẫn bảng Answers liên quan
                var questionIds = questionsToDelete.Select(q => q.Id).ToList();
                var answersToDelete = _context.Answers.Where(a => questionIds.Contains(a.QuestionId));
                _context.Answers.RemoveRange(answersToDelete); // Xóa đáp án trước
                _context.Questions.RemoveRange(questionsToDelete); // Xóa câu hỏi sau
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã xóa thành công {questionsToDelete.Count} câu hỏi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        //Logic sửa (Edit) câu hỏi chưa phân loại độ khó
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var question = _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Exams) // Load danh sách đề thi chứa câu hỏi này
                .FirstOrDefault(q => q.Id == id);

            if (question == null) return NotFound();

            // Lấy Id của đề thi đầu tiên chứa câu hỏi này truyền xuống ViewBag
            ViewBag.ExamId = question.Exams.FirstOrDefault()?.Id ?? 0;

            return View(question);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Question model, List<Answer> answers, int examId)
        {
            try
            {
                var existingQuestion = _context.Questions
                    .Include(q => q.Answers)
                    .Include(q => q.Exams)
                    .FirstOrDefault(q => q.Id == model.Id);

                if (existingQuestion == null) return NotFound();

                // Cập nhật nội dung câu hỏi
                existingQuestion.Content = model.Content;
                existingQuestion.Difficulty = model.Difficulty;

                // Cập nhật từng đáp án
                foreach (var ans in answers)
                {
                    var dbAns = existingQuestion.Answers.FirstOrDefault(a => a.Id == ans.Id);
                    if (dbAns != null)
                    {
                        dbAns.Content = ans.Content;
                        dbAns.IsCorrect = ans.IsCorrect;
                    }
                }

                await _context.SaveChangesAsync();

                // Điều hướng về kho câu hỏi của đề thi hiện tại
                int targetExamId = examId != 0 ? examId : (existingQuestion.Exams.FirstOrDefault()?.Id ?? 0);
                return RedirectToAction("ClassifyExam", new { examId = targetExamId });
            }
            catch
            {
                ViewBag.ExamId = examId; // Giữ lại ID nếu lưu thất bại
                return View(model);
            }
        }
    }
}
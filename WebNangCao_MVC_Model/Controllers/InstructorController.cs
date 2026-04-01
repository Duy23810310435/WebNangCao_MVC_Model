using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao_MVC_Model.Data;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.ViewModels;
using System.Security.Claims;

namespace WebNangCao_MVC_Model.Controllers
{
    [Authorize(Roles = "instructor")]
    public class InstructorController : Controller
    {
        private readonly AppDbContext _context;

        public InstructorController(AppDbContext context)
        {
            _context = context;
        }

        // Get current instructor ID from Claims
        private int GetInstructorId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int id) ? id : 0;
        }

        // ==========================================
        // DASHBOARD
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            int instructorId = GetInstructorId();
            if (instructorId == 0)
                return RedirectToAction("Index", "Account");

            // Get all groups owned by this instructor
            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            // Calculate statistics
            var totalClasses = instructorGroups.Count;
            var totalStudents = await _context.UserGroups
                .Where(ug => instructorGroups.Contains(ug.GroupId) && ug.User.Role != "instructor")
                .CountAsync();

            var exams = await _context.Exams
                .Where(e => instructorGroups.Contains(e.IdGroup))
                .Include(e => e.Questions)
                .ToListAsync();

            var totalExams = exams.Count;
            var completedExams = exams.Count(e => e.EndTime < DateTime.UtcNow);
            var pendingExams = exams.Count(e => e.StartTime <= DateTime.UtcNow && e.EndTime > DateTime.UtcNow);

            var examIds = exams.Select(e => e.Id).ToList();
            var allResults = await _context.ExamResults
                .Where(er => examIds.Contains(er.ExamId))
                .Include(er => er.Student)
                .ToListAsync();

            var averageScore = allResults.Any() ? allResults.Average(er => er.Score) : 0;
            var passCount = allResults.Count(er => er.Score >= 5);
            var passRate = allResults.Any() ? Math.Round((passCount * 100.0) / allResults.Count(), 1) : 0;

            var examsThisWeek = exams.Count(e =>
                e.StartTime >= DateTime.UtcNow.AddDays(-7) &&
                e.StartTime <= DateTime.UtcNow.AddDays(7));

            // Get classes
            var classes = await _context.Groups
                .Where(g => instructorGroups.Contains(g.Id))
                .Include(g => g.UserGroups)
                .Select(g => new ClassItemViewModel
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    StudentCount = g.UserGroups.Count(ug => ug.User.Role != "instructor")
                })
                .ToListAsync();

            // Get recent exams with submitted count
            var recentExams = exams
                .OrderByDescending(e => e.StartTime)
                .Take(5)
                .Select(e => new ExamItemViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    SubjectName = _context.Groups
                        .Where(g => g.Id == e.IdGroup)
                        .Select(g => g.GroupName)
                        .FirstOrDefault() ?? string.Empty,
                    GroupName = _context.Groups
                        .Where(g => g.Id == e.IdGroup)
                        .Select(g => g.GroupName)
                        .FirstOrDefault() ?? string.Empty,
                    IdGroup = e.IdGroup,
                    StartTime = e.StartTime,
                    Duration = e.Duration,
                    TotalQuestions = e.Questions?.Count ?? 0,
                    Status = e.EndTime < DateTime.UtcNow ? "Đã hoàn thành" :
                             (e.StartTime <= DateTime.UtcNow && e.EndTime > DateTime.UtcNow ? "Đang diễn ra" : "Đã lên lịch")
                })
                .ToList();

            // Get recent gradings
            var recentGradings = allResults
                .OrderByDescending(r => r.SubmitTime)
                .Take(5)
                .Select(r => new RecentGradingViewModel
                {
                    ResultId = r.Id,
                    StudentName = r.Student?.FullName ?? "Unknown",
                    ExamTitle = _context.Exams
                        .Where(e => e.Id == r.ExamId)
                        .Select(e => e.Title)
                        .FirstOrDefault() ?? string.Empty,
                    Score = r.Score,
                    SubmitTimeAgo = GetTimeAgoString(r.SubmitTime),
                    Status = r.Score >= 5 ? "graded" : "pending"
                })
                .ToList();

            var model = new InstructorDashboardViewModel
            {
                TotalClasses = totalClasses,
                TotalStudents = totalStudents,
                TotalExams = totalExams,
                CompletedExams = completedExams,
                AverageStudentScore = Math.Round(averageScore, 1),
                PassRate = passRate,
                ExamsThisWeek = examsThisWeek,
                PendingExams = pendingExams,
                Classes = classes,
                RecentExams = recentExams,
                RecentGradings = recentGradings
            };

            return View(model);
        }

        private string GetTimeAgoString(DateTime dateTime)
        {
            var timespan = DateTime.UtcNow - dateTime;
            if (timespan.TotalMinutes < 1)
                return "Vừa xong";
            if (timespan.TotalMinutes < 60)
                return $"{(int)timespan.TotalMinutes} phút trước";
            if (timespan.TotalHours < 24)
                return $"{(int)timespan.TotalHours} giờ trước";
            if (timespan.TotalDays < 7)
                return $"{(int)timespan.TotalDays} ngày trước";
            return dateTime.ToString("dd/MM/yyyy");
        }

        // ==========================================
        // CLASS MANAGEMENT - LIST
        // ==========================================
        public async Task<IActionResult> Classes()
        {
            int instructorId = GetInstructorId();
            if (instructorId == 0)
                return RedirectToAction("Index", "Account");

            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var classes = await _context.Groups
                .Where(g => instructorGroups.Contains(g.Id))
                .Include(g => g.UserGroups)
                .Select(g => new ClassItemViewModel
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    StudentCount = g.UserGroups.Count
                })
                .ToListAsync();

            return View(classes);
        }

        // ==========================================
        // CLASS MANAGEMENT - CREATE
        // ==========================================
        [HttpGet]
        public IActionResult CreateClass()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass(ClassCreateViewModel model)
        {
            //try { } catch { ErrorEventArgs("...Lỗi")}
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int instructorId = GetInstructorId();
            if (instructorId == 0)
                return RedirectToAction("Index", "Account");

            var group = new Group
            {
                GroupName = model.GroupName,
                Description = model.Description
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Associate instructor with the newly created group
            var userGroup = new UserGroup
            {
                UserId = instructorId,
                GroupId = group.Id
            };
            _context.UserGroups.Add(userGroup);
            await _context.SaveChangesAsync();

            return RedirectToAction("Classes");
        }

        // ==========================================
        // CLASS MANAGEMENT - EDIT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditClass(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            var model = new ClassCreateViewModel
            {
                GroupName = group.GroupName,
                Description = group.Description
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditClass(int id, ClassCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            group.GroupName = model.GroupName;
            group.Description = model.Description;

            _context.Groups.Update(group);
            await _context.SaveChangesAsync();

            return RedirectToAction("Classes");
        }

        // ==========================================
        // CLASS MANAGEMENT - DELETE
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            return RedirectToAction("Classes");
        }

        // ==========================================
        // EXAM MANAGEMENT - LIST
        // ==========================================
        public async Task<IActionResult> Exams()
        {
            int instructorId = GetInstructorId();
            if (instructorId == 0)
                return RedirectToAction("Index", "Account");

            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var exams = await _context.Exams
                .Where(e => instructorGroups.Contains(e.IdGroup))
                .Include(e => e.Questions)
                .ToListAsync();

            var examIds = exams.Select(e => e.Id).ToList();
            var completedCounts = await _context.ExamResults
                .Where(er => examIds.Contains(er.ExamId))
                .GroupBy(er => er.ExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = exams.Select(e => new ExamManagementViewModel
            {
                Id = e.Id,
                Title = e.Title,
                GroupName = _context.Groups.FirstOrDefault(g => g.Id == e.IdGroup)?.GroupName ?? "",
                IdGroup = e.IdGroup,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Duration = e.Duration,
                TotalQuestions = e.Questions?.Count ?? 0,
                CompletedCount = completedCounts.FirstOrDefault(c => c.ExamId == e.Id)?.Count ?? 0,
                IsActive = e.IsActive
            }).ToList();

            return View(model);
        }

        // ==========================================
        // EXAM MANAGEMENT - CREATE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CreateExam()
        {
            int instructorId = GetInstructorId();
            if (instructorId == 0)
                return RedirectToAction("Index", "Account");

            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var groups = await _context.Groups
                .Where(g => instructorGroups.Contains(g.Id))
                .Select(g => new { g.Id, g.GroupName })
                .ToListAsync();

            ViewBag.Groups = groups;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateExam(ExamCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload groups for re-display
                int instructorId = GetInstructorId();
                var instructorGroups = await _context.UserGroups
                    .Where(ug => ug.UserId == instructorId)
                    .Select(ug => ug.GroupId)
                    .ToListAsync();

                var groups = await _context.Groups
                    .Where(g => instructorGroups.Contains(g.Id))
                    .Select(g => new { g.Id, g.GroupName })
                    .ToListAsync();

                ViewBag.Groups = groups;
                return View(model);
            }

            var exam = new Exam
            {
                Title = model.Title,
                SubjectName = model.Title,  // or retrieve the actual subject name from the group
                IdGroup = model.IdGroup,
                StartTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Utc),
                EndTime = DateTime.SpecifyKind(model.EndTime, DateTimeKind.Utc),
                Duration = model.Duration,
                IsActive = model.IsActive
            };

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            // Add questions
            foreach (var questionVm in model.Questions)
            {
                var question = new Question
                {
                    Content = questionVm.Content,
                    Difficulty = questionVm.Difficulty
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // Add answers
                foreach (var answerVm in questionVm.Answers)
                {
                    var answer = new Answer
                    {
                        Content = answerVm.Text,
                        IsCorrect = answerVm.IsCorrect,
                        QuestionId = question.Id
                    };
                    _context.Answers.Add(answer);
                }

                // Link question to exam
                exam.Questions.Add(question);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Exams");
        }

        // ==========================================
        // EXAM MANAGEMENT - EDIT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditExam(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
                return NotFound();

            int instructorId = GetInstructorId();
            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            if (!instructorGroups.Contains(exam.IdGroup))
                return Forbid();

            var model = new ExamCreateViewModel
            {
                Title = exam.Title,
                IdGroup = exam.IdGroup,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                Duration = exam.Duration,
                IsActive = exam.IsActive
            };

            var groups = await _context.Groups
                .Where(g => instructorGroups.Contains(g.Id))
                .Select(g => new { g.Id, g.GroupName })
                .ToListAsync();

            ViewBag.Groups = groups;
            ViewBag.ExamId = id;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditExam(int id, ExamCreateViewModel model)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                int instructorId = GetInstructorId();
                var instructorGroups = await _context.UserGroups
                    .Where(ug => ug.UserId == instructorId)
                    .Select(ug => ug.GroupId)
                    .ToListAsync();

                var groups = await _context.Groups
                    .Where(g => instructorGroups.Contains(g.Id))
                    .Select(g => new { g.Id, g.GroupName })
                    .ToListAsync();

                ViewBag.Groups = groups;
                ViewBag.ExamId = id;
                return View(model);
            }

            exam.Title = model.Title;
            exam.IdGroup = model.IdGroup;
            exam.StartTime = model.StartTime;
            exam.EndTime = model.EndTime;
            exam.Duration = model.Duration;
            exam.IsActive = model.IsActive;

            _context.Exams.Update(exam);
            await _context.SaveChangesAsync();

            return RedirectToAction("Exams");
        }

        // ==========================================
        // EXAM MANAGEMENT - DELETE
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteExam(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
                return NotFound();

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();

            return RedirectToAction("Exams");
        }

        // ==========================================
        // VIEW EXAM RESULTS BY STUDENTS
        // ==========================================
        public async Task<IActionResult> ExamResults(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null)
                return NotFound();

            int instructorId = GetInstructorId();
            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            if (!instructorGroups.Contains(exam.IdGroup))
                return Forbid();

            var results = await _context.ExamResults
                .Where(er => er.ExamId == examId)
                .Include(er => er.Student)
                .ToListAsync();

            var model = new ExamResultSummaryViewModel
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                StudentResults = results.Select(r => new StudentExamResultViewModel
                {
                    StudentId = r.StudentId,
                    StudentName = r.Student?.FullName ?? "Unknown",
                    Score = r.Score,
                    SubmitTime = r.SubmitTime,
                    Status = r.Score >= 5 ? "Đạt" : "Không đạt"
                }).ToList()
            };

            return View(model);
        }

        // ==========================================
        // EXAM MANAGEMENT - UPDATE STATUS
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> UpdateExamStatus(int examId, string status)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null)
                return Json(new { success = false, message = "Exam not found" });

            int instructorId = GetInstructorId();
            var instructorGroups = await _context.UserGroups
                .Where(ug => ug.UserId == instructorId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            if (!instructorGroups.Contains(exam.IdGroup))
                return Json(new { success = false, message = "Unauthorized" });

            switch (status)
            {
                case "active":
                    exam.IsActive = true;
                    exam.StartTime = DateTime.UtcNow;
                    break;
                case "paused":
                    exam.IsActive = false;
                    break;
                case "completed":
                    exam.EndTime = DateTime.UtcNow;
                    exam.IsActive = false;
                    break;
                case "scheduled":
                    exam.IsActive = true;
                    break;
            }

            _context.Exams.Update(exam);
            await _context.SaveChangesAsync();

            return Json(new { success = true, status = status });
        }
    }
}
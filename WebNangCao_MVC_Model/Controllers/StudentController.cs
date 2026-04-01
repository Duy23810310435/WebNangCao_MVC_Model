using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao_MVC_Model.Data;
using WebNangCao_MVC_Model.Models;
using WebNangCao_MVC_Model.ViewModels;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

            var studentGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == studentId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var rawExams = await _context.Exams
                .Include(e => e.Questions)
                .Where(e => e.IsActive && studentGroupIds.Contains(e.IdGroup))
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

                if (completedExamIds.Contains(exam.Id))
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
                    status = "Đã hoàn thành";
                }

                examListVM.Add(new ExamItemViewModel
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    StartTime = exam.StartTime,
                    Duration = exam.Duration,
                    TotalQuestions = exam.Questions?.Count ?? 0,
                    Status = status,
                    IdGroup = exam.IdGroup,
                    SubjectName = "Môn học " + exam.IdGroup,
                    Difficulty = "Trung bình"
                });
            }

            var model = new StudentDashboardViewModel
            {
                TotalExams = rawExams.Count,
                CompletedExams = userResults.Count,
                UpcomingExams = examListVM.Count(e => e.Status == "Sắp tới"),
                AverageScore = userResults.Any() ? Math.Round(userResults.Average(r => r.Score), 1) : 0,
                Exams = examListVM.OrderBy(e => e.StartTime).ToList()
            };

            return View(model);
        }

        // ==========================================
        // 2. JOIN CLASS - XỬ LÝ KHI BẤM "THAM GIA LỚP"
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> JoinClass(int groupId)
        {
            // Lấy ID sinh viên hiện tại
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Account");
            }
            int studentId = int.Parse(userIdString);

            // Kiểm tra xem lớp học (Group) có tồn tại không
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
            {
                // Truyền thông báo lỗi sang View
                TempData["ErrorMessage"] = $"Không tìm thấy lớp học với mã '{groupId}'. Vui lòng kiểm tra lại!";
                return RedirectToAction(nameof(Dashboard));
            }

            // Kiểm tra xem sinh viên đã tham gia lớp này chưa
            var alreadyJoined = await _context.UserGroups
                .AnyAsync(ug => ug.UserId == studentId && ug.GroupId == groupId);

            if (alreadyJoined)
            {
                TempData["ErrorMessage"] = $"Bạn đã tham gia lớp học (Mã: {groupId}) này rồi!";
                return RedirectToAction(nameof(Dashboard));
            }

            // Thêm sinh viên vào lớp
            var newUserGroup = new UserGroup
            {
                UserId = studentId,
                GroupId = groupId,
                JoinedAt = DateTime.UtcNow // Lưu lại thời gian tham gia
            };

            _context.UserGroups.Add(newUserGroup);
            await _context.SaveChangesAsync();

            // Gửi thông báo thành công
            TempData["SuccessMessage"] = $"Chúc mừng! Bạn đã tham gia lớp học (Mã: {groupId}) thành công.";
            return RedirectToAction(nameof(Dashboard));
        }

        // ==========================================
        // 3. SEED DATA - DỮ LIỆU MẪU
        // ==========================================
        public IActionResult SeedData()
        {
            if (!_context.Exams.Any())
            {
                var now = DateTime.UtcNow;

                _context.Exams.AddRange(
                    new Exam
                    {
                        Title = "Kiểm tra Toán học Học kỳ 1",
                        IsActive = true,
                        StartTime = now.AddMinutes(-10),
                        EndTime = now.AddDays(1),
                        Duration = 60,
                        IdGroup = 1
                    },
                    new Exam
                    {
                        Title = "Tiếng Anh giữa kỳ",
                        IsActive = true,
                        StartTime = now.AddDays(2),
                        EndTime = now.AddDays(3),
                        Duration = 90,
                        IdGroup = 2
                    },
                    new Exam
                    {
                        Title = "Vật lý 15 phút",
                        IsActive = true,
                        StartTime = now.AddDays(-5),
                        EndTime = now.AddDays(-4),
                        Duration = 15,
                        IdGroup = 1
                    }
                );
                _context.SaveChanges();
            }

            if (!_context.ExamResults.Any())
            {
                var pastExam = _context.Exams.FirstOrDefault(e => e.Title.Contains("Vật lý"));
                if (pastExam != null)
                {
                    _context.ExamResults.Add(
                        new ExamResult { StudentId = 1, ExamId = pastExam.Id, Score = 8.5 }
                    );
                    _context.SaveChanges();
                }
            }

            return Content("Đã bơm dữ liệu mẫu THÀNH CÔNG! Hãy truy cập lại /Student/Dashboard để xem các thẻ bài thi xịn xò nhé.");
        }
    }
}
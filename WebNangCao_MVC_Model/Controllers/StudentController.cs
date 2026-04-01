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
                // Tính trạng thái bài thi
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

                // Tính độ khó ưu thế và CHUẨN HÓA TÊN
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
                        // Chuẩn hóa chuỗi: đưa về chữ thường và xóa khoảng trắng thừa
                        string normalizedTop = topDifficulty.ToLower().Trim();

                        switch (normalizedTop)
                        {
                            case "dễ":
                            case "easy":
                            case "de":
                                dominantDifficulty = "Dễ";
                                break;
                            case "trung bình":
                            case "medium":
                            case "trung binh":
                                dominantDifficulty = "Trung bình";
                                break;
                            case "khó":
                            case "hard":
                            case "kho":
                                dominantDifficulty = "Khó";
                                break;
                            default:
                                dominantDifficulty = "Chưa xác định";
                                break;
                        }
                    }
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
                    Difficulty = dominantDifficulty // Sẽ chỉ xuất ra "Dễ", "Trung bình", "Khó" hoặc "Chưa xác định"
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
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Account");
            }
            int studentId = int.Parse(userIdString);

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy lớp học với mã '{groupId}'. Vui lòng kiểm tra lại!";
                return RedirectToAction(nameof(Dashboard));
            }

            var alreadyJoined = await _context.UserGroups
                .AnyAsync(ug => ug.UserId == studentId && ug.GroupId == groupId);

            if (alreadyJoined)
            {
                TempData["ErrorMessage"] = $"Bạn đã tham gia lớp học (Mã: {groupId}) này rồi!";
                return RedirectToAction(nameof(Dashboard));
            }

            var newUserGroup = new UserGroup
            {
                UserId = studentId,
                GroupId = groupId,
                JoinedAt = DateTime.UtcNow
            };

            _context.UserGroups.Add(newUserGroup);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Chúc mừng! Bạn đã tham gia lớp học (Mã: {groupId}) thành công.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
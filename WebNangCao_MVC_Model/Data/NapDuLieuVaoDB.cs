using WebNangCao_MVC_Model.Models;
using System.Linq;
using System.Collections.Generic;
using System;

namespace WebNangCao_MVC_Model.Data
{
    public class NapDuLieuVaoDB
    {
        public static void Seed(AppDbContext context)
        {
            // Kiểm tra nếu đã có bài thi rồi thì thoát, không cần nạp lại
            if (context.Exams.Any()) return;

            // ==========================================
            // 1. TẠO LỚP HỌC (GROUP) MẪU
            // ==========================================
            var group = new Group
            {
                GroupName = "Lớp Tiếng Anh IT K12",
                Description = "Lớp học dành cho sinh viên kiểm tra đầu vào"
            };
            context.Groups.Add(group);
            context.SaveChanges(); // Lưu xuống DB để EF Core sinh ra group.Id

            // ==========================================
            // 2. THÊM TẤT CẢ USER HIỆN CÓ VÀO LỚP NÀY (ĐỂ TEST)
            // ==========================================
            // Lấy danh sách các tài khoản sinh viên đã đăng ký trước đó
            var existingUsers = context.Users.Where(u => u.Role == "student").ToList();
            foreach (var user in existingUsers)
            {
                // Thêm vào bảng trung gian UserGroup
                context.UserGroups.Add(new UserGroup
                {
                    UserId = user.Id,
                    GroupId = group.Id
                });
            }
            context.SaveChanges();

            // ==========================================
            // 3. TẠO BÀI THI MẪU VÀ GÁN VÀO LỚP HỌC
            // ==========================================
            var exam = new Exam
            {
                Title = "Bài kiểm tra Tiếng Anh B2",
                Duration = 45,
                IsActive = true,
                // Khởi tạo StartTime cách đây 5 phút để trạng thái là "Có thể làm"
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(7),
                IdGroup = group.Id, // <--- ĐIỂM QUAN TRỌNG: Liên kết bài thi với Group
                Questions = new List<Question>()
            };

            // 4. Tạo danh sách câu hỏi và đáp án (Giữ nguyên của bạn)
            var q1 = new Question
            {
                Content = "What does 'MVC' stand for in web development?",
                Difficulty = "Trung bình",
                Answers = new List<Answer>()
                {
                    new Answer { Content = "Model View Controller", IsCorrect = true },
                    new Answer { Content = "Main Video Center", IsCorrect = false },
                    new Answer { Content = "Modern View Concept", IsCorrect = false }
                }
            };

            var q2 = new Question
            {
                Content = "Which language is primarily used for ASP.NET Core?",
                Difficulty = "Dễ",
                Answers = new List<Answer>
                {
                    new Answer { Content = "Python", IsCorrect = false },
                    new Answer { Content = "C#", IsCorrect = true },
                    new Answer { Content = "Java", IsCorrect = false }
                }
            };

            var q3 = new Question
            {
                Content = "What is the purpose of Entity Framework in ASP.NET Core?",
                Difficulty = "Khó",
                Answers = new List<Answer>()
                {
                    new Answer { Content = "To manage database connections and queries", IsCorrect = true },
                    new Answer { Content = "To handle user authentication", IsCorrect = false },
                    new Answer { Content = "To create user interfaces", IsCorrect = false },
                    new Answer { Content = "To optimize application performance", IsCorrect = false }
                }
            };

            // 5. Kết nối chúng lại
            exam.Questions.Add(q1);
            exam.Questions.Add(q2);
            exam.Questions.Add(q3);

            // 6. Lưu Bài thi vào DB
            context.Exams.Add(exam);
            context.SaveChanges();
        }
    }
}
Tác giả: Dương Tiến Chiến, Nguyễn Đình Duy, Lê Công Đức
- Các câu lệnh Git quan trọng:
git clone <URL>
git status : xem trạng thái file
git add .
git commit -m "Tên Đặt cho Message Commit"
git push
- Các câu lệnh với branch:
git branch -r : xem danh sách các branch
git branch : xem branch hiện tại cùng với danh sách
git checkout <tên branch>
git checkout master : quay về banch main || master
git push : nếu để nguyên vậy thì nó tự động push lên branch hiện tại của nó (xem "git status" để biết branch nào )
git push origin main : push lên branch main || master

- Cách hoàn tác việc Push Commit:
git revert <commit_id> : "commit_id" xem trên detail của commit đấy. hoặc sử dụng "git log --oneline || git log"
git push origin <ten_branch>|| main  : sau khi sửa commit xong thì "push" lại

- Xóa hoàn toàn Commit
git reset --hard <commit_id>
git push origin <ten_branch> --force

- Xóa thứ tự Commit:
HEAD = commit hiện tại
HEAD~1 = commit trước đó
HEAD~2 = 2 commit trước
Ví dụ: git reset --hard HEAD~1

- Hợp nhất và đồng bộ dữ liệu từ Main sang các Branch khác
// Cập nhật main mới nhất
git checkout main
git pull origin main

//chuyển sang branch cần merge
git checkout Chien
(git checkout ....)

//merge main vào branch đó (lặp lại với branch khác)
git merge main
git push //push để cập nhật merge lên github

Công Nghệ Sử Dụng
Frontend: HTML, CSS, JavaScript, Tailwind CSS.

Backend: ASP.NET MVC (C#).

Cơ Sở Dữ Liệu: PostgreSQL.

Testing: Selenium (UI Test), NUnit (Unit Test).

Yêu Cầu Môi Trường (Prerequisites)
.NET SDK: Phiên bản 10.0 trở lên.

IDE: Visual Studio 2022 (hoặc VS Code).

Database Engine: PostgreSQL đã được cài đặt và chạy trên máy.

Hướng Dẫn Sử Dụng (Cho Môn Kiểm Thử)

Phần 1: Phục hồi Cơ sở dữ liệu (Database Restore)
Yêu cầu: Máy tính đã cài đặt PostgreSQL và công cụ quản lý pgAdmin 4.

Tạo một Database rỗng trong PostgreSQL với tên chính xác là: EduTestDB.

Nhấp chuột phải vào Database EduTestDB vừa tạo, chọn Restore...

Trỏ đường dẫn đến file .backup được đính kèm trong thư mục dự án để khôi phục toàn bộ cấu trúc bảng (Schema) và dữ liệu mẫu.

Phần 2: Cài đặt và Khởi chạy Website
Mở Terminal (hoặc CMD/Git Bash) tại thư mục muốn lưu dự án, chạy lệnh sau để tải source code:

Bash
git clone https://github.com/Duy23810310435/WebNangCao_MVC_Model
Mở Visual Studio 2022 (Đảm bảo máy đã cài đặt .NET 10.0 LTS) và mở thư mục/solution dự án vừa clone về.

[BƯỚC BẮT BUỘC]: Mở file cấu hình kết nối (thường là appsettings.json) trong project WebNangCao_MVC_Model. Tìm đến mục ConnectionStrings và cập nhật lại User ID và Password cho khớp với tài khoản PostgreSQL trên máy của bạn.

Đặt project WebNangCao_MVC_Model làm project khởi chạy mặc định (Right-click vào project > Chọn Set as Startup Project).

Nhấn Ctrl + F5 (Run without Debugging) để khởi chạy Website. Hãy giữ cho website được bật và chạy ngầm.

Phần 3: Chạy Kiểm thử Tự động (Selenium & NUnit)

Trong cửa sổ Solution Explorer của Visual Studio, tìm và mở project kiểm thử mang tên: WebNangCao_MVC_KiemThu.

Mở file mã nguồn chứa các Test Case.

Nhấp chuột phải vào màn hình code > Chọn Run Tests (Kèm biểu tượng ống nghiệm).

Hệ thống sẽ tự động bật trình duyệt và thực thi các kịch bản kiểm thử. Bạn có thể quan sát trực tiếp kết quả (Pass/Fail) trên cửa sổ Test Explorer.

## Cấu trúc thư mục & Phạm vi kiểm thử (Test Scope)

Vì đây là đồ án chuyên sâu về Kiểm thử Phần mềm, nhóm tập trung toàn bộ nguồn lực vào việc kiểm thử tự động (Automation Test) cho các luồng nghiệp vụ (Business Flow) cốt lõi nhất của hệ thống. 

Cấu trúc thư mục mã nguồn kiểm thử (`WebNangCao_MVC_KiemThu`) được tổ chức như sau:
 WebNangCao_MVC_KiemThu
 
 DangNhapDangKyTest.cs        # Chứa các kịch bản kiểm thử (Test Cases) cho chức năng Xác thực người dùng (Login/Register). Đảm bảo tính bảo mật và luồng truy cập hợp lệ.
 
 KiemThuQuaTrinhLamBaiThi.cs  # Chứa các kịch bản kiểm thử cho Nghiệp vụ cốt lõi: Quá trình người dùng chọn đề, làm bài thi và nộp bài (Submit). Đảm bảo hệ thống ghi nhận đúng điểm số và thời gian.
 
 WebNangCao_MVC_KiemThu.csproj # File cấu hình thư viện NUnit và Selenium cho project kiểm thử.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebNangCao_MVC_Model.Models
{
    [Table("Groups")]
    public class Group
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty; // VD: "Lớp Tiếng Anh B2"
        public string Description { get; set; } = string.Empty;

        // Lưu ID của giảng viên để hiển thị chủ nhân của nhóm lớp(Khóa ngoại trỏ tới bảng Users)
        public int? TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        // Navigation properties
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
        // Nếu file Exam của bạn đã có IdGroup, bạn có thể thêm List<Exam> ở đây
    }
}
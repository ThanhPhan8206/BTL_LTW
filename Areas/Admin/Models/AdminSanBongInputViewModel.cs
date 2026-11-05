using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminSanBongInputViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sân bóng")]
        [MaxLength(100, ErrorMessage = "Tên sân không quá 100 ký tự")]
        public string TenSan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại sân")]
        public string LoaiSan { get; set; } = "Sân 5 người";

        public string? MoTa { get; set; }

        public string TrangThai { get; set; } = "Sẵn sàng";

        public string? AnhSanHienTai { get; set; }

        public IFormFile? AnhSanFile { get; set; }
    }
}

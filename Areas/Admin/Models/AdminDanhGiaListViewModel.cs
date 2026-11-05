using System.Collections.Generic;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDanhGiaListViewModel
    {
        public List<AdminDanhGiaItemViewModel> Items { get; set; } = new();

        public int? SoSao { get; set; }
        public int? SanBongId { get; set; }

        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public double DiemTrungBinh { get; set; }
        public int TongDanhGia { get; set; }

        public int SoLuong5Sao { get; set; }
        public int SoLuong4Sao { get; set; }
        public int SoLuong3Sao { get; set; }
        public int SoLuong2Sao { get; set; }
        public int SoLuong1Sao { get; set; }
        public int SoLuongDaAn { get; set; }

        public List<SanBong> DanhSachSanBong { get; set; } = new();
    }
}


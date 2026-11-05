using System;
using System.Collections.Generic;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDatSanListViewModel
    {
        public List<AdminBookingItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayDat { get; set; }
        public int? SanBongId { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public int CountChoDuyet { get; set; }
        public int CountDaXacNhan { get; set; }
        public int CountDangDa { get; set; }
        public int CountHoanThanh { get; set; }
        public int CountDaHuy { get; set; }

        public List<SanBong> DanhSachSan { get; set; } = new();

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}


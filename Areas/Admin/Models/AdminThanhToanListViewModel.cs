using System;
using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThanhToanListViewModel
    {
        public List<AdminThanhToanItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? TrangThai { get; set; }
        public string? PhuongThuc { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        // 4 Thống kê doanh thu
        public decimal TongDoanhThu { get; set; }
        public decimal TongTienMat { get; set; }
        public decimal TongChuyenKhoan { get; set; }
        public decimal TongHoanCoc { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}


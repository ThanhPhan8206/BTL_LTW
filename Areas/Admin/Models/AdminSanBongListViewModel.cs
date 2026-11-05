using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminSanBongListViewModel
    {
        public List<AdminSanBongItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? LoaiSan { get; set; }
        public string? TrangThai { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public int TotalSan5 { get; set; }
        public int TotalSan7 { get; set; }
        public int TotalSan11 { get; set; }
        public int TotalDangBaoTri { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}


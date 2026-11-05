using System.Collections.Generic;

namespace QLSanBong.Models.ViewModels
{
    public class KhachHangAdminViewModel
    {
        public List<KhachHangAdminItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? TrangThai { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public int TotalActiveCount { get; set; }
        public int TotalLockedCount { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}


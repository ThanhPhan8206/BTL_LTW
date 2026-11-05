using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.Admin.Models;
using QLSanBong.Models.Entities;
using QLSanBong.Services;

namespace QLSanBong.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DanhGiaController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly NotificationService _notificationService;

        public DanhGiaController(QLSanBongDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Danh sách đánh giá & phản hồi của khách hàng: lọc theo số sao (1-5 sao), lọc theo sân, có phân trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? soSao, int? sanBongId, int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 10;

            var baseQuery = _context.DanhGia
                .Include(d => d.SanBong)
                .Include(d => d.NguoiDung)
                .Include(d => d.DatSan)
                .AsQueryable();

            // Thống kê tổng quan đánh giá
            var allRatings = await _context.DanhGia.ToListAsync();
            var tongDanhGia = allRatings.Count;
            var diemTrungBinh = tongDanhGia > 0 ? Math.Round(allRatings.Average(r => r.SoSao), 1) : 0;
            var soLuong5Sao = allRatings.Count(r => r.SoSao == 5);
            var soLuong4Sao = allRatings.Count(r => r.SoSao == 4);
            var soLuong3Sao = allRatings.Count(r => r.SoSao == 3);
            var soLuong2Sao = allRatings.Count(r => r.SoSao == 2);
            var soLuong1Sao = allRatings.Count(r => r.SoSao == 1);
            var soLuongDaAn = allRatings.Count(r => r.BinhLuan != null && r.BinhLuan.StartsWith("[ẨN]"));

            var query = baseQuery;

            // 1. Lọc theo số sao
            if (soSao.HasValue && soSao.Value >= 1 && soSao.Value <= 5)
            {
                query = query.Where(d => d.SoSao == soSao.Value);
            }

            // 2. Lọc theo sân bóng
            if (sanBongId.HasValue && sanBongId.Value > 0)
            {
                query = query.Where(d => d.SanBongId == sanBongId.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedData = await query
                .OrderByDescending(d => d.NgayTao ?? DateTime.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pagedData.Select(d =>
            {
                var raw = d.BinhLuan ?? string.Empty;
                var daAn = raw.StartsWith("[ẨN]");
                var clean = daAn ? raw.Substring(4).Trim() : raw;

                string binhLuanGoc = clean;
                string? phanHoiAdmin = null;

                const string adminTag = " || [Admin]: ";
                var tagIndex = clean.IndexOf(adminTag, StringComparison.OrdinalIgnoreCase);
                if (tagIndex >= 0)
                {
                    binhLuanGoc = clean.Substring(0, tagIndex).Trim();
                    phanHoiAdmin = clean.Substring(tagIndex + adminTag.Length).Trim();
                }

                return new AdminDanhGiaItemViewModel
                {
                    Id = d.Id,
                    DatSanId = d.DatSanId,
                    TenKhachHang = d.NguoiDung?.HoTen ?? "Khách hàng",
                    SoDienThoai = d.NguoiDung?.SoDienThoai ?? "",
                    Email = d.NguoiDung?.Email ?? "",
                    TenSan = d.SanBong?.TenSan ?? "Sân bóng",
                    LoaiSan = d.SanBong?.LoaiSan ?? "Sân",
                    SoSao = d.SoSao,
                    NgayTao = d.NgayTao,
                    BinhLuanGoc = binhLuanGoc,
                    PhanHoiAdmin = phanHoiAdmin,
                    DaAn = daAn,
                    BinhLuanRaw = raw
                };
            }).ToList();

            var danhSachSanBong = await _context.SanBongs
                .OrderBy(s => s.TenSan)
                .ToListAsync();

            var viewModel = new AdminDanhGiaListViewModel
            {
                Items = items,
                SoSao = soSao,
                SanBongId = sanBongId,
                Page = page,
                TotalPages = totalPages,
                TotalItems = totalItems,

                DiemTrungBinh = diemTrungBinh,
                TongDanhGia = tongDanhGia,
                SoLuong5Sao = soLuong5Sao,
                SoLuong4Sao = soLuong4Sao,
                SoLuong3Sao = soLuong3Sao,
                SoLuong2Sao = soLuong2Sao,
                SoLuong1Sao = soLuong1Sao,
                SoLuongDaAn = soLuongDaAn,

                DanhSachSanBong = danhSachSanBong
            };

            return View(viewModel);
        }

        /// <summary>
        /// Phản hồi đánh giá của khách hàng: Nối chuỗi " || [Admin]: ..." vào cuối BinhLuan và bắn thông báo tới khách.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanHoi(int id, string noiDungPhanHoi)
        {
            if (string.IsNullOrWhiteSpace(noiDungPhanHoi))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung phản hồi.";
                return RedirectToAction(nameof(Index));
            }

            var danhGia = await _context.DanhGia
                .Include(d => d.NguoiDung)
                .Include(d => d.SanBong)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (danhGia == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            var raw = danhGia.BinhLuan ?? string.Empty;
            var daAn = raw.StartsWith("[ẨN]");
            var clean = daAn ? raw.Substring(4).Trim() : raw;

            // Tách bỏ phản hồi admin cũ nếu có để cập nhật phản hồi mới
            const string adminTag = " || [Admin]: ";
            var tagIndex = clean.IndexOf(adminTag, StringComparison.OrdinalIgnoreCase);
            var binhLuanGoc = tagIndex >= 0 ? clean.Substring(0, tagIndex).Trim() : clean;

            var newBinhLuan = $"{binhLuanGoc}{adminTag}{noiDungPhanHoi.Trim()}";
            if (daAn)
            {
                newBinhLuan = $"[ẨN] {newBinhLuan}";
            }

            danhGia.BinhLuan = newBinhLuan;
            await _context.SaveChangesAsync();

            // Gửi thông báo đến khách hàng
            var tieuDe = $"[PHẢN HỒI] Ban quản lý sân đã phản hồi đánh giá #{danhGia.Id}";
            var noiDung = $"Cảm ơn bạn đã đóng góp ý kiến về sân {danhGia.SanBong?.TenSan ?? "UTC STADIUM"}.\nPhản hồi từ Admin: \"{noiDungPhanHoi.Trim()}\"";
            await _notificationService.GuiThongBaoAsync(danhGia.NguoiDungId, tieuDe, noiDung, "DanhGia");

            TempData["SuccessMessage"] = $"Đã phản hồi đánh giá của khách hàng '{danhGia.NguoiDung?.HoTen}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Ẩn hoặc Bỏ ẩn đánh giá không phù hợp (Gắn tiền tố [ẨN] để giao diện khách hàng tự động loại trừ)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnDanhGia(int id)
        {
            var danhGia = await _context.DanhGia.FindAsync(id);
            if (danhGia == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            var raw = danhGia.BinhLuan ?? string.Empty;
            if (raw.StartsWith("[ẨN]"))
            {
                // Mở lại hiển thị
                danhGia.BinhLuan = raw.Substring(4).Trim();
                TempData["SuccessMessage"] = "Đã bỏ ẩn và hiển thị lại đánh giá công khai.";
            }
            else
            {
                // Ẩn đánh giá
                danhGia.BinhLuan = $"[ẨN] {raw}".Trim();
                TempData["SuccessMessage"] = "Đã ẩn đánh giá khỏi giao diện khách hàng công khai.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa đánh giá vi phạm tiêu chuẩn cộng đồng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var danhGia = await _context.DanhGia.FindAsync(id);
            if (danhGia == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            _context.DanhGia.Remove(danhGia);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa vĩnh viễn đánh giá thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}


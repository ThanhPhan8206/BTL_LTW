using System;
using System.Collections.Generic;
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
    public class ThongBaoController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly NotificationService _notificationService;

        public ThongBaoController(QLSanBongDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Danh sách các thông báo & sự kiện đã phát hành: tìm kiếm, lọc theo loại, phân trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? loaiThongBao = null, int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 15;

            var baseQuery = _context.ThongBaos
                .Include(t => t.NguoiDung)
                .AsQueryable();

            // Thống kê tổng số lượng thông báo theo nhóm
            var allNotifications = await _context.ThongBaos.ToListAsync();
            var tongThongBao = allNotifications.Count;
            var soSuKien = allNotifications.Count(t => t.LoaiThongBao == "SuKien");
            var soHeThong = allNotifications.Count(t => t.LoaiThongBao == "HeThong");
            var soKhuyenMai = allNotifications.Count(t => t.LoaiThongBao == "KhuyenMai");

            var query = baseQuery;

            // 1. Tìm kiếm theo tiêu đề, nội dung, tên hoặc SĐT người nhận
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t => t.TieuDe.ToLower().Contains(s) ||
                                         t.NoiDung.ToLower().Contains(s) ||
                                         t.NguoiDung.HoTen.ToLower().Contains(s) ||
                                         t.NguoiDung.SoDienThoai.Contains(s));
            }

            // 2. Lọc theo loại thông báo
            if (!string.IsNullOrWhiteSpace(loaiThongBao) && loaiThongBao != "all")
            {
                query = query.Where(t => t.LoaiThongBao == loaiThongBao);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedData = await query
                .OrderByDescending(t => t.NgayTao ?? DateTime.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pagedData.Select(t => new AdminThongBaoItemViewModel
            {
                Id = t.Id,
                NguoiDungId = t.NguoiDungId,
                TenKhachHang = t.NguoiDung?.HoTen ?? "Người dùng",
                SoDienThoai = t.NguoiDung?.SoDienThoai ?? "",
                Email = t.NguoiDung?.Email ?? "",
                TieuDe = t.TieuDe,
                NoiDung = t.NoiDung,
                LoaiThongBao = t.LoaiThongBao,
                DaDoc = t.DaDoc ?? false,
                NgayTao = t.NgayTao
            }).ToList();

            var danhSachKhachHang = await _context.NguoiDungs
                .Where(n => n.TrangThai == "Hoạt động")
                .OrderBy(n => n.HoTen)
                .ToListAsync();

            var viewModel = new AdminThongBaoListViewModel
            {
                Items = items,
                Search = search,
                LoaiThongBao = loaiThongBao,
                Page = page,
                TotalPages = totalPages,
                TotalItems = totalItems,

                TongThongBao = tongThongBao,
                SoThongBaoSuKien = soSuKien,
                SoThongBaoHeThong = soHeThong,
                SoThongBaoKhuyenMai = soKhuyenMai,

                DanhSachKhachHang = danhSachKhachHang
            };

            return View(viewModel);
        }

        /// <summary>
        /// Tạo và phát hành thông báo sự kiện: Hỗ trợ gửi cho Một khách hàng hoặc Toàn bộ khách hàng hoạt động.
        /// Tích hợp lưu bảng dbo.ThongBao và tự động gửi Email Gmail qua NotificationService.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tieuDe, string noiDung, string loaiThongBao, int? nguoiDungId, bool guiTatCa = false)
        {
            if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tiêu đề và nội dung thông báo.";
                return RedirectToAction(nameof(Index));
            }

            var recipients = new List<NguoiDung>();
            if (guiTatCa)
            {
                recipients = await _context.NguoiDungs
                    .Where(u => u.TrangThai == "Hoạt động")
                    .ToListAsync();
            }
            else
            {
                if (!nguoiDungId.HasValue || nguoiDungId.Value <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn khách hàng nhận thông báo hoặc tích chọn gửi toàn hệ thống.";
                    return RedirectToAction(nameof(Index));
                }

                var customer = await _context.NguoiDungs.FindAsync(nguoiDungId.Value);
                if (customer != null)
                {
                    recipients.Add(customer);
                }
            }

            if (!recipients.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy người nhận hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var loai = string.IsNullOrWhiteSpace(loaiThongBao) ? "SuKien" : loaiThongBao.Trim();

            foreach (var user in recipients)
            {
                await _notificationService.GuiThongBaoAsync(user.Id, tieuDe.Trim(), noiDung.Trim(), loai);
            }

            TempData["SuccessMessage"] = $"Đã phát hành và gửi thông báo thành công đến {recipients.Count} khách hàng qua Web & Gmail!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa một thông báo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var thongBao = await _context.ThongBaos.FindAsync(id);
            if (thongBao == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                return RedirectToAction(nameof(Index));
            }

            _context.ThongBaos.Remove(thongBao);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa bản ghi thông báo thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}


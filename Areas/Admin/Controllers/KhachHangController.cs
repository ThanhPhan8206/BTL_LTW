using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Models.Entities;
using QLSanBong.Models.ViewModels;

namespace QLSanBong.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class KhachHangController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public KhachHangController(QLSanBongDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách khách hàng kèm bộ lọc, tìm kiếm và phân trang 10 dòng/trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? trangThai = null, int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 10;

            // Truy vấn người dùng thuộc quyền Khách hàng (loại trừ các tài khoản Admin / Quản trị)
            var baseQuery = _context.NguoiDungs
                .Include(n => n.QuyenHan)
                .Include(n => n.DatSans)
                    .ThenInclude(d => d.ThanhToans)
                .Where(n => n.QuyenHan == null ||
                            (!n.QuyenHan.TenQuyen.Contains("Admin") &&
                             !n.QuyenHan.TenQuyen.Contains("Quản trị") &&
                             !n.QuyenHan.TenQuyen.Contains("Quan tri")));

            // Thống kê nhanh số lượng Hoạt động vs Bị khóa
            var totalActive = await baseQuery.CountAsync(n => n.TrangThai == "Hoạt động" || n.TrangThai == "Active");
            var totalLocked = await baseQuery.CountAsync(n => n.TrangThai == "Bị khóa" || n.TrangThai == "Bi khoa" || n.TrangThai == "Locked");

            var query = baseQuery.AsQueryable();

            // Tìm kiếm theo Họ tên, Email hoặc Số điện thoại
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(n => n.HoTen.ToLower().Contains(s) ||
                                         n.Email.ToLower().Contains(s) ||
                                         n.SoDienThoai.Contains(s));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "all")
            {
                if (trangThai == "Hoạt động")
                {
                    query = query.Where(n => n.TrangThai == "Hoạt động" || n.TrangThai == "Active");
                }
                else if (trangThai == "Bị khóa")
                {
                    query = query.Where(n => n.TrangThai == "Bị khóa" || n.TrangThai == "Bi khoa" || n.TrangThai == "Locked");
                }
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var customers = await query
                .OrderByDescending(n => n.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = customers.Select(n =>
            {
                var totalSpent = n.DatSans
                    .SelectMany(d => d.ThanhToans)
                    .Where(t => t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                    .Sum(t => t.SoTien);

                return new KhachHangAdminItemViewModel
                {
                    Id = n.Id,
                    HoTen = n.HoTen,
                    Email = n.Email,
                    SoDienThoai = n.SoDienThoai,
                    AnhDaiDienUrl = n.AnhDaiDienUrl,
                    TrangThai = n.TrangThai,
                    NgayTao = n.NgayTao,
                    NgayCapNhat = n.NgayCapNhat,
                    TongDonDat = n.DatSans.Count,
                    TongChiTieu = totalSpent
                };
            }).ToList();

            var viewModel = new KhachHangAdminViewModel
            {
                Items = items,
                Search = search,
                TrangThai = trangThai,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                TotalActiveCount = totalActive,
                TotalLockedCount = totalLocked
            };

            return View(viewModel);
        }

        /// <summary>
        /// Xem chi tiết hồ sơ khách hàng, 4 chỉ số cá nhân và bảng lịch sử đặt sân.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.NguoiDungs
                .Include(n => n.QuyenHan)
                .Include(n => n.DatSans)
                    .ThenInclude(d => d.SanBong)
                .Include(n => n.DatSans)
                    .ThenInclude(d => d.KhungGio)
                .Include(n => n.DatSans)
                    .ThenInclude(d => d.ThanhToans)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var donHoanThanh = customer.DatSans.Count(d => d.TrangThai == "Hoàn thành");
            var donDaHuy = customer.DatSans.Count(d => d.TrangThai == "Từ chối" || d.TrangThai == "Đã hủy");
            var tongChiTieu = customer.DatSans
                .SelectMany(d => d.ThanhToans)
                .Where(t => t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                .Sum(t => t.SoTien);

            var historyItems = customer.DatSans
                .OrderByDescending(d => d.NgayDat)
                .ThenByDescending(d => d.Id)
                .Select(d =>
                {
                    var paid = d.ThanhToans
                        .Where(t => t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                        .Sum(t => t.SoTien);

                    var gioBatDau = d.KhungGio?.GioBatDau.ToString("HH:mm") ?? "17:30";
                    var gioKetThuc = d.KhungGio?.GioKetThuc.ToString("HH:mm") ?? "19:00";

                    return new KhachHangLichSuDatSanItemViewModel
                    {
                        Id = d.Id,
                        TenSan = d.SanBong?.TenSan ?? "Sân bóng",
                        LoaiSan = d.SanBong?.LoaiSan ?? "Sân 5 người",
                        KhungGioDisplay = $"{gioBatDau} - {gioKetThuc}",
                        NgayDat = d.NgayDat,
                        TongTien = d.TongTien,
                        DaThanhToan = paid,
                        TrangThai = d.TrangThai,
                        NgayTao = d.NgayTao
                    };
                }).ToList();

            var viewModel = new KhachHangAdminDetailViewModel
            {
                Id = customer.Id,
                HoTen = customer.HoTen,
                Email = customer.Email,
                SoDienThoai = customer.SoDienThoai,
                AnhDaiDienUrl = customer.AnhDaiDienUrl,
                TrangThai = customer.TrangThai,
                NgayTao = customer.NgayTao,
                NgayCapNhat = customer.NgayCapNhat,
                TongDonDat = customer.DatSans.Count,
                DonHoanThanh = donHoanThanh,
                DonDaHuy = donDaHuy,
                TongChiTieu = tongChiTieu,
                LichSuDatSan = historyItems
            };

            return View(viewModel);
        }

        /// <summary>
        /// Khóa / Mở khóa tài khoản khách hàng (chuyển đổi giữa 'Hoạt động' và 'Bị khóa').
        /// TUYỆT ĐỐI không xóa khách, không reset password.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id, string? returnUrl = null)
        {
            var customer = await _context.NguoiDungs
                .Include(n => n.QuyenHan)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng cần thao tác.";
                return RedirectToAction(nameof(Index));
            }

            // Bảo vệ an toàn: Không cho phép thao tác tài khoản quản trị viên
            var roleName = customer.QuyenHan?.TenQuyen ?? string.Empty;
            if (roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                roleName.Contains("Quản trị", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Không thể thao tác khóa trên tài khoản Quản trị viên.";
                return RedirectToAction(nameof(Index));
            }

            var isCurrentlyLocked = customer.TrangThai.Equals("Bị khóa", StringComparison.OrdinalIgnoreCase) ||
                                   customer.TrangThai.Equals("Bi khoa", StringComparison.OrdinalIgnoreCase) ||
                                   customer.TrangThai.Equals("Locked", StringComparison.OrdinalIgnoreCase);

            if (isCurrentlyLocked)
            {
                customer.TrangThai = "Hoạt động";
                TempData["SuccessMessage"] = $"Đã mở khóa tài khoản cho khách hàng '{customer.HoTen}' thành công.";
            }
            else
            {
                customer.TrangThai = "Bị khóa";
                TempData["SuccessMessage"] = $"Đã khóa tài khoản khách hàng '{customer.HoTen}'. Khách hàng sẽ không thể đăng nhập vào hệ thống.";
            }

            customer.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}


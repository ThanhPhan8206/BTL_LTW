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
    public class ThanhToanController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly NotificationService _notificationService;

        public ThanhToanController(QLSanBongDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Danh sách giao dịch thanh toán: tìm theo mã giao dịch / mã đơn, 
        /// lọc theo khoảng thời gian, trạng thái (Đã thanh toán, Chờ thanh toán, Đã hoàn tiền) và hình thức, có phân trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            DateTime? tuNgay = null,
            DateTime? denNgay = null,
            string? trangThai = null,
            string? phuongThuc = null,
            int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 10;

            var baseQuery = _context.ThanhToans
                .Include(t => t.DatSan)
                    .ThenInclude(d => d.NguoiDung)
                .Include(t => t.DatSan)
                    .ThenInclude(d => d.SanBong)
                .Include(t => t.DatSan)
                    .ThenInclude(d => d.KhungGio)
                .AsQueryable();

            // 4 Thống kê tổng quan
            var allPayments = await _context.ThanhToans.ToListAsync();
            var tongDoanhThu = allPayments
                .Where(t => t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                .Sum(t => t.SoTien);

            var tongTienMat = allPayments
                .Where(t => (t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công") &&
                            t.PhuongThucThanhToan == "Tiền mặt")
                .Sum(t => t.SoTien);

            var tongChuyenKhoan = allPayments
                .Where(t => (t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công") &&
                            (t.PhuongThucThanhToan.Contains("QR") || t.PhuongThucThanhToan.Contains("Chuyển khoản")))
                .Sum(t => t.SoTien);

            var tongHoanCoc = allPayments
                .Where(t => t.TrangThai == "Đã hoàn tiền" || t.LoaiThanhToan == "Hoàn cọc")
                .Sum(t => t.SoTien);

            var query = baseQuery;

            // 1. Tìm kiếm theo mã giao dịch, mã đơn, tên hoặc SĐT khách
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t => (t.MaGiaoDich != null && t.MaGiaoDich.ToLower().Contains(s)) ||
                                         t.DatSanId.ToString() == s ||
                                         t.DatSan.NguoiDung.HoTen.ToLower().Contains(s) ||
                                         t.DatSan.NguoiDung.SoDienThoai.Contains(s));
            }

            // 2. Lọc theo khoảng thời gian
            if (tuNgay.HasValue)
            {
                var start = tuNgay.Value.Date;
                query = query.Where(t => t.ThoiGianThanhToan.HasValue && t.ThoiGianThanhToan.Value >= start);
            }
            if (denNgay.HasValue)
            {
                var end = denNgay.Value.Date.AddDays(1);
                query = query.Where(t => t.ThoiGianThanhToan.HasValue && t.ThoiGianThanhToan.Value < end);
            }

            // 3. Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "all")
            {
                if (trangThai == "Đã thanh toán")
                {
                    query = query.Where(t => t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công");
                }
                else
                {
                    query = query.Where(t => t.TrangThai == trangThai);
                }
            }

            // 4. Lọc theo hình thức thanh toán
            if (!string.IsNullOrWhiteSpace(phuongThuc) && phuongThuc != "all")
            {
                query = query.Where(t => t.PhuongThucThanhToan.Contains(phuongThuc));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var rawPayments = await query
                .OrderByDescending(t => t.ThoiGianThanhToan ?? DateTime.MinValue)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rawPayments.Select(t => new AdminThanhToanItemViewModel
            {
                Id = t.Id,
                DatSanId = t.DatSanId,
                TenKhachHang = t.DatSan?.NguoiDung?.HoTen ?? "Khách hàng",
                SoDienThoai = t.DatSan?.NguoiDung?.SoDienThoai ?? "",
                TenSan = t.DatSan?.SanBong?.TenSan ?? "Sân bóng",
                KhungGioDisplay = t.DatSan?.KhungGio != null
                    ? $"{t.DatSan.KhungGio.GioBatDau:HH:mm} - {t.DatSan.KhungGio.GioKetThuc:HH:mm}"
                    : "N/A",
                NgayDat = t.DatSan?.NgayDat ?? DateOnly.FromDateTime(DateTime.Today),
                SoTien = t.SoTien,
                PhuongThucThanhToan = t.PhuongThucThanhToan,
                LoaiThanhToan = t.LoaiThanhToan,
                TrangThai = t.TrangThai,
                MaGiaoDich = t.MaGiaoDich,
                ThoiGianThanhToan = t.ThoiGianThanhToan
            }).ToList();

            var viewModel = new AdminThanhToanListViewModel
            {
                Items = items,
                Search = search,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                TrangThai = trangThai,
                PhuongThuc = phuongThuc,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                TongDoanhThu = tongDoanhThu,
                TongTienMat = tongTienMat,
                TongChuyenKhoan = tongChuyenKhoan,
                TongHoanCoc = tongHoanCoc
            };

            return View(viewModel);
        }

        /// <summary>
        /// Xác nhận đã nhận tiền chuyển khoản (QR) cho giao dịch đang 'Chờ thanh toán'
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToan(int id, string? returnUrl = null)
        {
            var payment = await _context.ThanhToans
                .Include(t => t.DatSan)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch thanh toán.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            payment.TrangThai = "Đã thanh toán";
            payment.ThoiGianThanhToan = DateTime.Now;
            await _context.SaveChangesAsync();

            // Gửi thông báo cho khách
            var tieuDe = $"Xác nhận thanh toán giao dịch #{payment.Id} thành công";
            var noiDung = $"Ban Quản Lý đã xác nhận thanh toán thành công số tiền {payment.SoTien:N0} đ (Phương thức: {payment.PhuongThucThanhToan}) cho đơn đặt sân #{payment.DatSanId}.";
            await _notificationService.GuiThongBaoAsync(payment.DatSan.NguoiDungId, tieuDe, noiDung, "ThanhToan");

            TempData["SuccessMessage"] = $"Đã xác nhận thanh toán thành công {payment.SoTien:N0} đ cho giao dịch #{id}!";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Ghi nhận thu tiền mặt phần còn lại của đơn đặt sân tại quầy
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThuTienMat(int datSanId, decimal soTien, string? ghiChu, string? returnUrl = null)
        {
            if (soTien <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền thu phải lớn hơn 0.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            var datSan = await _context.DatSans
                .Include(d => d.NguoiDung)
                .FirstOrDefaultAsync(d => d.Id == datSanId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            var payment = new ThanhToan
            {
                DatSanId = datSanId,
                SoTien = soTien,
                PhuongThucThanhToan = "Tiền mặt",
                LoaiThanhToan = string.IsNullOrWhiteSpace(ghiChu) ? "Thu tiền mặt tại quầy" : ghiChu,
                TrangThai = "Đã thanh toán",
                MaGiaoDich = $"TM{datSanId:D5}{DateTime.Now:HHmmss}",
                ThoiGianThanhToan = DateTime.Now
            };

            _context.ThanhToans.Add(payment);
            await _context.SaveChangesAsync();

            // Bắn thông báo cho khách hàng
            var tieuDe = $"Biên lai thu tiền mặt đơn #{datSanId}";
            var noiDung = $"Ban Quản Lý đã thu {soTien:N0} đ tiền mặt tại quầy cho đơn đặt sân #{datSanId}. Cảm ơn quý khách!";
            await _notificationService.GuiThongBaoAsync(datSan.NguoiDungId, tieuDe, noiDung, "ThanhToan");

            TempData["SuccessMessage"] = $"Đã lập biên lai thu tiền mặt {soTien:N0} đ cho đơn #{datSanId} thành công!";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xử lý hoàn cọc: Ghi nhận hoàn tiền cọc cho khách khi đơn bị hủy/từ chối
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoanCoc(int datSanId, decimal soTienHoan, string lyDo, string? returnUrl = null)
        {
            if (soTienHoan <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền hoàn cọc phải lớn hơn 0.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            var datSan = await _context.DatSans
                .Include(d => d.NguoiDung)
                .FirstOrDefaultAsync(d => d.Id == datSanId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân cần hoàn cọc.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            var payment = new ThanhToan
            {
                DatSanId = datSanId,
                SoTien = soTienHoan,
                PhuongThucThanhToan = "Chuyển khoản / Tiền mặt",
                LoaiThanhToan = "Hoàn cọc",
                TrangThai = "Đã hoàn tiền",
                MaGiaoDich = $"HC{datSanId:D5}{DateTime.Now:HHmmss}",
                ThoiGianThanhToan = DateTime.Now
            };

            _context.ThanhToans.Add(payment);
            await _context.SaveChangesAsync();

            // Gửi thông báo hoàn tiền cho khách
            var tieuDe = $"Xác nhận hoàn cọc đơn đặt sân #{datSanId}";
            var noiDung = $"Ban Quản Lý đã hoàn trả số tiền cọc {soTienHoan:N0} đ cho đơn #{datSanId}. Lý do: {(string.IsNullOrWhiteSpace(lyDo) ? "Hủy đơn theo thỏa thuận" : lyDo)}.";
            await _notificationService.GuiThongBaoAsync(datSan.NguoiDungId, tieuDe, noiDung, "HoanTien");

            TempData["SuccessMessage"] = $"Đã ghi nhận hoàn cọc {soTienHoan:N0} đ cho đơn #{datSanId} thành công!";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Trang chi tiết Hóa đơn và Xuất/In hóa đơn trực quan
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> HoaDon(int id)
        {
            // id có thể là DatSanId hoặc HoaDon.Id
            var booking = await _context.DatSans
                .Include(d => d.NguoiDung)
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.HoaDon)
                .Include(d => d.ThanhToans)
                .Include(d => d.ChiTietDichVuDats)
                    .ThenInclude(c => c.DichVu)
                .FirstOrDefaultAsync(d => d.Id == id || (d.HoaDon != null && d.HoaDon.Id == id));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin hóa đơn cho đơn đặt này.";
                return RedirectToAction(nameof(Index));
            }

            // Tự động tạo Hóa đơn nếu đơn đã hoàn thành nhưng chưa có mã
            if (booking.HoaDon == null)
            {
                var newInvoice = new HoaDon
                {
                    DatSanId = booking.Id,
                    MaHoaDon = $"HD{booking.Id:D5}{DateTime.Now:yyyyMMdd}",
                    NgayXuat = DateTime.Now
                };
                _context.HoaDons.Add(newInvoice);
                await _context.SaveChangesAsync();
                booking.HoaDon = newInvoice;
            }

            var paid = booking.ThanhToans
                .Where(t => t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                .Sum(t => t.SoTien);

            var services = booking.ChiTietDichVuDats.Select(c => new AdminDatSanDichVuItemViewModel
            {
                DichVuId = c.DichVuId,
                TenDichVu = c.DichVu?.TenDichVu ?? "Dịch vụ",
                DonViTinh = c.DichVu?.DonViTinh ?? "Lần",
                DonGia = c.DichVu != null ? c.DichVu.GiaDichVu : c.DonGia,
                SoLuong = c.SoLuong
            }).ToList();

            var tongTienDichVu = services.Sum(s => s.ThanhTien);
            var tienSan = Math.Max(0, booking.TongTien - tongTienDichVu);

            var viewModel = new AdminHoaDonDetailViewModel
            {
                MaHoaDon = booking.HoaDon.MaHoaDon,
                NgayXuat = booking.HoaDon.NgayXuat,
                DatSanId = booking.Id,
                TenKhachHang = booking.NguoiDung?.HoTen ?? "Khách hàng",
                SoDienThoai = booking.NguoiDung?.SoDienThoai ?? "",
                Email = booking.NguoiDung?.Email ?? "",
                TenSan = booking.SanBong?.TenSan ?? "Sân bóng",
                LoaiSan = booking.SanBong?.LoaiSan ?? "Sân 5 người",
                KhungGioDisplay = booking.KhungGio != null
                    ? $"{booking.KhungGio.GioBatDau:HH:mm} - {booking.KhungGio.GioKetThuc:HH:mm}"
                    : "N/A",
                NgayDat = booking.NgayDat,
                TienSan = tienSan,
                DichVus = services,
                TongTienDichVu = tongTienDichVu,
                TongTien = booking.TongTien,
                DaThanhToan = paid,
                TrangThaiDon = booking.TrangThai
            };

            return View(viewModel);
        }
    }
}


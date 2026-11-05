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
    public class DatSanController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly NotificationService _notificationService;

        public DatSanController(QLSanBongDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Danh sách đơn đặt sân với các bộ lọc thông minh: tìm theo mã đơn / tên / SĐT khách, 
        /// lọc trạng thái (Chờ duyệt, Đã xác nhận, Đang đá, Hoàn thành, Đã hủy), lọc theo ngày đặt và theo sân, có phân trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            string? trangThai = null,
            DateTime? ngayDat = null,
            int? sanBongId = null,
            string? status = null, // Hỗ trợ tương thích ngược query param status
            int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 10;

            // Đồng bộ query parameter
            if (!string.IsNullOrWhiteSpace(status) && status != "all" && string.IsNullOrWhiteSpace(trangThai))
            {
                trangThai = status;
            }

            var query = _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.NguoiDung)
                .Include(d => d.ThanhToans)
                .Include(d => d.ChiTietDichVuDats)
                    .ThenInclude(c => c.DichVu)
                .AsQueryable();

            // 5 Bộ đếm trạng thái tổng quan
            var countChoDuyet = await _context.DatSans.CountAsync(d => d.TrangThai == "Chờ duyệt");
            var countDaXacNhan = await _context.DatSans.CountAsync(d => d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đã duyệt");
            var countDangDa = await _context.DatSans.CountAsync(d => d.TrangThai == "Đang đá");
            var countHoanThanh = await _context.DatSans.CountAsync(d => d.TrangThai == "Hoàn thành");
            var countDaHuy = await _context.DatSans.CountAsync(d => d.TrangThai == "Từ chối" || d.TrangThai == "Đã hủy");

            // 1. Tìm kiếm theo mã đơn, tên, SĐT khách hoặc tên sân
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d => d.Id.ToString() == s ||
                                         d.NguoiDung.HoTen.ToLower().Contains(s) ||
                                         d.NguoiDung.SoDienThoai.Contains(s) ||
                                         d.SanBong.TenSan.ToLower().Contains(s));
            }

            // 2. Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "all")
            {
                if (trangThai == "Đã xác nhận" || trangThai == "Đã duyệt")
                {
                    query = query.Where(d => d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đã duyệt");
                }
                else if (trangThai == "Đã hủy" || trangThai == "Từ chối")
                {
                    query = query.Where(d => d.TrangThai == "Đã hủy" || d.TrangThai == "Từ chối");
                }
                else
                {
                    query = query.Where(d => d.TrangThai == trangThai);
                }
            }

            // 3. Lọc theo ngày đặt
            if (ngayDat.HasValue)
            {
                var targetDate = DateOnly.FromDateTime(ngayDat.Value);
                query = query.Where(d => d.NgayDat == targetDate);
            }

            // 4. Lọc theo sân bóng
            if (sanBongId.HasValue && sanBongId.Value > 0)
            {
                query = query.Where(d => d.SanBongId == sanBongId.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var rawBookings = await query
                .OrderByDescending(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rawBookings.Select(b =>
            {
                var paid = b.ThanhToans
                    .Where(t => t.TrangThai == "Thành công" || t.TrangThai == "Đã thanh toán")
                    .Sum(t => t.SoTien);

                return new AdminBookingItemViewModel
                {
                    Id = b.Id,
                    SanBongId = b.SanBongId,
                    TenSan = b.SanBong?.TenSan ?? "Sân bóng",
                    LoaiSan = b.SanBong?.LoaiSan ?? "Sân 5 người",
                    NguoiDungId = b.NguoiDungId,
                    TenNguoiDung = b.NguoiDung?.HoTen ?? "Khách hàng",
                    SoDienThoai = b.NguoiDung?.SoDienThoai ?? "",
                    Email = b.NguoiDung?.Email ?? "",
                    NgayDat = b.NgayDat,
                    GioBatDau = b.KhungGio?.GioBatDau ?? new TimeOnly(17, 30),
                    GioKetThuc = b.KhungGio?.GioKetThuc ?? new TimeOnly(19, 0),
                    TongTien = b.TongTien,
                    DaThanhToan = paid,
                    TrangThai = b.TrangThai,
                    NgayTao = b.NgayTao,
                    DanhSachDichVu = b.ChiTietDichVuDats.Select(c => $"{c.DichVu.TenDichVu} (x{c.SoLuong})").ToList()
                };
            }).ToList();

            var pitches = await _context.SanBongs.OrderBy(s => s.Id).ToListAsync();

            var viewModel = new AdminDatSanListViewModel
            {
                Items = items,
                Search = search,
                TrangThai = trangThai,
                NgayDat = ngayDat,
                SanBongId = sanBongId,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                CountChoDuyet = countChoDuyet,
                CountDaXacNhan = countDaXacNhan,
                CountDangDa = countDangDa,
                CountHoanThanh = countHoanThanh,
                CountDaHuy = countDaHuy,
                DanhSachSan = pitches
            };

            return View(viewModel);
        }

        /// <summary>
        /// Xem chi tiết đơn đặt sân: Thông tin khách hàng, Sân bóng, Khung giờ, Dịch vụ, Thanh toán, Hóa đơn & Lịch sử thông báo.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.NguoiDung)
                .Include(d => d.HoaDon)
                .Include(d => d.ThanhToans)
                .Include(d => d.ChiTietDichVuDats)
                    .ThenInclude(c => c.DichVu)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt sân.";
                return RedirectToAction(nameof(Index));
            }

            var paid = booking.ThanhToans
                .Where(t => t.TrangThai == "Thành công" || t.TrangThai == "Đã thanh toán")
                .Sum(t => t.SoTien);

            var services = booking.ChiTietDichVuDats.Select(c => new AdminDatSanDichVuItemViewModel
            {
                DichVuId = c.DichVuId,
                TenDichVu = c.DichVu?.TenDichVu ?? "Dịch vụ",
                DonViTinh = c.DichVu?.DonViTinh ?? "Lần",
                DonGia = c.DichVu != null ? c.DichVu.GiaDichVu : c.DonGia,
                SoLuong = c.SoLuong
            }).ToList();

            var payments = booking.ThanhToans
                .OrderByDescending(t => t.Id)
                .Select(t => new AdminDatSanThanhToanItemViewModel
                {
                    Id = t.Id,
                    SoTien = t.SoTien,
                    PhuongThucThanhToan = t.PhuongThucThanhToan,
                    LoaiThanhToan = t.LoaiThanhToan,
                    TrangThai = t.TrangThai,
                    MaGiaoDich = t.MaGiaoDich,
                    ThoiGianThanhToan = t.ThoiGianThanhToan
                }).ToList();

            // Tìm các thông báo gửi cho khách hàng này liên quan đến mã đơn
            var notifications = await _context.ThongBaos
                .Where(t => t.NguoiDungId == booking.NguoiDungId && t.NoiDung.Contains($"#{booking.Id}"))
                .OrderByDescending(t => t.Id)
                .Select(t => new AdminDatSanThongBaoItemViewModel
                {
                    Id = t.Id,
                    TieuDe = t.TieuDe,
                    NoiDung = t.NoiDung,
                    LoaiThongBao = t.LoaiThongBao,
                    NgayTao = t.NgayTao
                }).ToListAsync();

            var viewModel = new AdminDatSanDetailViewModel
            {
                Id = booking.Id,
                NgayDat = booking.NgayDat,
                TongTien = booking.TongTien,
                DaThanhToan = paid,
                TrangThai = booking.TrangThai,
                NgayTao = booking.NgayTao,
                NguoiDungId = booking.NguoiDungId,
                TenKhachHang = booking.NguoiDung?.HoTen ?? "Khách hàng",
                SoDienThoai = booking.NguoiDung?.SoDienThoai ?? "",
                Email = booking.NguoiDung?.Email ?? "",
                SanBongId = booking.SanBongId,
                TenSan = booking.SanBong?.TenSan ?? "Sân bóng",
                LoaiSan = booking.SanBong?.LoaiSan ?? "Sân 5 người",
                AnhSan = booking.SanBong?.AnhSan,
                KhungGioId = booking.KhungGioId,
                KhungGioDisplay = $"{booking.KhungGio?.GioBatDau:HH:mm} - {booking.KhungGio?.GioKetThuc:HH:mm}",
                DaXuatHoaDon = booking.HoaDon != null,
                MaHoaDon = booking.HoaDon?.MaHoaDon,
                NgayXuatHoaDon = booking.HoaDon?.NgayXuat,
                DanhSachDichVu = services,
                DanhSachThanhToan = payments,
                DanhSachThongBao = notifications
            };

            return View(viewModel);
        }

        /// <summary>
        /// Xử lý chuyển trạng thái đơn theo đúng quy tắc một chiều:
        /// 'Chờ duyệt' -> 'Đã xác nhận' hoặc 'Từ chối' (bắt buộc lý do)
        /// 'Đã xác nhận' -> 'Đang đá' (Nhận sân) hoặc 'Hoàn thành' (Trả sân) hoặc 'Đã hủy' (kèm lý do)
        /// 'Đang đá' -> 'Hoàn thành' (Trả sân)
        /// 'Hoàn thành', 'Đã hủy', 'Từ chối' là trạng thái cuối, tuyệt đối không cho phép đổi ngược lại.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string trangThaiMoi, string? lyDo = null, string? returnUrl = null)
        {
            var booking = await _context.DatSans
                .Include(d => d.NguoiDung)
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            var currentStatus = booking.TrangThai;

            // KIỂM TRA TRẠNG THÁI CUỐI
            if (currentStatus == "Hoàn thành" || currentStatus == "Đã hủy" || currentStatus == "Từ chối")
            {
                TempData["ErrorMessage"] = $"Đơn đặt #{id} đang ở trạng thái cuối '{currentStatus}', tuyệt đối không thể thay đổi ngược lại!";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            // KIỂM TRA QUY TẮC MỘT CHIỀU
            if (currentStatus == "Chờ duyệt")
            {
                if (trangThaiMoi != "Đã xác nhận" && trangThaiMoi != "Đã duyệt" && trangThaiMoi != "Từ chối" && trangThaiMoi != "Đã hủy")
                {
                    TempData["ErrorMessage"] = "Đơn 'Chờ duyệt' chỉ có thể chuyển sang 'Đã xác nhận' hoặc 'Từ chối'.";
                    return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
                }

                if ((trangThaiMoi == "Từ chối" || trangThaiMoi == "Đã hủy") && string.IsNullOrWhiteSpace(lyDo))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập lý do khi từ chối / hủy đơn đặt sân.";
                    return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
                }
            }
            else if (currentStatus == "Đã xác nhận" || currentStatus == "Đã duyệt")
            {
                if (trangThaiMoi != "Đang đá" && trangThaiMoi != "Hoàn thành" && trangThaiMoi != "Đã hủy")
                {
                    TempData["ErrorMessage"] = "Đơn 'Đã xác nhận' chỉ có thể chuyển sang 'Đang đá', 'Hoàn thành' hoặc 'Đã hủy'.";
                    return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
                }
            }
            else if (currentStatus == "Đang đá")
            {
                if (trangThaiMoi != "Hoàn thành")
                {
                    TempData["ErrorMessage"] = "Đơn đang trong trận ('Đang đá') chỉ có thể kết thúc chuyển sang 'Hoàn thành'.";
                    return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
                }
            }

            // Cập nhật trạng thái
            booking.TrangThai = trangThaiMoi;

            // Nếu chuyển thành 'Hoàn thành' -> Tự động sinh Hóa đơn nếu chưa có
            if (trangThaiMoi == "Hoàn thành")
            {
                var hasInvoice = await _context.HoaDons.AnyAsync(h => h.DatSanId == booking.Id);
                if (!hasInvoice)
                {
                    _context.HoaDons.Add(new HoaDon
                    {
                        DatSanId = booking.Id,
                        MaHoaDon = $"HD{booking.Id:D5}{DateTime.Now:yyyyMMdd}",
                        NgayXuat = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Bắn thông báo đa kênh (Web + Gmail SMTP)
            await _notificationService.ThongBaoTrangThaiDatSanAsync(id, trangThaiMoi, lyDo);

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn #{id} thành '{trangThaiMoi}' và tự động gửi thông báo cho khách hàng.";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Bàn giao sân: Nhận sân (Chuyển trạng thái 'Đã xác nhận' -> 'Đang đá')
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhanSan(int id, string? returnUrl = null)
        {
            var booking = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            if (booking.TrangThai != "Đã xác nhận" && booking.TrangThai != "Đã duyệt")
            {
                TempData["ErrorMessage"] = $"Chỉ có thể bàn giao nhận sân cho đơn đang ở trạng thái 'Đã xác nhận' (Trạng thái hiện tại: {booking.TrangThai}).";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            booking.TrangThai = "Đang đá";
            await _context.SaveChangesAsync();

            // Tạo thông báo nhận sân
            var tieuDe = $"Bàn giao nhận sân #{booking.Id} thành công";
            var noiDung = $"Bạn đã hoàn tất thủ tục nhận sân tại {booking.SanBong?.TenSan ?? "Sân bóng"} vào lúc {DateTime.Now:HH:mm}. Chúc đội bóng của bạn có trận cầu hấp dẫn và an toàn!";
            await _notificationService.GuiThongBaoAsync(booking.NguoiDungId, tieuDe, noiDung, "NhanSan");

            TempData["SuccessMessage"] = $"Đã bàn giao nhận sân #{id} thành công! Trận đấu hiện đang diễn ra ('Đang đá').";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Bàn giao sân: Trả sân (Chuyển trạng thái 'Đang đá'/'Đã xác nhận' -> 'Hoàn thành' & sinh Hóa đơn)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TraSan(int id, string? tinhTrangSan = null, string? returnUrl = null)
        {
            var booking = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            if (booking.TrangThai != "Đang đá" && booking.TrangThai != "Đã xác nhận" && booking.TrangThai != "Đã duyệt")
            {
                TempData["ErrorMessage"] = $"Không thể làm thủ tục trả sân cho đơn có trạng thái '{booking.TrangThai}'.";
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            booking.TrangThai = "Hoàn thành";

            // Tự động sinh bản ghi Hóa đơn điện tử nếu chưa có
            var existingInvoice = await _context.HoaDons.FirstOrDefaultAsync(h => h.DatSanId == booking.Id);
            if (existingInvoice == null)
            {
                _context.HoaDons.Add(new HoaDon
                {
                    DatSanId = booking.Id,
                    MaHoaDon = $"HD{booking.Id:D5}{DateTime.Now:yyyyMMdd}",
                    NgayXuat = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Bắn thông báo mời viết đánh giá & tri ân
            await _notificationService.ThongBaoTrangThaiDatSanAsync(id, "Hoàn thành", tinhTrangSan);

            TempData["SuccessMessage"] = $"Đã xác nhận trả sân #{id} thành công! Hệ thống đã xuất Hóa đơn và gửi email mời khách viết đánh giá.";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Tương thích ngược action DuyetDon
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDon(int id, string? ghiChu, string? returnUrl = null)
        {
            return await UpdateStatus(id, "Đã xác nhận", ghiChu, returnUrl);
        }

        /// <summary>
        /// Tương thích ngược action TuChoiDon
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoiDon(int id, string lyDo, string? returnUrl = null)
        {
            return await UpdateStatus(id, "Từ chối", lyDo, returnUrl);
        }

        /// <summary>
        /// Tương thích ngược action HoanThanhDon
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoanThanhDon(int id, string? returnUrl = null)
        {
            return await TraSan(id, null, returnUrl);
        }

        /// <summary>
        /// Danh sách yêu cầu đổi lịch từ sinh viên (dbo.YeuCauDoiLich)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> YeuCauDoiLich(string? trangThai = "all")
        {
            var query = _context.YeuCauDoiLiches
                .Include(y => y.DatSan)
                    .ThenInclude(d => d.SanBong)
                .Include(y => y.DatSan)
                    .ThenInclude(d => d.KhungGio)
                .Include(y => y.DatSan)
                    .ThenInclude(d => d.NguoiDung)
                .Include(y => y.KhungGioMoi)
                .Include(y => y.SanBongMoi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "all")
            {
                query = query.Where(y => y.TrangThai == trangThai);
            }

            var rawList = await query
                .OrderByDescending(y => y.Id)
                .ToListAsync();

            var list = rawList.Select(y => new AdminRescheduleItemViewModel
            {
                Id = y.Id,
                DatSanId = y.DatSanId,
                NguoiDungId = y.DatSan?.NguoiDungId ?? 0,
                TenNguoiDung = y.DatSan?.NguoiDung?.HoTen ?? "Khách hàng",
                SoDienThoai = y.DatSan?.NguoiDung?.SoDienThoai ?? "",
                SanBongCuId = y.DatSan?.SanBongId ?? 0,
                TenSanCu = y.DatSan?.SanBong?.TenSan ?? "Sân cũ",
                NgayCu = y.DatSan?.NgayDat ?? DateOnly.FromDateTime(DateTime.Today),
                KhungGioCu = y.DatSan?.KhungGio != null
                    ? $"{y.DatSan.KhungGio.GioBatDau:HH:mm} - {y.DatSan.KhungGio.GioKetThuc:HH:mm}"
                    : "Ca cũ",
                SanBongMoiId = y.SanBongMoiId,
                TenSanMoi = y.SanBongMoi?.TenSan ?? "Sân mới",
                NgayMoi = y.NgayDatMoi,
                KhungGioMoiId = y.KhungGioMoiId,
                KhungGioMoi = y.KhungGioMoi != null
                    ? $"{y.KhungGioMoi.GioBatDau:HH:mm} - {y.KhungGioMoi.GioKetThuc:HH:mm}"
                    : "Ca mới",
                LyDo = y.LyDo ?? "",
                TrangThai = y.TrangThai,
                NgayTao = y.NgayTao
            }).ToList();

            ViewBag.CurrentTrangThai = trangThai;
            ViewBag.CountChoDuyet = await _context.YeuCauDoiLiches.CountAsync(y => y.TrangThai == "Chờ duyệt");
            ViewBag.CountDaDuyet = await _context.YeuCauDoiLiches.CountAsync(y => y.TrangThai == "Đã duyệt");
            ViewBag.CountTuChoi = await _context.YeuCauDoiLiches.CountAsync(y => y.TrangThai == "Từ chối");

            return View(list);
        }

        /// <summary>
        /// Admin Phê duyệt đổi lịch -> Kiểm tra trùng lịch ca mới và lịch bảo trì trước khi chấp thuận.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDoiLich(int id)
        {
            var yeuCau = await _context.YeuCauDoiLiches
                .Include(y => y.DatSan)
                .Include(y => y.KhungGioMoi)
                .Include(y => y.SanBongMoi)
                .FirstOrDefaultAsync(y => y.Id == id);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu đổi lịch.";
                return RedirectToAction(nameof(YeuCauDoiLich));
            }

            if (yeuCau.TrangThai != "Chờ duyệt")
            {
                TempData["ErrorMessage"] = $"Yêu cầu đổi lịch này đã ở trạng thái '{yeuCau.TrangThai}'.";
                return RedirectToAction(nameof(YeuCauDoiLich));
            }

            // 1. KIỂM TRA TRÙNG CA MỚI VỚI ĐƠN KHÁCH KHÁC
            var isSlotTaken = await _context.DatSans.AnyAsync(d =>
                d.Id != yeuCau.DatSanId &&
                d.SanBongId == yeuCau.SanBongMoiId &&
                d.KhungGioId == yeuCau.KhungGioMoiId &&
                d.NgayDat == yeuCau.NgayDatMoi &&
                (d.TrangThai == "Đã duyệt" || d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đang đá" || d.TrangThai == "Hoàn thành"));

            if (isSlotTaken)
            {
                TempData["ErrorMessage"] = "CHẶN DUYỆT ĐỔI LỊCH! Ca đề xuất mới này đã có một đơn khác được xác nhận giữ chỗ từ trước. Vui lòng từ chối hoặc chọn ca khác.";
                return RedirectToAction(nameof(YeuCauDoiLich));
            }

            // 2. KIỂM TRA TRÙNG LỊCH BẢO TRÌ SÂN MỚI
            var khungGioMoi = yeuCau.KhungGioMoi ?? await _context.KhungGios.FindAsync(yeuCau.KhungGioMoiId);
            if (khungGioMoi != null)
            {
                var slotStart = yeuCau.NgayDatMoi.ToDateTime(khungGioMoi.GioBatDau);
                var slotEnd = yeuCau.NgayDatMoi.ToDateTime(khungGioMoi.GioKetThuc);

                var isUnderMaintenance = await _context.LichBaoTris.AnyAsync(m =>
                    m.SanBongId == yeuCau.SanBongMoiId &&
                    m.ThoiGianBatDau < slotEnd &&
                    m.ThoiGianKetThuc > slotStart);

                if (isUnderMaintenance)
                {
                    TempData["ErrorMessage"] = "CHẶN DUYỆT ĐỔI LỊCH! Sân mới đang có lịch bảo dưỡng mặt sân/hệ thống chiếu sáng vào khung giờ này.";
                    return RedirectToAction(nameof(YeuCauDoiLich));
                }
            }

            // 3. Cập nhật ngày và khung giờ mới cho đơn đặt sân gốc
            if (yeuCau.DatSan != null)
            {
                yeuCau.DatSan.SanBongId = yeuCau.SanBongMoiId;
                yeuCau.DatSan.KhungGioId = yeuCau.KhungGioMoiId;
                yeuCau.DatSan.NgayDat = yeuCau.NgayDatMoi;
                yeuCau.DatSan.TrangThai = "Đã xác nhận";
            }

            yeuCau.TrangThai = "Đã duyệt";
            await _context.SaveChangesAsync();

            // Bắn thông báo đa kênh (Web + Gmail SMTP)
            await _notificationService.ThongBaoTrangThaiDoiLichAsync(id, "Đã duyệt");

            TempData["SuccessMessage"] = $"Đã chấp thuận đổi lịch #{id}! Đơn đặt #{yeuCau.DatSanId} đã cập nhật sang ca mới ({yeuCau.NgayDatMoi:dd/MM/yyyy}) và tự động gửi email cho khách.";
            return RedirectToAction(nameof(YeuCauDoiLich));
        }

        /// <summary>
        /// Admin Từ chối yêu cầu đổi lịch -> Kích hoạt lưu ThongBao & Bắn Gmail SMTP.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoiDoiLich(int id, string lyDo)
        {
            var yeuCau = await _context.YeuCauDoiLiches.FindAsync(id);
            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu đổi lịch.";
                return RedirectToAction(nameof(YeuCauDoiLich));
            }

            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối yêu cầu đổi lịch.";
                return RedirectToAction(nameof(YeuCauDoiLich));
            }

            yeuCau.TrangThai = "Từ chối";
            await _context.SaveChangesAsync();

            // Bắn thông báo đa kênh
            await _notificationService.ThongBaoTrangThaiDoiLichAsync(id, "Từ chối", lyDo);

            TempData["SuccessMessage"] = $"Đã từ chối yêu cầu đổi lịch #{id}. Hệ thống đã gửi email thông báo lý do đến khách hàng.";
            return RedirectToAction(nameof(YeuCauDoiLich));
        }

        /// <summary>
        /// Tính năng [Nên có]: Đặt hộ khách tại quầy (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateTaiQuay()
        {
            var customers = await _context.NguoiDungs
                .Where(n => n.TrangThai == "Hoạt động")
                .OrderBy(n => n.HoTen)
                .ToListAsync();

            var pitches = await _context.SanBongs
                .Where(s => s.TrangThai != "Ngừng hoạt động")
                .OrderBy(s => s.Id)
                .ToListAsync();

            var slots = await _context.KhungGios
                .OrderBy(k => k.GioBatDau)
                .ToListAsync();

            var services = await _context.DichVus
                .OrderBy(d => d.Id)
                .Select(d => new AdminSelectedDichVuViewModel
                {
                    DichVuId = d.Id,
                    TenDichVu = d.TenDichVu,
                    DonGia = d.GiaDichVu,
                    DonViTinh = d.DonViTinh,
                    SoLuong = 0
                }).ToListAsync();

            var model = new AdminCreateTaiQuayViewModel
            {
                DanhSachKhachHang = customers,
                DanhSachSan = pitches,
                DanhSachKhungGio = slots,
                DichVus = services,
                NgayDat = DateOnly.FromDateTime(DateTime.Today),
                SanBongId = pitches.FirstOrDefault()?.Id ?? 0,
                KhungGioId = slots.FirstOrDefault()?.Id ?? 0
            };

            return View(model);
        }

        /// <summary>
        /// Tính năng [Nên có]: Đặt hộ khách tại quầy (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTaiQuay(AdminCreateTaiQuayViewModel model, Dictionary<int, int>? soLuongDichVu)
        {
            var pitches = await _context.SanBongs.Where(s => s.TrangThai != "Ngừng hoạt động").OrderBy(s => s.Id).ToListAsync();
            var slots = await _context.KhungGios.OrderBy(k => k.GioBatDau).ToListAsync();
            var customers = await _context.NguoiDungs.Where(n => n.TrangThai == "Hoạt động").OrderBy(n => n.HoTen).ToListAsync();
            var allServices = await _context.DichVus.OrderBy(d => d.Id).ToListAsync();

            // 1. Kiểm tra xung đột trùng lịch đặt
            var isTaken = await _context.DatSans.AnyAsync(d =>
                d.SanBongId == model.SanBongId &&
                d.KhungGioId == model.KhungGioId &&
                d.NgayDat == model.NgayDat &&
                (d.TrangThai == "Đã duyệt" || d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đang đá" || d.TrangThai == "Hoàn thành"));

            if (isTaken)
            {
                ModelState.AddModelError(string.Empty, "Sân bóng này đã có khách đặt trong ca đã chọn. Vui lòng chọn ca hoặc sân khác.");
            }

            // 2. Kiểm tra xung đột bảo trì
            var selectedSlot = await _context.KhungGios.FindAsync(model.KhungGioId);
            if (selectedSlot != null)
            {
                var start = model.NgayDat.ToDateTime(selectedSlot.GioBatDau);
                var end = model.NgayDat.ToDateTime(selectedSlot.GioKetThuc);

                var isMaintenance = await _context.LichBaoTris.AnyAsync(m =>
                    m.SanBongId == model.SanBongId &&
                    m.ThoiGianBatDau < end &&
                    m.ThoiGianKetThuc > start);

                if (isMaintenance)
                {
                    ModelState.AddModelError(string.Empty, "Sân bóng đang trong lịch bảo trì vào khung giờ này.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.DanhSachKhachHang = customers;
                model.DanhSachSan = pitches;
                model.DanhSachKhungGio = slots;
                model.DichVus = allServices.Select(d => new AdminSelectedDichVuViewModel
                {
                    DichVuId = d.Id,
                    TenDichVu = d.TenDichVu,
                    DonGia = d.GiaDichVu,
                    DonViTinh = d.DonViTinh,
                    SoLuong = soLuongDichVu != null && soLuongDichVu.TryGetValue(d.Id, out var q) ? q : 0
                }).ToList();
                return View(model);
            }

            // 3. Xác định tài khoản khách hàng (khách đã có hoặc tạo tài khoản khách vãng lai)
            int customerId;
            if (model.NguoiDungId.HasValue && model.NguoiDungId.Value > 0)
            {
                customerId = model.NguoiDungId.Value;
            }
            else
            {
                var cleanPhone = model.SoDienThoai.Trim();
                var existingUser = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.SoDienThoai == cleanPhone);
                if (existingUser != null)
                {
                    customerId = existingUser.Id;
                }
                else
                {
                    // Lấy quyền khách hàng mặc định
                    var customerRole = await _context.QuyenHans.FirstOrDefaultAsync(r => r.TenQuyen.Contains("Khách"));
                    var roleId = customerRole?.Id ?? 2;

                    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<NguoiDung>();
                    var newUser = new NguoiDung
                    {
                        HoTen = model.TenKhachHang.Trim(),
                        SoDienThoai = cleanPhone,
                        Email = !string.IsNullOrWhiteSpace(model.Email) ? model.Email.Trim() : $"{cleanPhone}@utcstadium.local",
                        MatKhauMaHoa = "",
                        TrangThai = "Hoạt động",
                        QuyenHanId = roleId,
                        NgayTao = DateTime.Now
                    };
                    newUser.MatKhauMaHoa = hasher.HashPassword(newUser, "UTC@123456");

                    _context.NguoiDungs.Add(newUser);
                    await _context.SaveChangesAsync();
                    customerId = newUser.Id;
                }
            }

            // 4. Tính tiền sân
            var dayOfWeekName = model.NgayDat.DayOfWeek switch
            {
                DayOfWeek.Saturday or DayOfWeek.Sunday => "Cuối tuần",
                _ => "Ngày thường"
            };

            var priceSetting = await _context.ThietLapGia
                .FirstOrDefaultAsync(t => t.SanBongId == model.SanBongId &&
                                          t.KhungGioId == model.KhungGioId &&
                                          t.NgayTrongTuan == dayOfWeekName);

            var selectedPitch = await _context.SanBongs.FindAsync(model.SanBongId);
            decimal tienSan = priceSetting?.GiaThue ?? (selectedPitch?.LoaiSan.Contains("7") == true ? 400000m : 250000m);

            // 5. Tính tiền dịch vụ
            decimal tienDichVu = 0;
            var serviceDetails = new List<ChiTietDichVuDat>();
            if (soLuongDichVu != null)
            {
                foreach (var kvp in soLuongDichVu)
                {
                    if (kvp.Value > 0)
                    {
                        var srv = allServices.FirstOrDefault(s => s.Id == kvp.Key);
                        if (srv != null)
                        {
                            tienDichVu += srv.GiaDichVu * kvp.Value;
                            serviceDetails.Add(new ChiTietDichVuDat
                            {
                                DichVuId = srv.Id,
                                SoLuong = kvp.Value,
                                DonGia = srv.GiaDichVu
                            });
                        }
                    }
                }
            }

            decimal tongTien = tienSan + tienDichVu;

            // 6. Tạo đơn đặt sân
            var datSan = new DatSan
            {
                NguoiDungId = customerId,
                SanBongId = model.SanBongId,
                KhungGioId = model.KhungGioId,
                NgayDat = model.NgayDat,
                TongTien = tongTien,
                TrangThai = "Đã xác nhận", // Đặt trực tiếp tại quầy BQL
                NgayTao = DateTime.Now
            };

            _context.DatSans.Add(datSan);
            await _context.SaveChangesAsync();

            // Lưu chi tiết dịch vụ
            if (serviceDetails.Any())
            {
                foreach (var detail in serviceDetails)
                {
                    detail.DatSanId = datSan.Id;
                    _context.ChiTietDichVuDats.Add(detail);
                }
                await _context.SaveChangesAsync();
            }

            // Lưu bản ghi thanh toán tại quầy nếu có
            if (model.SoTienThanhToan > 0)
            {
                _context.ThanhToans.Add(new ThanhToan
                {
                    DatSanId = datSan.Id,
                    SoTien = model.SoTienThanhToan,
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    LoaiThanhToan = model.SoTienThanhToan >= tongTien ? "Toàn bộ" : "Đặt cọc",
                    TrangThai = "Đã thanh toán",
                    MaGiaoDich = $"QUAY{datSan.Id:D5}{DateTime.Now:HHmmss}",
                    ThoiGianThanhToan = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // Gửi thông báo cho khách hàng
            var tieuDe = $"Tạo đơn đặt sân #{datSan.Id} tại quầy thành công";
            var noiDung = $"Ban Quản Lý đã tạo đơn #{datSan.Id} cho bạn tại {selectedPitch?.TenSan ?? "Sân bóng"} ngày {datSan.NgayDat:dd/MM/yyyy}. Tổng tiền: {tongTien:N0} đ. Đã thanh toán: {model.SoTienThanhToan:N0} đ.";
            await _notificationService.GuiThongBaoAsync(customerId, tieuDe, noiDung, "DuyetDon");

            TempData["SuccessMessage"] = $"Đã tạo đơn đặt sân tại quầy #{datSan.Id} thành công cho khách hàng '{model.TenKhachHang}'!";
            return RedirectToAction(nameof(Details), new { id = datSan.Id });
        }
    }
}

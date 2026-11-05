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
    public class MaGiamGiaController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly NotificationService _notificationService;

        public MaGiamGiaController(QLSanBongDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Danh sách mã giảm giá / voucher: MaCode, % giảm, số tiền giảm, giá trị đơn tối thiểu, hiệu lực từ... đến...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? trangThai = null)
        {
            var now = DateTime.Now;
            var query = _context.MaGiamGia.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpper();
                query = query.Where(v => v.MaCode.ToUpper().Contains(s));
            }

            var allVouchers = await query
                .OrderByDescending(v => v.Id)
                .ToListAsync();

            var items = allVouchers.Select(v => new AdminMaGiamGiaItemViewModel
            {
                Id = v.Id,
                MaCode = v.MaCode,
                PhanTramGiam = v.PhanTramGiam,
                SoTienGiam = v.SoTienGiam,
                HieuLucTu = v.HieuLucTu,
                HieuLucDen = v.HieuLucDen,
                GiaTriDonToiThieu = v.GiaTriDonToiThieu
            }).ToList();

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "all")
            {
                items = items.Where(i => i.TrangThaiHienTai == trangThai).ToList();
            }

            var tongVoucher = allVouchers.Count;
            var conHieuLuc = allVouchers.Count(v => now >= v.HieuLucTu && now <= v.HieuLucDen);
            var hetHieuLuc = allVouchers.Count(v => now > v.HieuLucDen);

            var activeCustomers = await _context.NguoiDungs
                .Where(n => n.TrangThai == "Hoạt động")
                .OrderBy(n => n.HoTen)
                .ToListAsync();

            var viewModel = new AdminMaGiamGiaListViewModel
            {
                Items = items,
                Search = search,
                TrangThai = trangThai,
                TongVoucher = tongVoucher,
                ConHieuLuc = conHieuLuc,
                HetHieuLuc = hetHieuLuc,
                DanhSachKhachHang = activeCustomers
            };

            return View(viewModel);
        }

        /// <summary>
        /// Thêm mới mã giảm giá
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string maCode, int? phanTramGiam, decimal? soTienGiam, DateTime hieuLucTu, DateTime hieuLucDen, decimal? giaTriDonToiThieu)
        {
            if (string.IsNullOrWhiteSpace(maCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã Voucher / Code.";
                return RedirectToAction(nameof(Index));
            }

            if (hieuLucDen <= hieuLucTu)
            {
                TempData["ErrorMessage"] = "Thời gian hết hạn phải sau thời gian bắt đầu có hiệu lực.";
                return RedirectToAction(nameof(Index));
            }

            var cleanCode = maCode.Trim().ToUpper();
            var exists = await _context.MaGiamGia.AnyAsync(m => m.MaCode.ToUpper() == cleanCode);
            if (exists)
            {
                TempData["ErrorMessage"] = $"Mã Voucher '{cleanCode}' đã tồn tại trên hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var voucher = new MaGiamGium
            {
                MaCode = cleanCode,
                PhanTramGiam = phanTramGiam,
                SoTienGiam = soTienGiam,
                HieuLucTu = hieuLucTu,
                HieuLucDen = hieuLucDen,
                GiaTriDonToiThieu = giaTriDonToiThieu
            };

            _context.MaGiamGia.Add(voucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo mã giảm giá '{voucher.MaCode}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Chỉnh sửa mã giảm giá
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string maCode, int? phanTramGiam, decimal? soTienGiam, DateTime hieuLucTu, DateTime hieuLucDen, decimal? giaTriDonToiThieu)
        {
            var voucher = await _context.MaGiamGia.FindAsync(id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            if (hieuLucDen <= hieuLucTu)
            {
                TempData["ErrorMessage"] = "Thời gian hết hạn phải sau thời gian bắt đầu hiệu lực.";
                return RedirectToAction(nameof(Index));
            }

            var cleanCode = maCode.Trim().ToUpper();
            var isDuplicate = await _context.MaGiamGia.AnyAsync(m => m.Id != id && m.MaCode.ToUpper() == cleanCode);
            if (isDuplicate)
            {
                TempData["ErrorMessage"] = $"Mã Voucher '{cleanCode}' đã bị trùng với một mã khác.";
                return RedirectToAction(nameof(Index));
            }

            voucher.MaCode = cleanCode;
            voucher.PhanTramGiam = phanTramGiam;
            voucher.SoTienGiam = soTienGiam;
            voucher.HieuLucTu = hieuLucTu;
            voucher.HieuLucDen = hieuLucDen;
            voucher.GiaTriDonToiThieu = giaTriDonToiThieu;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật mã giảm giá '{voucher.MaCode}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Quy ước tắt mã: Vô hiệu hóa (tắt) mã giảm giá ngay lập tức bằng cách cập nhật HieuLucDen = DateTime.Now
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoHieuHoa(int id)
        {
            var voucher = await _context.MaGiamGia.FindAsync(id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            // Đặt thời gian hết hạn là ngay bây giờ để vô hiệu hóa
            voucher.HieuLucDen = DateTime.Now.AddSeconds(-1);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã vô hiệu hóa (tắt) mã giảm giá '{voucher.MaCode}'. Mã đã hết hạn hiệu lực ngay lập tức.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Bật lại hoặc gia hạn mã thêm số ngày
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaHan(int id, int soNgay = 7)
        {
            var voucher = await _context.MaGiamGia.FindAsync(id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            voucher.HieuLucDen = DateTime.Now.AddDays(soNgay > 0 ? soNgay : 7);
            if (voucher.HieuLucTu > DateTime.Now)
            {
                voucher.HieuLucTu = DateTime.Now.AddMinutes(-5);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã gia hạn mã '{voucher.MaCode}' đến ngày {voucher.HieuLucDen:dd/MM/yyyy HH:mm} thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa mã giảm giá
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.MaGiamGia.FindAsync(id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            _context.MaGiamGia.Remove(voucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa mã giảm giá '{voucher.MaCode}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Tính năng [Nên có]: Gửi mã giảm giá cho khách hàng qua email kèm tạo thông báo (tận dụng EmailService và bảng ThongBao)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiVoucherChoKhach(int maGiamGiaId, int? nguoiDungId, bool guiTatCa = false)
        {
            var voucher = await _context.MaGiamGia.FindAsync(maGiamGiaId);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            var recipients = new List<NguoiDung>();
            if (guiTatCa)
            {
                recipients = await _context.NguoiDungs
                    .Where(n => n.TrangThai == "Hoạt động")
                    .ToListAsync();
            }
            else
            {
                if (!nguoiDungId.HasValue || nguoiDungId.Value <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn khách hàng nhận mã.";
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

            var mucGiam = voucher.PhanTramGiam.HasValue && voucher.PhanTramGiam > 0
                ? $"{voucher.PhanTramGiam}%"
                : (voucher.SoTienGiam.HasValue ? $"{voucher.SoTienGiam.Value:N0} đ" : "Ưu đãi");

            var tieuDe = $"[QUÀ TẶNG] Tặng bạn mã Voucher {voucher.MaCode} giảm {mucGiam}";
            var noiDung = $"Ban Quản Lý sân UTC STADIUM gửi tặng bạn mã giảm giá {voucher.MaCode} (Giảm {mucGiam} cho đơn thi đấu). Mã có hiệu lực từ {voucher.HieuLucTu:dd/MM/yyyy} đến {voucher.HieuLucDen:dd/MM/yyyy}. Hãy nhập mã khi đặt sân để nhận ưu đãi ngay hôm nay!";

            foreach (var user in recipients)
            {
                await _notificationService.GuiThongBaoAsync(user.Id, tieuDe, noiDung, "KhuyenMai");
            }

            TempData["SuccessMessage"] = $"Đã phát hành mã '{voucher.MaCode}' và gửi email thông báo thành công đến {recipients.Count} khách hàng!";
            return RedirectToAction(nameof(Index));
        }
    }
}


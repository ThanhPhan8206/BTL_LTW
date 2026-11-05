using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.KhachHang.Models;
using QLSanBong.Models.Entities;
using QLSanBong.Services;

namespace QLSanBong.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class DatSanController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly NotificationService _notificationService;

        public DatSanController(QLSanBongDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // ===================================================================
        // 1. [HttpGet] TRANG / MODAL ĐẶT SÂN CHI TIẾT & CHỌN DỊCH VỤ ĐI KÈM
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> TaoDon(int sanBongId, DateOnly? ngayDat, int? khungGioId)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == userId);
            var pitch = await _context.SanBongs
                .Include(s => s.ThietLapGia)
                .ThenInclude(t => t.KhungGio)
                .FirstOrDefaultAsync(s => s.Id == sanBongId);

            // Query danh sách khung giờ từ dbo.KhungGio (kèm cơ chế tự động khởi tạo nếu CSDL chưa có dữ liệu)
            var allTimeSlots = await GetOrSeedTimeSlotsAsync();
            ViewBag.KhungGios = allTimeSlots;

            DateOnly selectedDate = (ngayDat.HasValue && ngayDat.Value >= DateOnly.FromDateTime(DateTime.Today))
                ? ngayDat.Value
                : DateOnly.FromDateTime(DateTime.Today);

            int selectedSlotId = khungGioId ?? (allTimeSlots.FirstOrDefault()?.Id ?? 0);
            var selectedSlot = allTimeSlots.FirstOrDefault(k => k.Id == selectedSlotId);

            // Xác định đơn giá thuê sân
            decimal pitchPrice = 250000m;
            if (pitch != null && pitch.ThietLapGia.Any() && selectedSlotId > 0)
            {
                var priceSetting = pitch.ThietLapGia.FirstOrDefault(t => t.KhungGioId == selectedSlotId);
                pitchPrice = priceSetting?.GiaThue ?? pitch.ThietLapGia.Min(t => t.GiaThue);
            }
            else if (pitch != null)
            {
                pitchPrice = pitch.LoaiSan.Contains("7") ? 450000m : 250000m;
            }

            // Lấy danh sách đầy đủ dịch vụ đi kèm (tự động seed các dịch vụ chuẩn nếu DB đang thiếu)
            var serviceItems = await GetOrSeedServicesAsync();

            var model = new BookingCreateViewModel
            {
                SanBongId = sanBongId > 0 ? sanBongId : (pitch?.Id ?? 1),
                TenSan = pitch?.TenSan ?? "Sân 5A - Mặt Cỏ FIFA",
                LoaiSan = pitch?.LoaiSan ?? "Sân 5 người",
                AnhSan = !string.IsNullOrWhiteSpace(pitch?.AnhSan) ? pitch.AnhSan : "/uploads/sanbong/san-5-so-1.jpg",
                NgayDat = selectedDate,
                KhungGioId = selectedSlotId,
                KhungGioDisplay = selectedSlot != null ? $"{selectedSlot.GioBatDau:HH:mm} - {selectedSlot.GioKetThuc:HH:mm}" : "06:00 - 07:30",
                GiaTienSan = pitchPrice,

                HoTenNguoiDat = user?.HoTen ?? HttpContext.Session.GetString("Customer_HoTen") ?? User.Identity?.Name ?? "",
                SoDienThoaiNguoiDat = user?.SoDienThoai ?? HttpContext.Session.GetString("Customer_SoDienThoai") ?? "",
                EmailNguoiDat = user?.Email ?? HttpContext.Session.GetString("Customer_Email") ?? "",

                DanhSachDichVu = serviceItems,
                DanhSachKhungGio = allTimeSlots,
                HinhThucThanhToan = "DatCoc",
                PhanTramDatCoc = 30,
                TongTienTamTinh = pitchPrice,
                TongTienThanhToan = pitchPrice,
                SoTienDatCoc = Math.Round(pitchPrice * 0.3m / 1000m) * 1000m,
                SoTienPhaiTraNgay = Math.Round(pitchPrice * 0.3m / 1000m) * 1000m
            };

            return View(model);
        }

        // ===================================================================
        // 2. [HttpGet] AJAX KIỂM TRA & ÁP DỤNG VOUCHER (dbo.MaGiamGia)
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraVoucher(string code, decimal tamTinh)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            var normalizedCode = code.Trim().ToUpperInvariant();
            var voucher = await _context.MaGiamGia
                .FirstOrDefaultAsync(v => v.MaCode.ToUpper() == normalizedCode &&
                                          v.HieuLucTu <= DateTime.Now &&
                                          v.HieuLucDen >= DateTime.Now);

            decimal discount = 0;
            string message = "";

            if (voucher != null)
            {
                if (voucher.GiaTriDonToiThieu.HasValue && tamTinh < voucher.GiaTriDonToiThieu.Value)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Mã {voucher.MaCode} chỉ áp dụng cho đơn từ {voucher.GiaTriDonToiThieu.Value:N0} đ."
                    });
                }

                if (voucher.PhanTramGiam.HasValue && voucher.PhanTramGiam.Value > 0)
                {
                    discount = Math.Round(tamTinh * voucher.PhanTramGiam.Value / 100m);
                    message = $"Áp dụng thành công voucher giảm {voucher.PhanTramGiam.Value}% (-{discount:N0} đ).";
                }
                else if (voucher.SoTienGiam.HasValue && voucher.SoTienGiam.Value > 0)
                {
                    discount = Math.Min(tamTinh, voucher.SoTienGiam.Value);
                    message = $"Áp dụng thành công voucher giảm {discount:N0} đ.";
                }
            }
            else
            {
                // Fallback voucher thử nghiệm phổ biến dành cho sinh viên UTC
                if (normalizedCode == "UTC10")
                {
                    discount = Math.Round(tamTinh * 0.1m);
                    message = $"Áp dụng voucher UTC10 giảm 10% (-{discount:N0} đ).";
                }
                else if (normalizedCode == "SINHVIEN20K" && tamTinh >= 200000m)
                {
                    discount = 20000m;
                    message = "Áp dụng voucher SINHVIEN20K giảm 20.000 đ.";
                }
                else if (normalizedCode == "HOITHAO50K" && tamTinh >= 400000m)
                {
                    discount = 50000m;
                    message = "Áp dụng voucher HOITHAO50K giảm 50.000 đ cho giải đấu.";
                }
                else
                {
                    return Json(new { success = false, message = "Mã voucher không tồn tại hoặc đã hết hạn sử dụng." });
                }
            }

            decimal finalTotal = Math.Max(0, tamTinh - discount);
            decimal depositAmount = Math.Round(finalTotal * 0.3m / 1000m) * 1000m;

            return Json(new
            {
                success = true,
                message = message,
                soTienGiam = discount,
                tongTienMoi = finalTotal,
                tienDatCocMoi = depositAmount
            });
        }

        // ===================================================================
        // 3. [HttpPost] XỬ LÝ LƯU ĐƠN ĐẶT SÂN, DỊCH VỤ VÀ TẠO HÓA ĐƠN
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoDon(BookingCreateViewModel model)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Query danh sách khung giờ từ dbo.KhungGio
            var allTimeSlots = await GetOrSeedTimeSlotsAsync();
            ViewBag.KhungGios = allTimeSlots;
            model.DanhSachKhungGio = allTimeSlots;

            if (model.KhungGioId <= 0)
            {
                ModelState.AddModelError(nameof(model.KhungGioId), "Vui lòng chọn khung giờ thi đấu.");
            }

            var selectedSlot = allTimeSlots.FirstOrDefault(k => k.Id == model.KhungGioId);

            // Kiểm tra trùng lịch hoặc bảo trì (Smart overlap check)
            var isBooked = await _context.DatSans.AnyAsync(d =>
                d.SanBongId == model.SanBongId &&
                d.NgayDat == model.NgayDat &&
                d.KhungGioId == model.KhungGioId &&
                d.TrangThai != "Đã hủy" &&
                d.TrangThai != "Từ chối" &&
                d.TrangThai != "Cancelled");

            if (isBooked)
            {
                ModelState.AddModelError(string.Empty, "Rất tiếc! Ca sân này vừa có người giữ hoặc đã được xác nhận. Vui lòng chọn khung giờ khác.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.KhungGios = allTimeSlots;
                model.DanhSachKhungGio = allTimeSlots;

                if (model.DanhSachDichVu == null || !model.DanhSachDichVu.Any())
                {
                    model.DanhSachDichVu = await GetOrSeedServicesAsync();
                }

                return View(model);
            }

            // Tính tiền dịch vụ
            decimal serviceTotal = 0;
            var selectedServices = model.DanhSachDichVu.Where(s => s.IsSelected && s.SoLuong > 0).ToList();
            foreach (var s in selectedServices)
            {
                serviceTotal += s.SoLuong * s.GiaDichVu;
            }

            // Tính giảm giá voucher
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(model.MaVoucher))
            {
                var normalizedCode = model.MaVoucher.Trim().ToUpperInvariant();
                var voucher = await _context.MaGiamGia.FirstOrDefaultAsync(v =>
                    v.MaCode.ToUpper() == normalizedCode &&
                    v.HieuLucTu <= DateTime.Now &&
                    v.HieuLucDen >= DateTime.Now);

                decimal subTotal = model.GiaTienSan + serviceTotal;
                if (voucher != null)
                {
                    if (voucher.PhanTramGiam.HasValue)
                        discountAmount = Math.Round(subTotal * voucher.PhanTramGiam.Value / 100m);
                    else if (voucher.SoTienGiam.HasValue)
                        discountAmount = voucher.SoTienGiam.Value;
                }
                else if (normalizedCode == "UTC10")
                {
                    discountAmount = Math.Round(subTotal * 0.1m);
                }
                else if (normalizedCode == "SINHVIEN20K")
                {
                    discountAmount = 20000m;
                }
            }

            decimal finalTotal = Math.Max(0, model.GiaTienSan + serviceTotal - discountAmount);

            // ===============================================================
            // XỬ LÝ CONCURRENCY / RACE CONDITION: KIỂM TRA TRÙNG LỊCH THỜI ĐIỂM THỰC
            // ===============================================================
            var isConflict = await _context.DatSans.AnyAsync(d =>
                d.SanBongId == model.SanBongId &&
                d.NgayDat == model.NgayDat &&
                d.KhungGioId == model.KhungGioId &&
                d.TrangThai != "Đã hủy" &&
                d.TrangThai != "Từ chối" &&
                d.TrangThai != "Cancelled");

            if (isConflict)
            {
                ModelState.AddModelError(string.Empty, "Rất tiếc! Ca thi đấu này vừa được một đội bóng khác giữ chỗ thành công trước ít giây. Vui lòng chọn ca hoặc sân bóng khác.");
                model.DanhSachKhungGio = await GetOrSeedTimeSlotsAsync();
                model.DanhSachDichVu = await GetOrSeedServicesAsync();
                return View(model);
            }

            var currentSlot = await _context.KhungGios.FindAsync(model.KhungGioId);
            if (currentSlot != null)
            {
                DateTime checkStart = model.NgayDat.ToDateTime(currentSlot.GioBatDau);
                DateTime checkEnd = model.NgayDat.ToDateTime(currentSlot.GioKetThuc);
                bool isUnderMaintenance = await _context.LichBaoTris.AnyAsync(m =>
                    m.SanBongId == model.SanBongId &&
                    m.ThoiGianBatDau < checkEnd &&
                    m.ThoiGianKetThuc > checkStart);

                if (isUnderMaintenance)
                {
                    ModelState.AddModelError(string.Empty, "Sân bóng đang trong lịch bảo dưỡng mặt cỏ/hệ thống chiếu sáng vào khung giờ này. Vui lòng chọn sân khác.");
                    model.DanhSachKhungGio = await GetOrSeedTimeSlotsAsync();
                    model.DanhSachDichVu = await GetOrSeedServicesAsync();
                    return View(model);
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo bản ghi DatSan
                var datSan = new DatSan
                {
                    NguoiDungId = userId,
                    SanBongId = model.SanBongId,
                    KhungGioId = model.KhungGioId,
                    NgayDat = model.NgayDat,
                    TongTien = finalTotal,
                    TrangThai = "Chờ duyệt",
                    NgayTao = DateTime.Now
                };

                _context.DatSans.Add(datSan);
                await _context.SaveChangesAsync();

                // 2. Lưu chi tiết dịch vụ vào dbo.ChiTietDichVuDat (nếu có chọn)
                if (selectedServices.Any())
                {
                    // Đảm bảo các Id dịch vụ tồn tại trong DB, nếu chưa có thì seed nhanh để không dính Foreign Key
                    var existingServiceIds = await _context.DichVus.Select(d => d.Id).ToListAsync();
                    foreach (var s in selectedServices)
                    {
                        int validServiceId = s.DichVuId;
                        if (!existingServiceIds.Contains(validServiceId))
                        {
                            var newDbService = new DichVu
                            {
                                TenDichVu = s.TenDichVu,
                                GiaDichVu = s.GiaDichVu,
                                DonViTinh = s.DonViTinh
                            };
                            _context.DichVus.Add(newDbService);
                            await _context.SaveChangesAsync();
                            validServiceId = newDbService.Id;
                        }

                        _context.ChiTietDichVuDats.Add(new ChiTietDichVuDat
                        {
                            DatSanId = datSan.Id,
                            DichVuId = validServiceId,
                            SoLuong = s.SoLuong,
                            DonGia = s.GiaDichVu
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 3. Tạo Hóa Đơn trong dbo.HoaDon
                var hoaDon = new HoaDon
                {
                    DatSanId = datSan.Id,
                    MaHoaDon = $"HD{DateTime.Now:yyyyMMdd}{datSan.Id:D4}",
                    NgayXuat = DateTime.Now
                };
                _context.HoaDons.Add(hoaDon);

                // 4. Xác định số tiền thanh toán (Đặt cọc 30% hoặc Toàn bộ 100%)
                decimal amountToPay = model.HinhThucThanhToan == "ToanBo"
                    ? finalTotal
                    : Math.Round(finalTotal * 0.3m / 1000m) * 1000m;

                var thanhToan = new ThanhToan
                {
                    DatSanId = datSan.Id,
                    SoTien = amountToPay,
                    PhuongThucThanhToan = "Chuyển khoản VietQR",
                    LoaiThanhToan = model.HinhThucThanhToan == "ToanBo" ? "Thanh toán toàn bộ" : "Đặt cọc",
                    TrangThai = "Chờ chuyển khoản",
                    MaGiaoDich = $"COC{datSan.Id}{DateTime.Now:HHmmss}",
                    ThoiGianThanhToan = DateTime.Now
                };
                _context.ThanhToans.Add(thanhToan);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Bắn thông báo đa kênh (Lưu Web + Gửi Gmail SMTP bất đồng bộ)
                string noteInfo = !string.IsNullOrWhiteSpace(model.GhiChu) ? $" (Ghi chú: {model.GhiChu})" : "";
                _ = _notificationService.GuiThongBaoAsync(userId,
                    $"Đã tiếp nhận đơn đặt sân #{datSan.Id}",
                    $"Đơn đặt {model.TenSan} ngày {model.NgayDat:dd/MM/yyyy} ({model.KhungGioDisplay}). Tổng tiền: {finalTotal:N0} đ{noteInfo}. Vui lòng quét mã VietQR để hoàn tất giữ ca.",
                    "DatSan");

                TempData["SuccessMessage"] = $"Tạo đơn đặt sân #{datSan.Id} thành công! Vui lòng quét mã QR chuyển khoản để giữ ca thi đấu.";
                return RedirectToAction(nameof(ThanhToan), new { id = datSan.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ===================================================================
        // 4. [HttpGet] TRANG THANH TOÁN ĐẶT CỌC & SINH MÃ VIETQR ĐỘNG
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> ThanhToan(int id)
        {
            int userId = GetCurrentUserId();
            var datSan = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.NguoiDung)
                .Include(d => d.HoaDon)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.Id == id && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt sân.";
                return RedirectToAction(nameof(LichSu));
            }

            var latestPayment = datSan.ThanhToans.OrderByDescending(t => t.Id).FirstOrDefault();
            decimal payAmount = latestPayment?.SoTien ?? Math.Round(datSan.TongTien * 0.3m / 1000m) * 1000m;
            decimal remainingAmount = Math.Max(0, datSan.TongTien - payAmount);

            // Cú pháp chuyển khoản chuẩn: COC SAN [MaDatSan] [SoDienThoai]
            string transferSyntax = $"COC SAN {datSan.Id} {datSan.NguoiDung.SoDienThoai}".Trim();
            string encodedSyntax = WebUtility.UrlEncode(transferSyntax);
            long amountLong = (long)payAmount;

            // Sinh link ảnh VietQR động chuẩn format quốc tế
            string vietQrUrl = $"https://img.vietqr.io/image/MB-0988686868-compact2.png?amount={amountLong}&addInfo={encodedSyntax}&accountName=UTC%20STADIUM%20SAN%20BONG";

            var model = new PaymentProcessViewModel
            {
                DatSanId = datSan.Id,
                MaHoaDon = datSan.HoaDon?.MaHoaDon ?? $"HD{datSan.Id:D6}",
                TenSan = datSan.SanBong?.TenSan ?? "Sân bóng UTC",
                LoaiSan = datSan.SanBong?.LoaiSan ?? "Sân 5 người",
                NgayDat = datSan.NgayDat,
                KhungGioDisplay = datSan.KhungGio != null ? $"{datSan.KhungGio.GioBatDau:HH:mm} - {datSan.KhungGio.GioKetThuc:HH:mm}" : "",
                HoTenNguoiDat = datSan.NguoiDung.HoTen,
                SoDienThoai = datSan.NguoiDung.SoDienThoai,
                TongTienDon = datSan.TongTien,
                SoTienThanhToan = payAmount,
                SoTienConLai = remainingAmount,
                LoaiThanhToan = latestPayment?.LoaiThanhToan ?? "Đặt cọc giữ sân",
                TrangThaiThanhToan = latestPayment?.TrangThai ?? "Chờ chuyển khoản",
                TrangThaiDatSan = datSan.TrangThai,
                NoiDungChuyenKhoan = transferSyntax,
                VietQrImageUrl = vietQrUrl,
                NgayTao = datSan.NgayTao
            };

            return View(model);
        }

        // ===================================================================
        // 5. [HttpPost] XÁC NHẬN ĐÃ CHUYỂN KHOẢN VIETQR
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToan(int id, string? maGiaoDich)
        {
            int userId = GetCurrentUserId();
            var datSan = await _context.DatSans
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.Id == id && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân.";
                return RedirectToAction(nameof(LichSu));
            }

            var latestPayment = datSan.ThanhToans.OrderByDescending(t => t.Id).FirstOrDefault();
            if (latestPayment != null)
            {
                latestPayment.TrangThai = "Thành công";
                latestPayment.MaGiaoDich = !string.IsNullOrWhiteSpace(maGiaoDich) ? maGiaoDich.Trim() : latestPayment.MaGiaoDich;
                latestPayment.ThoiGianThanhToan = DateTime.Now;
            }

            datSan.TrangThai = (latestPayment?.LoaiThanhToan == "Thanh toán toàn bộ") ? "Đã duyệt" : "Đã đặt cọc";

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiDungId = userId,
                TieuDe = $"Giao dịch đơn #{datSan.Id} đã được xác nhận",
                NoiDung = $"Hệ thống đã ghi nhận số tiền {latestPayment?.SoTien:N0} đ. Ban Quản Lý Sân UTC STADIUM đã khóa ca giữ chỗ cho bạn.",
                LoaiThongBao = "ThanhToan",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Giao dịch thành công! Ban Quản Lý Sân UTC STADIUM đã xác nhận lịch đá của bạn.";
            return RedirectToAction(nameof(ChiTietHoaDon), new { id = datSan.Id });
        }

        // ===================================================================
        // 6. [HttpGet] QUẢN LÝ LỊCH ĐẶT CÁ NHÂN & TIME-LOCK RULE
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> LichSu(string? filter)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var bookingsQuery = _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.HoaDon)
                .Include(d => d.ThanhToans)
                .Include(d => d.ChiTietDichVuDats)
                    .ThenInclude(c => c.DichVu)
                .Where(d => d.NguoiDungId == userId)
                .OrderByDescending(d => d.NgayDat)
                .ThenByDescending(d => d.Id)
                .AsQueryable();

            var bookings = await bookingsQuery.ToListAsync();
            var reviewsDict = await _context.DanhGia
                .Where(dg => dg.NguoiDungId == userId)
                .ToDictionaryAsync(dg => dg.DatSanId, dg => dg);

            var now = DateTime.Now;
            var historyItems = new List<BookingHistoryItemModel>();

            foreach (var b in bookings)
            {
                decimal paidAmount = b.ThanhToans.Where(t => t.TrangThai == "Thành công" || t.TrangThai == "Đã thanh toán").Sum(t => t.SoTien);
                string paymentStatus = paidAmount >= b.TongTien ? "Đã thanh toán đủ" : (paidAmount > 0 ? "Đã đặt cọc" : "Chờ chuyển khoản");
                bool daDanhGia = reviewsDict.TryGetValue(b.Id, out var rv);

                // ===========================================================
                // TIME-LOCK RULE: RÀNG BUỘC THỜI GIAN TRƯỚC GIỜ BÓNG LĂN 2 TIẾNG
                // ===========================================================
                DateTime kickoffTime = b.NgayDat.ToDateTime(b.KhungGio?.GioBatDau ?? new TimeOnly(17, 30));
                TimeSpan diff = kickoffTime - now;
                double hoursLeft = diff.TotalHours;

                bool isCancelled = b.TrangThai.Contains("Hủy", StringComparison.OrdinalIgnoreCase) || b.TrangThai.Contains("Từ chối", StringComparison.OrdinalIgnoreCase);
                bool canModify = hoursLeft >= 2.0 && !isCancelled;
                string lockStatusText = "";
                string? lockReason = null;

                if (isCancelled)
                {
                    lockStatusText = "Đơn đã bị hủy";
                    lockReason = "Đơn đặt sân này đã ở trạng thái hủy/từ chối.";
                }
                else if (hoursLeft < 0)
                {
                    lockStatusText = "Trận đấu đã kết thúc";
                    lockReason = "Trận đấu đã diễn ra, không thể thay đổi.";
                }
                else if (hoursLeft < 2.0)
                {
                    lockStatusText = $"Đã khóa (< 2h trước trận)";
                    lockReason = "Theo quy định Time-lock, bạn chỉ có thể đổi/hủy lịch trước giờ đá tối thiểu 2 tiếng. Sát giờ vui lòng gọi Hotline 0988.686.868.";
                }
                else
                {
                    lockStatusText = $"Được phép đổi/hủy (còn {Math.Round(hoursLeft, 1)} tiếng)";
                }

                historyItems.Add(new BookingHistoryItemModel
                {
                    DatSanId = b.Id,
                    SanBongId = b.SanBongId,
                    TenSan = b.SanBong?.TenSan ?? "Sân bóng UTC",
                    LoaiSan = b.SanBong?.LoaiSan ?? "Sân 5 người",
                    AnhSan = !string.IsNullOrWhiteSpace(b.SanBong?.AnhSan) ? b.SanBong.AnhSan : "/uploads/sanbong/san-5-so-1.jpg",
                    NgayDat = b.NgayDat,
                    GioBatDau = b.KhungGio?.GioBatDau ?? new TimeOnly(17, 30),
                    GioKetThuc = b.KhungGio?.GioKetThuc ?? new TimeOnly(19, 0),
                    TongTien = b.TongTien,
                    DaThanhToan = paidAmount,
                    TrangThaiDatSan = b.TrangThai,
                    TrangThaiThanhToan = paymentStatus,
                    MaHoaDon = b.HoaDon?.MaHoaDon,
                    NgayTao = b.NgayTao,
                    DanhSachDichVu = b.ChiTietDichVuDats.Select(c => $"{c.DichVu.TenDichVu} (x{c.SoLuong})").ToList(),
                    DaDanhGia = daDanhGia,
                    SoSaoDanhGia = rv?.SoSao,
                    BinhLuanDanhGia = rv?.BinhLuan,
                    CanRescheduleOrCancel = canModify,
                    HoursUntilKickoff = hoursLeft,
                    TimeLockStatusText = lockStatusText,
                    LockReason = lockReason
                });
            }

            var model = new BookingHistoryViewModel
            {
                DanhSachDonDat = historyItems,
                FilterTrangThai = filter
            };

            return View(model);
        }

        // ===================================================================
        // 7. [HttpGet] XEM LẠI BILL THANH TOÁN (HÓA ĐƠN ĐIỆN TỬ CHI TIẾT)
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> ChiTietHoaDon(int id)
        {
            int userId = GetCurrentUserId();
            var datSan = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.NguoiDung)
                .Include(d => d.HoaDon)
                .Include(d => d.ThanhToans)
                .Include(d => d.ChiTietDichVuDats)
                    .ThenInclude(c => c.DichVu)
                .FirstOrDefaultAsync(d => d.Id == id && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn tương ứng.";
                return RedirectToAction(nameof(LichSu));
            }

            decimal servicesCost = datSan.ChiTietDichVuDats.Sum(c => c.SoLuong * c.DonGia);
            decimal pitchCost = Math.Max(0, datSan.TongTien - servicesCost);

            var model = new InvoiceDetailViewModel
            {
                MaHoaDon = datSan.HoaDon?.MaHoaDon ?? $"HD{datSan.Id:D6}",
                NgayXuat = datSan.HoaDon?.NgayXuat ?? datSan.NgayTao,
                DatSanId = datSan.Id,
                TenKhachHang = datSan.NguoiDung.HoTen,
                SoDienThoai = datSan.NguoiDung.SoDienThoai,
                Email = datSan.NguoiDung.Email,
                TenSan = datSan.SanBong?.TenSan ?? "Sân bóng UTC",
                LoaiSan = datSan.SanBong?.LoaiSan ?? "Sân 5 người",
                NgayDat = datSan.NgayDat,
                KhungGioDisplay = datSan.KhungGio != null ? $"{datSan.KhungGio.GioBatDau:HH:mm} - {datSan.KhungGio.GioKetThuc:HH:mm}" : "",
                TienSan = pitchCost,
                TongTienDon = datSan.TongTien,
                TrangThaiDatSan = datSan.TrangThai,
                DanhSachDichVu = datSan.ChiTietDichVuDats.Select(c => new InvoiceServiceItemModel
                {
                    TenDichVu = c.DichVu.TenDichVu,
                    DonViTinh = c.DichVu.DonViTinh,
                    SoLuong = c.SoLuong,
                    DonGia = c.DonGia
                }).ToList(),
                DanhSachThanhToan = datSan.ThanhToans.Select(t => new InvoicePaymentItemModel
                {
                    Id = t.Id,
                    SoTien = t.SoTien,
                    PhuongThuc = t.PhuongThucThanhToan,
                    LoaiThanhToan = t.LoaiThanhToan,
                    TrangThai = t.TrangThai,
                    MaGiaoDich = t.MaGiaoDich,
                    ThoiGian = t.ThoiGianThanhToan
                }).ToList()
            };

            return View(model);
        }

        // ===================================================================
        // 8. [HttpPost] HỦY ĐẶT SÂN KÈM RÀNG BUỘC TIME-LOCK RULE
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDatSan(int id, string? lyDoHuy)
        {
            int userId = GetCurrentUserId();
            var datSan = await _context.DatSans
                .Include(d => d.KhungGio)
                .Include(d => d.SanBong)
                .FirstOrDefaultAsync(d => d.Id == id && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân cần hủy.";
                return RedirectToAction(nameof(LichSu));
            }

            // Kiểm tra ràng buộc Time-lock (tối thiểu 2 tiếng trước giờ đá)
            DateTime kickoffTime = datSan.NgayDat.ToDateTime(datSan.KhungGio?.GioBatDau ?? new TimeOnly(17, 30));
            TimeSpan diff = kickoffTime - DateTime.Now;

            if (diff.TotalHours < 2.0)
            {
                TempData["ErrorMessage"] = "Quy định Time-lock: Không thể hủy sân khi thời gian đá còn dưới 2 tiếng! Vui lòng liên hệ Hotline BQL Sân 0988.686.868.";
                return RedirectToAction(nameof(LichSu));
            }

            datSan.TrangThai = "Đã hủy";
            await _context.SaveChangesAsync();

            // Bắn thông báo đa kênh (Web + Gmail SMTP)
            await _notificationService.ThongBaoTrangThaiDatSanAsync(datSan.Id, "Đã hủy", lyDoHuy ?? "Có việc đột xuất");

            TempData["SuccessMessage"] = $"Đã hủy đơn đặt sân #{id} thành công!";
            return RedirectToAction(nameof(LichSu));
        }

        // ===================================================================
        // HELPER METHODS
        // ===================================================================
        private int GetCurrentUserId()
        {
            var sessionUid = HttpContext.Session.GetString("Customer_UserId");
            if (int.TryParse(sessionUid, out int uidFromSession) && uidFromSession > 0)
                return uidFromSession;

            var claimUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimUid, out int uidFromClaim))
                return uidFromClaim;

            return 0;
        }

        private async Task<List<KhungGio>> GetOrSeedTimeSlotsAsync()
        {
            var slots = await _context.KhungGios.OrderBy(k => k.GioBatDau).ToListAsync();
            if (!slots.Any())
            {
                var defaultSlots = new List<KhungGio>
                {
                    new() { GioBatDau = new TimeOnly(6, 0), GioKetThuc = new TimeOnly(7, 30) },
                    new() { GioBatDau = new TimeOnly(7, 30), GioKetThuc = new TimeOnly(9, 0) },
                    new() { GioBatDau = new TimeOnly(9, 0), GioKetThuc = new TimeOnly(10, 30) },
                    new() { GioBatDau = new TimeOnly(14, 30), GioKetThuc = new TimeOnly(16, 0) },
                    new() { GioBatDau = new TimeOnly(16, 0), GioKetThuc = new TimeOnly(17, 30) },
                    new() { GioBatDau = new TimeOnly(17, 30), GioKetThuc = new TimeOnly(19, 0) },
                    new() { GioBatDau = new TimeOnly(19, 0), GioKetThuc = new TimeOnly(20, 30) },
                    new() { GioBatDau = new TimeOnly(20, 30), GioKetThuc = new TimeOnly(22, 0) }
                };

                try
                {
                    _context.KhungGios.AddRange(defaultSlots);
                    await _context.SaveChangesAsync();
                    return defaultSlots;
                }
                catch
                {
                    int simId = 1;
                    foreach (var s in defaultSlots)
                    {
                        s.Id = simId++;
                    }
                    return defaultSlots;
                }
            }
            return slots;
        }

        private static string NormalizeServiceName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalizedString = text.Trim().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC)
                .Replace("đ", "d", StringComparison.OrdinalIgnoreCase)
                .Replace("Đ", "D", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .ToLowerInvariant();
        }

        private async Task<List<ServiceSelectionItemModel>> GetOrSeedServicesAsync()
        {
            try
            {
                var standardServices = new List<DichVu>
                {
                    new() { TenDichVu = "Nước khoáng Lavie 500ml", GiaDichVu = 10000, DonViTinh = "Chai" },
                    new() { TenDichVu = "Nước thể thao bù khoáng Revive", GiaDichVu = 15000, DonViTinh = "Chai" },
                    new() { TenDichVu = "Thuê bộ áo pitch tập (10 áo)", GiaDichVu = 50000, DonViTinh = "Bộ" },
                    new() { TenDichVu = "Thuê bóng thi đấu Động Lực FIFA", GiaDichVu = 30000, DonViTinh = "Quả" },
                    new() { TenDichVu = "Bình nước khoáng 20L kèm cốc", GiaDichVu = 40000, DonViTinh = "Bình" },
                    new() { TenDichVu = "Thuê trọng tài bắt trận giao hữu", GiaDichVu = 150000, DonViTinh = "Trận" }
                };

                var dbServices = await _context.DichVus.ToListAsync();
                bool hasNew = false;

                foreach (var std in standardServices)
                {
                    var stdKey = NormalizeServiceName(std.TenDichVu);
                    if (!dbServices.Any(d => NormalizeServiceName(d.TenDichVu) == stdKey))
                    {
                        _context.DichVus.Add(std);
                        hasNew = true;
                    }
                }

                if (hasNew)
                {
                    await _context.SaveChangesAsync();
                    dbServices = await _context.DichVus.ToListAsync();
                }

                if (dbServices.Any())
                {
                    return dbServices.Select(d => new ServiceSelectionItemModel
                    {
                        DichVuId = d.Id,
                        TenDichVu = d.TenDichVu,
                        GiaDichVu = d.GiaDichVu,
                        DonViTinh = d.DonViTinh,
                        SoLuong = 0,
                        IsSelected = false
                    }).ToList();
                }
            }
            catch
            {
                // Fallback nếu kết nối CSDL có trục trặc
            }

            return GetFallbackServices();
        }

        private static List<ServiceSelectionItemModel> GetFallbackServices()
        {
            return new List<ServiceSelectionItemModel>
            {
                new() { DichVuId = 1, TenDichVu = "Nước khoáng Lavie 500ml", GiaDichVu = 10000, DonViTinh = "Chai", SoLuong = 0 },
                new() { DichVuId = 2, TenDichVu = "Nước thể thao bù khoáng Revive", GiaDichVu = 15000, DonViTinh = "Chai", SoLuong = 0 },
                new() { DichVuId = 3, TenDichVu = "Thuê bộ áo pitch tập (10 áo)", GiaDichVu = 50000, DonViTinh = "Bộ", SoLuong = 0 },
                new() { DichVuId = 4, TenDichVu = "Thuê bóng thi đấu Động Lực FIFA", GiaDichVu = 30000, DonViTinh = "Quả", SoLuong = 0 },
                new() { DichVuId = 5, TenDichVu = "Bình nước khoáng 20L kèm cốc", GiaDichVu = 40000, DonViTinh = "Bình", SoLuong = 0 },
                new() { DichVuId = 6, TenDichVu = "Thuê trọng tài bắt trận giao hữu", GiaDichVu = 150000, DonViTinh = "Trận", SoLuong = 0 }
            };
        }
    }
}


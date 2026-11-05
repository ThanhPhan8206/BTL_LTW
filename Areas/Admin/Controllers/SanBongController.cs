using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.Admin.Models;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SanBongController : Controller
    {
        private readonly QLSanBongDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SanBongController(QLSanBongDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Danh sách sân bóng với tìm kiếm theo tên, lọc theo loại sân (5, 7, 11) và trạng thái, có phân trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? loaiSan = null, string? trangThai = null, int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 10;

            var now = DateTime.Now;
            var query = _context.SanBongs
                .Include(s => s.DatSans)
                .Include(s => s.LichBaoTris)
                .AsQueryable();

            // Đếm thống kê tổng quan
            var totalSan5 = await _context.SanBongs.CountAsync(s => s.LoaiSan.Contains("5"));
            var totalSan7 = await _context.SanBongs.CountAsync(s => s.LoaiSan.Contains("7"));
            var totalSan11 = await _context.SanBongs.CountAsync(s => s.LoaiSan.Contains("11"));
            var totalDangBaoTri = await _context.LichBaoTris
                .CountAsync(b => b.ThoiGianBatDau <= now && b.ThoiGianKetThuc >= now);

            // Tìm kiếm theo tên hoặc mô tả
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.TenSan.ToLower().Contains(s) || (x.MoTa != null && x.MoTa.ToLower().Contains(s)));
            }

            // Lọc theo loại sân (5, 7, 11)
            if (!string.IsNullOrWhiteSpace(loaiSan) && loaiSan != "all")
            {
                query = query.Where(x => x.LoaiSan.Contains(loaiSan));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "all")
            {
                if (trangThai == "Bảo trì")
                {
                    var pitchIdsInMaintenance = await _context.LichBaoTris
                        .Where(b => b.ThoiGianBatDau <= now && b.ThoiGianKetThuc >= now)
                        .Select(b => b.SanBongId)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(x => pitchIdsInMaintenance.Contains(x.Id) || x.TrangThai == "Bảo trì");
                }
                else if (trangThai == "Sẵn sàng")
                {
                    query = query.Where(x => x.TrangThai == "Sẵn sàng" || x.TrangThai == "Hoạt động");
                }
                else if (trangThai == "Ngừng hoạt động")
                {
                    query = query.Where(x => x.TrangThai == "Ngừng hoạt động");
                }
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var rawPitches = await query
                .OrderBy(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rawPitches.Select(s =>
            {
                var currentMaintenance = s.LichBaoTris
                    .FirstOrDefault(b => b.ThoiGianBatDau <= now && b.ThoiGianKetThuc >= now);

                var isUnderMaintenance = currentMaintenance != null || s.TrangThai == "Bảo trì";
                var infoBaoTri = currentMaintenance != null
                    ? $"{currentMaintenance.ThoiGianBatDau:HH:mm dd/MM} - {currentMaintenance.ThoiGianKetThuc:HH:mm dd/MM} ({currentMaintenance.GhiChu ?? "Bảo trì"})"
                    : null;

                return new AdminSanBongItemViewModel
                {
                    Id = s.Id,
                    TenSan = s.TenSan,
                    LoaiSan = s.LoaiSan,
                    MoTa = s.MoTa,
                    TrangThai = isUnderMaintenance ? "Bảo trì" : s.TrangThai,
                    NgayTao = s.NgayTao,
                    AnhSan = s.AnhSan,
                    TongDonDat = s.DatSans.Count,
                    DangBaoTri = isUnderMaintenance,
                    ThongTinBaoTriHienTai = infoBaoTri,
                    CoTheXoa = !s.DatSans.Any()
                };
            }).ToList();

            var viewModel = new AdminSanBongListViewModel
            {
                Items = items,
                Search = search,
                LoaiSan = loaiSan,
                TrangThai = trangThai,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                TotalSan5 = totalSan5,
                TotalSan7 = totalSan7,
                TotalSan11 = totalSan11,
                TotalDangBaoTri = totalDangBaoTri
            };

            return View(viewModel);
        }

        /// <summary>
        /// Thêm mới sân bóng (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AdminSanBongInputViewModel());
        }

        /// <summary>
        /// Thêm mới sân bóng (POST) kèm upload ảnh vào wwwroot/uploads/sanbong/
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminSanBongInputViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng tên sân
            var isDuplicateName = await _context.SanBongs.AnyAsync(s => s.TenSan.ToLower() == model.TenSan.Trim().ToLower());
            if (isDuplicateName)
            {
                ModelState.AddModelError("TenSan", "Tên sân bóng này đã tồn tại trên hệ thống.");
                return View(model);
            }

            var sanBong = new SanBong
            {
                TenSan = model.TenSan.Trim(),
                LoaiSan = model.LoaiSan,
                MoTa = model.MoTa,
                TrangThai = string.IsNullOrWhiteSpace(model.TrangThai) ? "Sẵn sàng" : model.TrangThai,
                NgayTao = DateTime.Now
            };

            // Xử lý upload ảnh nếu có
            if (model.AnhSanFile != null && model.AnhSanFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "sanbong");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"san_{Guid.NewGuid():N}{Path.GetExtension(model.AnhSanFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AnhSanFile.CopyToAsync(fileStream);
                }

                sanBong.AnhSan = $"/uploads/sanbong/{uniqueFileName}";
            }
            else
            {
                // Ảnh mặc định theo loại sân
                sanBong.AnhSan = model.LoaiSan.Contains("7")
                    ? "/uploads/sanbong/san-7-so-1.jpg"
                    : (model.LoaiSan.Contains("11") ? "/uploads/sanbong/san-11-so-1.jpg" : "/uploads/sanbong/san-5-so-1.jpg");
            }

            _context.SanBongs.Add(sanBong);
            await _context.SaveChangesAsync();

            // Khởi tạo ma trận giá mặc định cho các khung giờ hiện có
            var existingSlots = await _context.KhungGios.ToListAsync();
            var dayTypes = new[] { "Ngày thường", "Cuối tuần", "Ngày lễ" };
            decimal basePrice = model.LoaiSan.Contains("7") ? 400000m : (model.LoaiSan.Contains("11") ? 750000m : 250000m);

            foreach (var slot in existingSlots)
            {
                foreach (var dayType in dayTypes)
                {
                    decimal price = basePrice;
                    if (dayType == "Cuối tuần") price += 50000m;
                    else if (dayType == "Ngày lễ") price += 100000m;

                    _context.ThietLapGia.Add(new ThietLapGium
                    {
                        SanBongId = sanBong.Id,
                        KhungGioId = slot.Id,
                        NgayTrongTuan = dayType,
                        GiaThue = price
                    });
                }
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm thành công sân bóng '{sanBong.TenSan}' ({sanBong.LoaiSan}) và tự động cấu hình bảng giá khởi tạo!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Cập nhật thông tin sân bóng (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var pitch = await _context.SanBongs.FindAsync(id);
            if (pitch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng.";
                return RedirectToAction(nameof(Index));
            }

            var model = new AdminSanBongInputViewModel
            {
                Id = pitch.Id,
                TenSan = pitch.TenSan,
                LoaiSan = pitch.LoaiSan,
                MoTa = pitch.MoTa,
                TrangThai = pitch.TrangThai,
                AnhSanHienTai = pitch.AnhSan
            };

            return View(model);
        }

        /// <summary>
        /// Cập nhật thông tin sân bóng (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminSanBongInputViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var pitch = await _context.SanBongs.FindAsync(id);
            if (pitch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra trùng tên với sân khác
            var isDuplicate = await _context.SanBongs
                .AnyAsync(s => s.Id != id && s.TenSan.ToLower() == model.TenSan.Trim().ToLower());
            if (isDuplicate)
            {
                ModelState.AddModelError("TenSan", "Tên sân bóng này đã bị trùng với một sân khác.");
                return View(model);
            }

            pitch.TenSan = model.TenSan.Trim();
            pitch.LoaiSan = model.LoaiSan;
            pitch.MoTa = model.MoTa;
            pitch.TrangThai = model.TrangThai;

            // Xử lý upload ảnh mới nếu có
            if (model.AnhSanFile != null && model.AnhSanFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "sanbong");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"san_{Guid.NewGuid():N}{Path.GetExtension(model.AnhSanFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AnhSanFile.CopyToAsync(fileStream);
                }

                pitch.AnhSan = $"/uploads/sanbong/{uniqueFileName}";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật thông tin sân bóng '{pitch.TenSan}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Chuyển trạng thái Sân bóng sang 'Ngừng hoạt động' (Xóa mềm bảo toàn dữ liệu lịch sử)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NgungHoatDong(int id)
        {
            var pitch = await _context.SanBongs.FindAsync(id);
            if (pitch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng.";
                return RedirectToAction(nameof(Index));
            }

            pitch.TrangThai = "Ngừng hoạt động";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chuyển sân '{pitch.TenSan}' sang trạng thái 'Ngừng hoạt động'. Sân sẽ tạm ẩn khỏi giao diện khách.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kích hoạt lại sân bóng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KichHoat(int id)
        {
            var pitch = await _context.SanBongs.FindAsync(id);
            if (pitch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng.";
                return RedirectToAction(nameof(Index));
            }

            pitch.TrangThai = "Sẵn sàng";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã kích hoạt sân '{pitch.TenSan}' thành công. Sân đã sẵn sàng đón khách đặt ca!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Quy tắc Xóa / Ngừng hoạt động: Nếu đã có DatSan thì chặn xóa cứng và chuyển sang xóa mềm (Ngừng hoạt động).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var pitch = await _context.SanBongs
                .Include(s => s.DatSans)
                .Include(s => s.LichBaoTris)
                .Include(s => s.ThietLapGia)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (pitch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng.";
                return RedirectToAction(nameof(Index));
            }

            // Quy tắc xóa mềm: Đã có đơn đặt sân
            if (pitch.DatSans.Any())
            {
                pitch.TrangThai = "Ngừng hoạt động";
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = $"Sân '{pitch.TenSan}' đã phát sinh {pitch.DatSans.Count} đơn đặt sân từ trước. Hệ thống đã tự động chuyển sang trạng thái 'Ngừng hoạt động' (Xóa mềm) để bảo toàn lịch sử dữ liệu!";
                return RedirectToAction(nameof(Index));
            }

            // Chưa có đơn đặt: Cho phép xóa cứng sau khi xóa ma trận giá & lịch bảo trì
            _context.ThietLapGia.RemoveRange(pitch.ThietLapGia);
            _context.LichBaoTris.RemoveRange(pitch.LichBaoTris);
            _context.SanBongs.Remove(pitch);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa hoàn toàn sân '{pitch.TenSan}' khỏi hệ thống thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Trang Quản lý Lịch bảo trì mặt sân (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BaoTri(int? sanBongId = null)
        {
            var query = _context.LichBaoTris
                .Include(b => b.SanBong)
                .AsQueryable();

            if (sanBongId.HasValue && sanBongId.Value > 0)
            {
                query = query.Where(b => b.SanBongId == sanBongId.Value);
            }

            var rawList = await query
                .OrderByDescending(b => b.ThoiGianBatDau)
                .ToListAsync();

            var items = rawList.Select(b => new AdminLichBaoTriItemViewModel
            {
                Id = b.Id,
                SanBongId = b.SanBongId,
                TenSan = b.SanBong?.TenSan ?? "Sân bóng",
                LoaiSan = b.SanBong?.LoaiSan ?? "Sân 5 người",
                ThoiGianBatDau = b.ThoiGianBatDau,
                ThoiGianKetThuc = b.ThoiGianKetThuc,
                GhiChu = b.GhiChu
            }).ToList();

            var pitches = await _context.SanBongs
                .OrderBy(s => s.Id)
                .Select(s => new AdminSanBongItemViewModel
                {
                    Id = s.Id,
                    TenSan = s.TenSan,
                    LoaiSan = s.LoaiSan,
                    TrangThai = s.TrangThai
                })
                .ToListAsync();

            var viewModel = new AdminLichBaoTriViewModel
            {
                DanhSachBaoTri = items,
                SelectedSanBongId = sanBongId,
                DanhSachSan = pitches,
                SanBongId = sanBongId ?? (pitches.FirstOrDefault()?.Id ?? 0),
                ThoiGianBatDau = DateTime.Now.AddHours(1),
                ThoiGianKetThuc = DateTime.Now.AddHours(5)
            };

            return View(viewModel);
        }

        /// <summary>
        /// Thêm lịch bảo trì mới (POST) kèm kiểm tra xung đột đơn đặt sân.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoLichBaoTri(int sanBongId, DateTime tuNgay, DateTime denNgay, string? ghiChu)
        {
            if (denNgay <= tuNgay)
            {
                TempData["ErrorMessage"] = "Thời gian kết thúc bảo trì phải sau thời gian bắt đầu.";
                return RedirectToAction(nameof(BaoTri), new { sanBongId });
            }

            var pitch = await _context.SanBongs.FindAsync(sanBongId);
            if (pitch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng được chọn.";
                return RedirectToAction(nameof(BaoTri));
            }

            // KIỂM TRA XUNG ĐỘT: Tìm các đơn đặt sân đang 'Chờ duyệt', 'Đã duyệt', hoặc 'Đã xác nhận' trùng khoảng thời gian bảo trì
            var activeBookings = await _context.DatSans
                .Include(d => d.NguoiDung)
                .Include(d => d.KhungGio)
                .Include(d => d.SanBong)
                .Where(d => d.SanBongId == sanBongId &&
                            (d.TrangThai == "Chờ duyệt" || d.TrangThai == "Đã duyệt" || d.TrangThai == "Đã xác nhận"))
                .ToListAsync();

            var conflictingBookings = activeBookings.Where(d =>
            {
                if (d.KhungGio == null) return false;
                var slotStart = d.NgayDat.ToDateTime(d.KhungGio.GioBatDau);
                var slotEnd = d.NgayDat.ToDateTime(d.KhungGio.GioKetThuc);
                return slotStart < denNgay && slotEnd > tuNgay;
            }).Select(d => new AdminConflictBookingItemViewModel
            {
                Id = d.Id,
                TenKhachHang = d.NguoiDung?.HoTen ?? "Khách hàng",
                SoDienThoai = d.NguoiDung?.SoDienThoai ?? "",
                TenSan = d.SanBong?.TenSan ?? "Sân bóng",
                NgayDat = d.NgayDat,
                KhungGioDisplay = $"{d.KhungGio?.GioBatDau:HH:mm} - {d.KhungGio?.GioKetThuc:HH:mm}",
                TrangThai = d.TrangThai,
                TongTien = d.TongTien
            }).ToList();

            // Nếu phát hiện có đơn kẹt lịch -> CHẶN LẬP TỨC và trả về danh sách xung đột để Admin xử lý
            if (conflictingBookings.Any())
            {
                var pitches = await _context.SanBongs
                    .OrderBy(s => s.Id)
                    .Select(s => new AdminSanBongItemViewModel
                    {
                        Id = s.Id,
                        TenSan = s.TenSan,
                        LoaiSan = s.LoaiSan,
                        TrangThai = s.TrangThai
                    }).ToListAsync();

                var rawAllMaintenance = await _context.LichBaoTris
                    .Include(b => b.SanBong)
                    .OrderByDescending(b => b.ThoiGianBatDau)
                    .ToListAsync();

                var allMaintenance = rawAllMaintenance.Select(b => new AdminLichBaoTriItemViewModel
                {
                    Id = b.Id,
                    SanBongId = b.SanBongId,
                    TenSan = b.SanBong?.TenSan ?? "Sân bóng",
                    LoaiSan = b.SanBong?.LoaiSan ?? "Sân 5 người",
                    ThoiGianBatDau = b.ThoiGianBatDau,
                    ThoiGianKetThuc = b.ThoiGianKetThuc,
                    GhiChu = b.GhiChu
                }).ToList();

                var errorModel = new AdminLichBaoTriViewModel
                {
                    DanhSachBaoTri = allMaintenance,
                    SelectedSanBongId = sanBongId,
                    DanhSachSan = pitches,
                    SanBongId = sanBongId,
                    ThoiGianBatDau = tuNgay,
                    ThoiGianKetThuc = denNgay,
                    GhiChu = ghiChu,
                    ConflictingBookings = conflictingBookings
                };

                TempData["ErrorMessage"] = $"CHẶN TẠO LỊCH BẢO TRÌ! Phát hiện {conflictingBookings.Count} đơn đặt sân đang trùng vào khung giờ này. Vui lòng xử lý (hủy/chuyển sân) các đơn này trước!";
                return View(nameof(BaoTri), errorModel);
            }

            // Không xung đột -> Lưu lịch bảo trì
            var baoTri = new LichBaoTri
            {
                SanBongId = sanBongId,
                ThoiGianBatDau = tuNgay,
                ThoiGianKetThuc = denNgay,
                GhiChu = ghiChu
            };

            // Nếu đang trong thời điểm diễn ra bảo trì -> Cập nhật trạng thái sân thành "Bảo trì"
            var now = DateTime.Now;
            if (tuNgay <= now && denNgay >= now)
            {
                pitch.TrangThai = "Bảo trì";
            }

            _context.LichBaoTris.Add(baoTri);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thiết lập lịch bảo trì cho sân '{pitch.TenSan}' từ {tuNgay:HH:mm dd/MM/yyyy} đến {denNgay:HH:mm dd/MM/yyyy} thành công!";
            return RedirectToAction(nameof(BaoTri), new { sanBongId });
        }

        /// <summary>
        /// Xóa lịch bảo trì
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaLichBaoTri(int id)
        {
            var baoTri = await _context.LichBaoTris
                .Include(b => b.SanBong)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (baoTri == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch bảo trì.";
                return RedirectToAction(nameof(BaoTri));
            }

            var sanBongId = baoTri.SanBongId;

            // Nếu sân đang ở trạng thái Bảo trì, trả về trạng thái "Sẵn sàng"
            if (baoTri.SanBong != null && baoTri.SanBong.TrangThai == "Bảo trì")
            {
                baoTri.SanBong.TrangThai = "Sẵn sàng";
            }

            _context.LichBaoTris.Remove(baoTri);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa lịch bảo trì thành công.";
            return RedirectToAction(nameof(BaoTri), new { sanBongId });
        }
    }
}

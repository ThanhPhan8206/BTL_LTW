using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.KhachHang.Models;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class AccountController : Controller
    {
        private const string AuthScheme = "AppCookieAuth";
        private readonly QLSanBongDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(QLSanBongDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===================================================================
        // 1. [HttpGet] HIỂN THỊ HỒ SƠ TÀI KHOẢN THEO SESSION ĐĂNG NHẬP
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Lấy ID người dùng từ Session hoặc từ Claims
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var user = await _context.NguoiDungs
                .Include(u => u.QuyenHan)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản người dùng.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Đồng bộ tự động thông tin vào Session để phục vụ các form tiếp theo
            SyncUserToSession(user);

            var model = new UserProfileViewModel
            {
                Id = user.Id,
                HoTen = user.HoTen,
                Email = user.Email,
                SoDienThoai = user.SoDienThoai,
                AnhDaiDienUrl = user.AnhDaiDienUrl,
                TenQuyen = user.QuyenHan?.TenQuyen ?? "Khách hàng sinh viên",
                TrangThai = user.TrangThai,
                NgayTao = user.NgayTao,
                NgayCapNhat = user.NgayCapNhat
            };

            return View(model);
        }

        // ===================================================================
        // 2. [HttpPost] CẬP NHẬT THÔNG TIN TÀI KHOẢN VÀ ĐỒNG BỘ AUTO-FILL
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId <= 0 || model.Id != currentUserId)
            {
                TempData["ErrorMessage"] = "Phiên làm việc không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var normalizedPhone = model.SoDienThoai.Trim();
            var normalizedName = model.HoTen.Trim();

            // Kiểm tra trùng Email với tài khoản khác
            var emailExists = await _context.NguoiDungs
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != model.Id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Địa chỉ Email này đã được sử dụng bởi một tài khoản khác.");
            }

            // Kiểm tra trùng Số điện thoại với tài khoản khác
            var phoneExists = await _context.NguoiDungs
                .AnyAsync(u => u.SoDienThoai == normalizedPhone && u.Id != model.Id);
            if (phoneExists)
            {
                ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được đăng ký bởi người dùng khác.");
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var user = await _context.NguoiDungs
                .Include(u => u.QuyenHan)
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu người dùng để cập nhật.";
                return RedirectToAction(nameof(Profile));
            }

            // Xử lý tải ảnh đại diện Avatar mới (lưu vào wwwroot/uploads/avatars/)
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError(nameof(model.AvatarFile), "Vui lòng chọn file ảnh hợp lệ (.jpg, .jpeg, .png, .webp).");
                    return View("Profile", model);
                }

                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.AvatarFile), "Dung lượng ảnh đại diện không được vượt quá 5MB.");
                    return View("Profile", model);
                }

                var avatarsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(avatarsFolder))
                {
                    Directory.CreateDirectory(avatarsFolder);
                }

                var fileName = $"avatar_{user.Id}_{DateTime.Now.Ticks}{ext}";
                var filePath = Path.Combine(avatarsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                user.AnhDaiDienUrl = $"/uploads/avatars/{fileName}";
            }

            user.HoTen = normalizedName;
            user.Email = normalizedEmail;
            user.SoDienThoai = normalizedPhone;
            user.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

            // Đồng bộ tức thời vào Session để phục vụ Auto-fill cho các form đặt sân
            SyncUserToSession(user);

            // Làm mới Cookie Authentication để Header hiển thị ngay họ tên & avatar mới
            await RefreshUserClaimsAsync(user);

            TempData["SuccessMessage"] = "Cập nhật hồ sơ tài khoản thành công! Dữ liệu của bạn đã được đồng bộ tự động để sẵn sàng đặt sân.";
            return RedirectToAction(nameof(Profile));
        }

        // ===================================================================
        // HELPER METHODS
        // ===================================================================
        private int GetCurrentUserId()
        {
            var sessionUid = HttpContext.Session.GetString("Customer_UserId");
            if (int.TryParse(sessionUid, out int uidFromSession) && uidFromSession > 0)
            {
                return uidFromSession;
            }

            var claimUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimUid, out int uidFromClaim))
            {
                return uidFromClaim;
            }

            return 0;
        }

        private void SyncUserToSession(NguoiDung user)
        {
            HttpContext.Session.SetString("Customer_UserId", user.Id.ToString());
            HttpContext.Session.SetString("Customer_HoTen", user.HoTen);
            HttpContext.Session.SetString("Customer_Email", user.Email);
            HttpContext.Session.SetString("Customer_SoDienThoai", user.SoDienThoai);
            if (!string.IsNullOrEmpty(user.AnhDaiDienUrl))
            {
                HttpContext.Session.SetString("Customer_AvatarUrl", user.AnhDaiDienUrl);
            }
        }

        private async Task RefreshUserClaimsAsync(NguoiDung user)
        {
            var roleName = user.QuyenHan?.TenQuyen?.Trim() ?? "Khách hàng";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.SoDienThoai ?? string.Empty),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(ClaimTypes.Role, "KhachHang")
            };

            if (!string.IsNullOrEmpty(user.AnhDaiDienUrl))
            {
                claims.Add(new Claim("AnhDaiDienUrl", user.AnhDaiDienUrl));
            }

            var claimsIdentity = new ClaimsIdentity(claims, AuthScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(AuthScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }
    }
}


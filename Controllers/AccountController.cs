using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QLSanBong.Models.Entities;
using QLSanBong.Models.ViewModels;
using QLSanBong.Services;

namespace QLSanBong.Controllers
{
    public class AccountController : Controller
    {
        private const string AuthScheme = "AppCookieAuth";
        private const int OtpExpireMinutes = 5;
        private readonly QLSanBongDbContext _context;
        private readonly PasswordHasher<NguoiDung> _passwordHasher;
        private readonly IMemoryCache _cache;
        private readonly EmailService _emailService;

        public AccountController(
            QLSanBongDbContext context,
            IMemoryCache cache,
            EmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _passwordHasher = new PasswordHasher<NguoiDung>();
        }

        // ===================================================================
        // 1. ĐĂNG KÝ TÀI KHOẢN (REGISTER)
        // ===================================================================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByUserRole();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var normalizedPhone = model.SoDienThoai.Trim();
            var normalizedName = model.HoTen.Trim();

            // Kiểm tra trùng Email hoặc Số điện thoại trong bảng NguoiDung (Tận dụng Unique Index trên cột Email)
            var emailExists = await _context.NguoiDungs
                .AnyAsync(u => u.Email == normalizedEmail);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Địa chỉ Email này đã được sử dụng cho một tài khoản khác.");
            }

            var phoneExists = await _context.NguoiDungs
                .AnyAsync(u => u.SoDienThoai == normalizedPhone);

            if (phoneExists)
            {
                ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được đăng ký trên hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm QuyenHanId mặc định của Khách hàng trong bảng QuyenHan
                var customerRole = await GetOrCreateDefaultCustomerRoleAsync();

                var newUser = new NguoiDung
                {
                    HoTen = normalizedName,
                    Email = normalizedEmail,
                    SoDienThoai = normalizedPhone,
                    QuyenHanId = customerRole.Id,
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };

                // Băm mật khẩu an toàn bằng PasswordHasher<NguoiDung>
                newUser.MatKhauMaHoa = _passwordHasher.HashPassword(newUser, model.MatKhau);

                _context.NguoiDungs.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập để đặt sân tại UTC STADIUM.";
                TempData["RegisteredEmail"] = newUser.Email;

                return RedirectToAction(nameof(Login));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể hoàn tất đăng ký do trùng dữ liệu hoặc lỗi kết nối CSDL. Vui lòng kiểm tra lại.");
                return View(model);
            }
        }

        // ===================================================================
        // 2. ĐĂNG NHẬP HỆ THỐNG (LOGIN)
        // ===================================================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByUserRole(returnUrl);
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Email = TempData["RegisteredEmail"] as string ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            model.ReturnUrl = returnUrl ?? model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var accountInput = model.Email.Trim();
            var accountInputLower = accountInput.ToLowerInvariant();

            // Truy vấn NguoiDung kết hợp Include(n => n.QuyenHan) theo Email (hoặc Số điện thoại)
            var user = await _context.NguoiDungs
                .Include(n => n.QuyenHan)
                .FirstOrDefaultAsync(n =>
                    n.Email.ToLower() == accountInputLower ||
                    n.SoDienThoai == accountInput);

            if (user == null || !VerifyAndUpgradePasswordIfNeeded(user, model.MatKhau, out bool shouldSaveRehash))
            {
                ModelState.AddModelError(string.Empty, "Email/Số điện thoại hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra trạng thái hoạt động của tài khoản
            if (!string.IsNullOrWhiteSpace(user.TrangThai) &&
                !user.TrangThai.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase) &&
                !user.TrangThai.Equals("Hoat dong", StringComparison.OrdinalIgnoreCase) &&
                !user.TrangThai.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                if (user.TrangThai.Equals("Bị khóa", StringComparison.OrdinalIgnoreCase) ||
                    user.TrangThai.Equals("Bi khoa", StringComparison.OrdinalIgnoreCase) ||
                    user.TrangThai.Equals("Locked", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa bởi Ban Quản trị. Vui lòng liên hệ Hotline 0988.686.868 để được hỗ trợ mở khóa.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Tài khoản của bạn hiện đang ở trạng thái '{user.TrangThai}'. Vui lòng liên hệ Ban Quản Lý Sân.");
                }
                return View(model);
            }

            if (shouldSaveRehash)
            {
                user.NgayCapNhat = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            var roleName = user.QuyenHan?.TenQuyen?.Trim() ?? "Khách hàng";
            var isAdmin = IsAdminRole(roleName);

            // Thiết lập danh sách Claims cho Cookie Authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.SoDienThoai ?? string.Empty),
                new Claim(ClaimTypes.Role, roleName)
            };

            if (!string.IsNullOrEmpty(user.AnhDaiDienUrl))
            {
                claims.Add(new Claim("AnhDaiDienUrl", user.AnhDaiDienUrl));
            }

            // Bổ sung Role chuẩn hóa ("Admin" / "KhachHang") để tương thích mọi thuộc tính [Authorize(Roles = ...)]
            var normalizedRoleClaim = isAdmin ? "Admin" : "KhachHang";
            if (!roleName.Equals(normalizedRoleClaim, StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, normalizedRoleClaim));
            }

            var claimsIdentity = new ClaimsIdentity(claims, AuthScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(AuthScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.HoTen} đến với UTC STADIUM.";

            // Phân quyền điều hướng sau khi đăng nhập thành công
            if (isAdmin)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "KhachHang" });
        }

        // ===================================================================
        // 3. ĐĂNG XUẤT (LOGOUT)
        // ===================================================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthScheme);
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống UTC STADIUM.";
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào khu vực này.";
            return RedirectToAction(nameof(Login));
        }

        // ===================================================================
        // 4. QUÊN MẬT KHẨU - GỬI MÃ OTP QUA GMAIL SMTP (FORGOT PASSWORD)
        // ===================================================================
        [HttpGet]
        public IActionResult ForgotPassword(string? email = null)
        {
            var model = new ForgotPasswordViewModel
            {
                Email = email?.Trim() ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();

            // Kiểm tra Email có tồn tại trong bảng NguoiDung hay không
            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                ModelState.AddModelError(nameof(model.Email), "Địa chỉ Email này chưa được đăng ký tài khoản trên hệ thống UTC STADIUM.");
                return View(model);
            }

            // Tạo ngẫu nhiên mã OTP gồm 6 chữ số (từ 100000 đến 999999)
            var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

            // Lưu mã OTP vào MemoryCache với thời gian hiệu lực 5 phút (không cần sửa bảng NguoiDung)
            var cacheKey = GetOtpCacheKey(normalizedEmail);
            _cache.Set(cacheKey, otpCode, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpireMinutes)
            });

            try
            {
                // Gửi email chứa mã OTP qua Gmail SMTP
                await _emailService.SendOtpEmailAsync(user.Email, user.HoTen, otpCode, OtpExpireMinutes);

                TempData["SuccessMessage"] = $"Mã xác thực OTP gồm 6 chữ số đã được gửi tới {user.Email}. Mã có hiệu lực trong vòng {OtpExpireMinutes} phút.";
                return RedirectToAction(nameof(ResetPassword), new { email = user.Email });
            }
            catch (Exception ex)
            {
                // Xóa OTP trong cache nếu gửi email thất bại
                _cache.Remove(cacheKey);
                ModelState.AddModelError(
                    string.Empty,
                    $"Không thể gửi email OTP qua Gmail SMTP. Vui lòng kiểm tra cấu hình EmailSettings (SenderEmail & SenderAppPassword 16 ký tự) trong appsettings.json. Chi tiết: {ex.Message}");
                return View(model);
            }
        }

        // ===================================================================
        // 5. ĐẶT LẠI MẬT KHẨU MỚI BẰNG MÃ OTP (RESET PASSWORD)
        // ===================================================================
        [HttpGet]
        public IActionResult ResetPassword(string? email = null)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email?.Trim() ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var otpInput = model.MaOtp.Trim();
            var cacheKey = GetOtpCacheKey(normalizedEmail);

            // Kiểm tra mã OTP trong MemoryCache còn hạn hay không
            if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || string.IsNullOrWhiteSpace(cachedOtp))
            {
                ModelState.AddModelError(
                    nameof(model.MaOtp),
                    $"Mã xác thực OTP đã hết hạn (quá {OtpExpireMinutes} phút) hoặc chưa được yêu cầu. Vui lòng nhấn 'Gửi lại mã OTP'.");
                return View(model);
            }

            // Kiểm tra mã OTP có khớp hoàn toàn không
            if (!string.Equals(cachedOtp, otpInput, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.MaOtp), "Mã xác thực OTP không chính xác. Vui lòng kiểm tra lại hộp thư email của bạn.");
                return View(model);
            }

            // Tìm người dùng trong bảng NguoiDung theo Email
            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                ModelState.AddModelError(nameof(model.Email), "Không tìm thấy tài khoản tương ứng với địa chỉ Email này.");
                return View(model);
            }

            // Băm (Hash) mật khẩu mới an toàn và cập nhật cột MatKhauMaHoa trong bảng NguoiDung
            user.MatKhauMaHoa = _passwordHasher.HashPassword(user, model.MatKhauMoi);
            user.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

            // Xóa mã OTP khỏi bộ nhớ đệm ngay sau khi đặt lại mật khẩu thành công
            _cache.Remove(cacheKey);

            TempData["SuccessMessage"] = "Đặt lại mật khẩu mới thành công! Vui lòng đăng nhập bằng mật khẩu vừa cập nhật.";
            TempData["RegisteredEmail"] = user.Email;

            return RedirectToAction(nameof(Login));
        }

        private static string GetOtpCacheKey(string normalizedEmail)
        {
            return $"UTC_STADIUM_OTP_RESET_{normalizedEmail}";
        }

        // ===================================================================
        // CÁC PHƯƠNG THỨC HỖ TRỢ NỘI BỘ (PRIVATE HELPERS)
        // ===================================================================
        private async Task<QuyenHan> GetOrCreateDefaultCustomerRoleAsync()
        {
            var allRoles = await _context.QuyenHans.ToListAsync();

            var customerRole = allRoles.FirstOrDefault(r =>
                r.TenQuyen.Contains("Khách", StringComparison.OrdinalIgnoreCase) ||
                r.TenQuyen.Contains("Khach", StringComparison.OrdinalIgnoreCase) ||
                r.TenQuyen.Contains("Customer", StringComparison.OrdinalIgnoreCase) ||
                r.TenQuyen.Contains("Sinh viên", StringComparison.OrdinalIgnoreCase) ||
                r.TenQuyen.Contains("User", StringComparison.OrdinalIgnoreCase));

            if (customerRole != null)
            {
                return customerRole;
            }

            // Nếu bảng QuyenHan chưa có dữ liệu, tự động khởi tạo 2 quyền chuẩn "Admin" và "Khách hàng"
            if (allRoles.Count == 0)
            {
                var adminRole = new QuyenHan { TenQuyen = "Admin" };
                var newCustomerRole = new QuyenHan { TenQuyen = "Khách hàng" };
                _context.QuyenHans.AddRange(adminRole, newCustomerRole);
                await _context.SaveChangesAsync();
                return newCustomerRole;
            }

            // Nếu có sẵn bảng QuyenHan nhưng chưa có quyền Khách hàng thì lấy quyền không phải Admin hoặc tạo mới
            var nonAdminRole = allRoles.FirstOrDefault(r => !IsAdminRole(r.TenQuyen));
            if (nonAdminRole != null)
            {
                return nonAdminRole;
            }

            var createdCustomerRole = new QuyenHan { TenQuyen = "Khách hàng" };
            _context.QuyenHans.Add(createdCustomerRole);
            await _context.SaveChangesAsync();
            return createdCustomerRole;
        }

        private bool VerifyAndUpgradePasswordIfNeeded(NguoiDung user, string inputPassword, out bool shouldSaveRehash)
        {
            shouldSaveRehash = false;

            if (string.IsNullOrEmpty(user.MatKhauMaHoa))
            {
                return false;
            }

            // 1. Thử xác thực bằng PasswordHasher chuẩn của ASP.NET Core Identity
            try
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.MatKhauMaHoa, inputPassword);
                if (verificationResult == PasswordVerificationResult.Success)
                {
                    return true;
                }
                if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.MatKhauMaHoa = _passwordHasher.HashPassword(user, inputPassword);
                    shouldSaveRehash = true;
                    return true;
                }
            }
            catch (FormatException)
            {
                // Trường hợp mật khẩu trong DB được seed thủ công bằng SQL (Plain-text hoặc SHA256 Hex)
            }

            // 2. Hỗ trợ kiểm tra SHA256 hoặc Plain-text cho các tài khoản Admin/Khách được seed từ SQL Script
            var sha256Hex = ComputeSha256Hex(inputPassword);
            if (user.MatKhauMaHoa == inputPassword ||
                user.MatKhauMaHoa.Equals(sha256Hex, StringComparison.OrdinalIgnoreCase))
            {
                // Tự động nâng cấp sang chuỗi băm PasswordHasher an toàn
                user.MatKhauMaHoa = _passwordHasher.HashPassword(user, inputPassword);
                shouldSaveRehash = true;
                return true;
            }

            return false;
        }

        private static string ComputeSha256Hex(string rawData)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes);
        }

        private static bool IsAdminRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            return roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Contains("Quản trị", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Contains("Quan tri", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Contains("QuanTri", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult RedirectByUserRole(string? returnUrl = null)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Quản trị viên") || User.IsInRole("Quản trị"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "KhachHang" });
        }
    }
}


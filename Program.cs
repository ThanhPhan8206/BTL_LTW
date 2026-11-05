using Microsoft.EntityFrameworkCore;
using QLSanBong.Models.Entities;
using QLSanBong.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký kết nối Database DbContext
builder.Services.AddDbContext<QLSanBongDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Thêm Cookie Authentication để phân quyền sau này
builder.Services.AddAuthentication("AppCookieAuth")
    .AddCookie("AppCookieAuth", options =>
    {
        options.Cookie.Name = "QLSanBong.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

// 3. Đăng ký MemoryCache (lưu OTP tạm thời 5 phút) & EmailService gửi mã OTP qua Gmail SMTP
builder.Services.AddMemoryCache();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();

// 4. Đăng ký Session lưu thông tin phiên làm việc
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Route cho Area (Admin & KhachHang)
app.MapAreaControllerRoute(
    name: "admin_area",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapAreaControllerRoute(
    name: "khachhang_area",
    areaName: "KhachHang",
    pattern: "KhachHang/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

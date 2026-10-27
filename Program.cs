using Microsoft.EntityFrameworkCore;
using QLSanBong.Models.Entities;

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Route cho Area (Admin & KhachHang)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
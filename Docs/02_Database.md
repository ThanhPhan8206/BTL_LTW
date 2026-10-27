# THIẾT KẾ CƠ SỞ DỮ LIỆU QLSANBONG 

> Kịch bản tạo Database cho hệ thống Quản lý Đặt Sân Bóng Đá (ASP.NET Core MVC .NET 10).

---


```sql
DROP DATABASE IF EXISTS QLSanBong;
GO

CREATE DATABASE QLSanBong;
GO

USE QLSanBong;
GO

-- 1. Bảng Quyền hạn (Phân quyền người dùng)
CREATE TABLE QuyenHan (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenQuyen NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- 2. Bảng Người dùng
CREATE TABLE NguoiDung (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuyenHanId INT NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(20) NOT NULL,
    MatKhauMaHoa VARCHAR(255) NOT NULL,
    AnhDaiDienUrl VARCHAR(255) NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Hoạt động',
    NgayTao DATETIME2 DEFAULT GETDATE(),
    NgayCapNhat DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_NguoiDung_QuyenHan FOREIGN KEY (QuyenHanId) REFERENCES QuyenHan(Id)
);
GO

-- 3. Bảng Sân bóng
CREATE TABLE SanBong (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenSan NVARCHAR(100) NOT NULL,
    LoaiSan NVARCHAR(50) NOT NULL,
    MoTa NVARCHAR(MAX) NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Sẵn sàng',
    NgayTao DATETIME2 DEFAULT GETDATE(),
    AnhSan NVARCHAR(255) NULL
);
GO

-- 4. Bảng Lịch bảo trì sân
CREATE TABLE LichBaoTri (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SanBongId INT NOT NULL,
    ThoiGianBatDau DATETIME2 NOT NULL,
    ThoiGianKetThuc DATETIME2 NOT NULL,
    GhiChu NVARCHAR(MAX) NULL,
    CONSTRAINT FK_LichBaoTri_SanBong FOREIGN KEY (SanBongId) REFERENCES SanBong(Id) ON DELETE CASCADE
);
GO

-- 5. Bảng Khung giờ (Ca đá)
CREATE TABLE KhungGio (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    GioBatDau TIME NOT NULL,
    GioKetThuc TIME NOT NULL
);
GO

-- 6. Bảng Thiết lập giá linh hoạt
CREATE TABLE ThietLapGia (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SanBongId INT NOT NULL,
    KhungGioId INT NOT NULL,
    NgayTrongTuan NVARCHAR(20) NOT NULL,
    GiaThue DECIMAL(18,2) NOT NULL CHECK (GiaThue >= 0),
    CONSTRAINT FK_ThietLapGia_SanBong FOREIGN KEY (SanBongId) REFERENCES SanBong(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ThietLapGia_KhungGio FOREIGN KEY (KhungGioId) REFERENCES KhungGio(Id) ON DELETE CASCADE
);
GO

-- 7. Bảng Dịch vụ đi kèm
CREATE TABLE DichVu (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenDichVu NVARCHAR(100) NOT NULL,
    GiaDichVu DECIMAL(18,2) NOT NULL CHECK (GiaDichVu >= 0),
    DonViTinh NVARCHAR(20) NOT NULL
);
GO

-- 8. Bảng Đặt sân bóng
CREATE TABLE DatSan (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NguoiDungId INT NOT NULL,
    SanBongId INT NOT NULL,
    KhungGioId INT NOT NULL,
    NgayDat DATE NOT NULL,
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (TongTien >= 0),
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
    NgayTao DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_DatSan_NguoiDung FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung(Id),
    CONSTRAINT FK_DatSan_SanBong FOREIGN KEY (SanBongId) REFERENCES SanBong(Id),
    CONSTRAINT FK_DatSan_KhungGio FOREIGN KEY (KhungGioId) REFERENCES KhungGio(Id)
);
GO

-- 9. Bảng Chi tiết dịch vụ theo đơn đặt sân
CREATE TABLE ChiTietDichVuDat (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DatSanId INT NOT NULL,
    DichVuId INT NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGia DECIMAL(18,2) NOT NULL CHECK (DonGia >= 0),
    CONSTRAINT FK_ChiTietDichVuDat_DatSan FOREIGN KEY (DatSanId) REFERENCES DatSan(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ChiTietDichVuDat_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id)
);
GO

-- 10. Bảng Yêu cầu đổi lịch đặt
CREATE TABLE YeuCauDoiLich (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DatSanId INT NOT NULL,
    SanBongMoiId INT NOT NULL,
    KhungGioMoiId INT NOT NULL,
    NgayDatMoi DATE NOT NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
    LyDo NVARCHAR(MAX) NULL,
    NgayTao DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_YeuCauDoiLich_DatSan FOREIGN KEY (DatSanId) REFERENCES DatSan(Id),
    CONSTRAINT FK_YeuCauDoiLich_SanBong FOREIGN KEY (SanBongMoiId) REFERENCES SanBong(Id),
    CONSTRAINT FK_YeuCauDoiLich_KhungGio FOREIGN KEY (KhungGioMoiId) REFERENCES KhungGio(Id)
);
GO

-- 11. Bảng Thanh toán
CREATE TABLE ThanhToan (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DatSanId INT NOT NULL,
    SoTien DECIMAL(18,2) NOT NULL CHECK (SoTien >= 0),
    PhuongThucThanhToan NVARCHAR(50) NOT NULL,
    LoaiThanhToan NVARCHAR(50) NOT NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý',
    MaGiaoDich VARCHAR(100) NULL,
    ThoiGianThanhToan DATETIME2 NULL,
    CONSTRAINT FK_ThanhToan_DatSan FOREIGN KEY (DatSanId) REFERENCES DatSan(Id) ON DELETE CASCADE
);
GO

-- 12. Bảng Hóa đơn
CREATE TABLE HoaDon (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DatSanId INT NOT NULL UNIQUE,
    MaHoaDon VARCHAR(50) NOT NULL UNIQUE,
    NgayXuat DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_HoaDon_DatSan FOREIGN KEY (DatSanId) REFERENCES DatSan(Id) ON DELETE CASCADE
);
GO

-- 13. Bảng Mã giảm giá
CREATE TABLE MaGiamGia (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MaCode VARCHAR(50) NOT NULL UNIQUE,
    PhanTramGiam INT NULL CHECK (PhanTramGiam BETWEEN 1 AND 100),
    SoTienGiam DECIMAL(18,2) NULL CHECK (SoTienGiam >= 0),
    HieuLucTu DATETIME2 NOT NULL,
    HieuLucDen DATETIME2 NOT NULL,
    GiaTriDonToiThieu DECIMAL(18,2) DEFAULT 0
);
GO

-- 14. Bảng Thông báo người dùng
CREATE TABLE ThongBao (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NguoiDungId INT NOT NULL,
    TieuDe NVARCHAR(200) NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    LoaiThongBao NVARCHAR(50) NOT NULL,
    DaDoc BIT DEFAULT 0,
    NgayTao DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_ThongBao_NguoiDung FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung(Id) ON DELETE CASCADE
);
GO

-- 15. Bảng Đánh giá sân bóng
CREATE TABLE DanhGia (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DatSanId INT NOT NULL UNIQUE,
    NguoiDungId INT NOT NULL,
    SanBongId INT NOT NULL,
    SoSao INT NOT NULL CHECK (SoSao BETWEEN 1 AND 5),
    BinhLuan NVARCHAR(MAX) NULL,
    NgayTao DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_DanhGia_DatSan FOREIGN KEY (DatSanId) REFERENCES DatSan(Id),
    CONSTRAINT FK_DanhGia_NguoiDung FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung(Id),
    CONSTRAINT FK_DanhGia_SanBong FOREIGN KEY (SanBongId) REFERENCES SanBong(Id)
);
GO
```

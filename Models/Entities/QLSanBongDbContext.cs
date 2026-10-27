using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QLSanBong.Models.Entities;

public partial class QLSanBongDbContext : DbContext
{
    public QLSanBongDbContext()
    {
    }

    public QLSanBongDbContext(DbContextOptions<QLSanBongDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietDichVuDat> ChiTietDichVuDats { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<DatSan> DatSans { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhungGio> KhungGios { get; set; }

    public virtual DbSet<LichBaoTri> LichBaoTris { get; set; }

    public virtual DbSet<MaGiamGium> MaGiamGia { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<QuyenHan> QuyenHans { get; set; }

    public virtual DbSet<SanBong> SanBongs { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThietLapGium> ThietLapGia { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<YeuCauDoiLich> YeuCauDoiLiches { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=QLSanBong;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietDichVuDat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChiTietD__3214EC078C194078");

            entity.ToTable("ChiTietDichVuDat");

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.DatSan).WithMany(p => p.ChiTietDichVuDats)
                .HasForeignKey(d => d.DatSanId)
                .HasConstraintName("FK_ChiTietDichVuDat_DatSan");

            entity.HasOne(d => d.DichVu).WithMany(p => p.ChiTietDichVuDats)
                .HasForeignKey(d => d.DichVuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietDichVuDat_DichVu");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhGia__3214EC0794AC394E");

            entity.HasIndex(e => e.DatSanId, "UQ__DanhGia__AE3C65EB595F5939").IsUnique();

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.DatSan).WithOne(p => p.DanhGium)
                .HasForeignKey<DanhGium>(d => d.DatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_DatSan");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_NguoiDung");

            entity.HasOne(d => d.SanBong).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.SanBongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_SanBong");
        });

        modelBuilder.Entity<DatSan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DatSan__3214EC074FEEC9FF");

            entity.ToTable("DatSan");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ duyệt");

            entity.HasOne(d => d.KhungGio).WithMany(p => p.DatSans)
                .HasForeignKey(d => d.KhungGioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatSan_KhungGio");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DatSans)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatSan_NguoiDung");

            entity.HasOne(d => d.SanBong).WithMany(p => p.DatSans)
                .HasForeignKey(d => d.SanBongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatSan_SanBong");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DichVu__3214EC073F676D4C");

            entity.ToTable("DichVu");

            entity.Property(e => e.DonViTinh).HasMaxLength(20);
            entity.Property(e => e.GiaDichVu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenDichVu).HasMaxLength(100);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HoaDon__3214EC07DE3D5F0F");

            entity.ToTable("HoaDon");

            entity.HasIndex(e => e.MaHoaDon, "UQ__HoaDon__835ED13AEDEF7960").IsUnique();

            entity.HasIndex(e => e.DatSanId, "UQ__HoaDon__AE3C65EB04E36C6D").IsUnique();

            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NgayXuat).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.DatSan).WithOne(p => p.HoaDon)
                .HasForeignKey<HoaDon>(d => d.DatSanId)
                .HasConstraintName("FK_HoaDon_DatSan");
        });

        modelBuilder.Entity<KhungGio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__KhungGio__3214EC074A9432C3");

            entity.ToTable("KhungGio");
        });

        modelBuilder.Entity<LichBaoTri>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LichBaoT__3214EC073D6A11A2");

            entity.ToTable("LichBaoTri");

            entity.HasOne(d => d.SanBong).WithMany(p => p.LichBaoTris)
                .HasForeignKey(d => d.SanBongId)
                .HasConstraintName("FK_LichBaoTri_SanBong");
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MaGiamGi__3214EC07085B7EAD");

            entity.HasIndex(e => e.MaCode, "UQ__MaGiamGi__152C7C5C4F733BAA").IsUnique();

            entity.Property(e => e.GiaTriDonToiThieu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SoTienGiam).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NguoiDun__3214EC072C7A878F");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.Email, "UQ__NguoiDun__A9D10534A95194B5").IsUnique();

            entity.Property(e => e.AnhDaiDienUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MatKhauMaHoa)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Hoạt động");

            entity.HasOne(d => d.QuyenHan).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.QuyenHanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NguoiDung_QuyenHan");
        });

        modelBuilder.Entity<QuyenHan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QuyenHan__3214EC07B513A0D2");

            entity.ToTable("QuyenHan");

            entity.HasIndex(e => e.TenQuyen, "UQ__QuyenHan__5637EE79ECEB6F9D").IsUnique();

            entity.Property(e => e.TenQuyen).HasMaxLength(50);
        });

        modelBuilder.Entity<SanBong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanBong__3214EC076F0D23E4");

            entity.ToTable("SanBong");

            entity.Property(e => e.AnhSan).HasMaxLength(255);
            entity.Property(e => e.LoaiSan).HasMaxLength(50);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TenSan).HasMaxLength(100);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Sẵn sàng");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThanhToa__3214EC07F96AA7A5");

            entity.ToTable("ThanhToan");

            entity.Property(e => e.LoaiThanhToan).HasMaxLength(50);
            entity.Property(e => e.MaGiaoDich)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(50);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ xử lý");

            entity.HasOne(d => d.DatSan).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.DatSanId)
                .HasConstraintName("FK_ThanhToan_DatSan");
        });

        modelBuilder.Entity<ThietLapGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThietLap__3214EC07DB575804");

            entity.Property(e => e.GiaThue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayTrongTuan).HasMaxLength(20);

            entity.HasOne(d => d.KhungGio).WithMany(p => p.ThietLapGia)
                .HasForeignKey(d => d.KhungGioId)
                .HasConstraintName("FK_ThietLapGia_KhungGio");

            entity.HasOne(d => d.SanBong).WithMany(p => p.ThietLapGia)
                .HasForeignKey(d => d.SanBongId)
                .HasConstraintName("FK_ThietLapGia_SanBong");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThongBao__3214EC0792F40628");

            entity.ToTable("ThongBao");

            entity.Property(e => e.DaDoc).HasDefaultValue(false);
            entity.Property(e => e.LoaiThongBao).HasMaxLength(50);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TieuDe).HasMaxLength(200);

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK_ThongBao_NguoiDung");
        });

        modelBuilder.Entity<YeuCauDoiLich>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__YeuCauDo__3214EC07C381AFBC");

            entity.ToTable("YeuCauDoiLich");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ duyệt");

            entity.HasOne(d => d.DatSan).WithMany(p => p.YeuCauDoiLiches)
                .HasForeignKey(d => d.DatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_YeuCauDoiLich_DatSan");

            entity.HasOne(d => d.KhungGioMoi).WithMany(p => p.YeuCauDoiLiches)
                .HasForeignKey(d => d.KhungGioMoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_YeuCauDoiLich_KhungGio");

            entity.HasOne(d => d.SanBongMoi).WithMany(p => p.YeuCauDoiLiches)
                .HasForeignKey(d => d.SanBongMoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_YeuCauDoiLich_SanBong");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

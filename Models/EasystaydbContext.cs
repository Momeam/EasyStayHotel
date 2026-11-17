using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace EasyStayHotel.Models;

/// <summary>
/// คลาส EasystaydbContext: หัวใจสำคัญของ Entity Framework Core (Database Context)
/// หน้าที่: จัดการการเชื่อมต่อ (Connection) และการแปลงข้อมูล (Mapping) ระหว่าง C# Objects กับตารางในฐานข้อมูล MySQL
/// 
/// คำถามที่อาจารย์มักถามเกี่ยวกับ DbContext:
/// 1. DbSet คืออะไร?
///    -> คือตัวแทนของตาราง (Table) ในฐานข้อมูล ช่วยให้สามารถทำ CRUD Operations (Create, Read, Update, Delete) ผ่าน LINQ ได้
/// 2. OnModelCreating มีหน้าที่อะไร?
///    -> ใช้ Fluent API ในการกำหนด Schema, Primary Key, Foreign Key Constraints, ความยาวคอลัมน์ (MaxLength), 
///       การทำ Index และ Collation ภาษาไทย (utf8mb4_unicode_ci)
/// 3. ทำไมต้องใช้ utf8mb4?
///    -> เพื่อรองรับตัวอักษรภาษาไทยและอักขระสากลได้อย่างสมบูรณ์ โดยไม่เกิดปัญหาตัวหนังสือกลายเป็นเครื่องหมายคำถาม (?)
/// </summary>
public partial class EasystaydbContext : DbContext
{
    public EasystaydbContext()
    {
    }

    /// <summary>
    /// Constructor สำหรับรับ DbContextOptions เข้ามาผ่าน Dependency Injection (ลงทะเบียนไว้ใน Program.cs)
    /// </summary>
    public EasystaydbContext(DbContextOptions<EasystaydbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// ตาราง bookings: ข้อมูลการจองห้องพัก
    /// </summary>
    public virtual DbSet<Booking> Bookings { get; set; }

    /// <summary>
    /// ตาราง payments: ข้อมูลธุรกรรมการชำระเงินและสลิป
    /// </summary>
    public virtual DbSet<Payment> Payments { get; set; }

    /// <summary>
    /// ตาราง rooms: ข้อมูลห้องพักและสถานะความพร้อมใช้งาน
    /// </summary>
    public virtual DbSet<Room> Rooms { get; set; }

    /// <summary>
    /// ตาราง roomtypes: ข้อมูลประเภทห้องพัก ราคา และความจุ
    /// </summary>
    public virtual DbSet<Roomtype> Roomtypes { get; set; }

    /// <summary>
    /// ตาราง users: ข้อมูลผู้ใช้งานระบบทุกประเภท (ลูกค้า, พนักงาน, ผู้บริหาร, แม่บ้าน)
    /// </summary>
    public virtual DbSet<User> Users { get; set; }

    /// <summary>
    /// การตั้งค่าการเชื่อมต่อในกรณีไม่ได้กำหนดผ่าน Dependency Injection (Fallback Configuration)
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=easystaydb;user=root;password=1234", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.46-mysql"));

    /// <summary>
    /// กำหนดโครงสร้าง Schema ความสัมพันธ์ และข้อกำหนดต่างๆ ของแต่ละตารางด้วย Fluent API
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // กำหนด Collation และ Character Set ให้รองรับภาษาไทย
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        // -------------------------------------------------------------
        // 1. ตาราง bookings
        // -------------------------------------------------------------
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PRIMARY");
            entity.ToTable("bookings");

            // ดัชนีสำหรับการค้นหาที่รวดเร็ว (Indexes)
            entity.HasIndex(e => e.RoomId, "RoomId");
            entity.HasIndex(e => e.UserId, "UserId");

            entity.Property(e => e.BookingRef).HasMaxLength(20);
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'");
            entity.Property(e => e.TotalPrice).HasPrecision(10, 2);

            // ความสัมพันธ์ Foreign Key: การจองผูกกับห้องพัก (Room)
            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("bookings_ibfk_2");

            // ความสัมพันธ์ Foreign Key: การจองผูกกับผู้ใช้ (User)
            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("bookings_ibfk_1");
        });

        // -------------------------------------------------------------
        // 2. ตาราง payments
        // -------------------------------------------------------------
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PRIMARY");
            entity.ToTable("payments");

            entity.HasIndex(e => e.BookingId, "BookingId");

            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'");
            entity.Property(e => e.SlipImageUrl).HasMaxLength(255);
            entity.Property(e => e.TransferDate).HasColumnType("datetime");

            // ความสัมพันธ์ Foreign Key: การชำระเงินผูกกับรายการจอง (Booking)
            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("payments_ibfk_1");
        });

        // -------------------------------------------------------------
        // 3. ตาราง rooms
        // -------------------------------------------------------------
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PRIMARY");
            entity.ToTable("rooms");

            entity.HasIndex(e => e.RoomTypeId, "RoomTypeId");

            entity.Property(e => e.RoomNo).HasMaxLength(10);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Available'");

            // ความสัมพันธ์ Foreign Key: ห้องพักผูกกับประเภทห้องพัก (Roomtype)
            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .HasConstraintName("rooms_ibfk_1");

            entity.Property(e => e.Branch)
                .HasMaxLength(100)
                .HasColumnName("Branch");    
        });

        // -------------------------------------------------------------
        // 4. ตาราง roomtypes
        // -------------------------------------------------------------
        modelBuilder.Entity<Roomtype>(entity =>
        {
            entity.HasKey(e => e.RoomTypeId).HasName("PRIMARY");
            entity.ToTable("roomtypes");

            entity.Property(e => e.BasePrice).HasPrecision(10, 2);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.TypeName).HasMaxLength(100);
        });

        // -------------------------------------------------------------
        // 5. ตาราง users
        // -------------------------------------------------------------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");
            entity.ToTable("users");

            // ป้องกันการสมัครสมาชิกด้วยอีเมลซ้ำ (Unique Constraint)
            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdCardOrPassport).HasMaxLength(20);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Customer'");
            entity.Property(e => e.DateOfBirth)
                .HasColumnType("date")
                .HasColumnName("DateOfBirth");    
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

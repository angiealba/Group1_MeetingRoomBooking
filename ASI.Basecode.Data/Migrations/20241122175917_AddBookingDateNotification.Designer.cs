﻿// <auto-generated />
using System;
using ASI.Basecode.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    [DbContext(typeof(AsiBasecodeDBContext))]
    [Migration("20241122175917_AddBookingDateNotification")]
    partial class AddBookingDateNotification
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("ASI.Basecode.Data.Models.Booking", b =>
                {
                    b.Property<int>("bookingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("bookingId"), 1L, 1);

                    b.Property<int>("ID")
                        .HasColumnType("int");

                    b.Property<string>("bookingRefId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("date")
                        .HasColumnType("datetime2");

                    b.Property<int>("duration")
                        .HasColumnType("int");

                    b.Property<bool>("isRecurring")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("recurrenceEndDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("recurrenceFrequency")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("recurringBookingId")
                        .HasColumnType("int");

                    b.Property<int>("roomId")
                        .HasColumnType("int");

                    b.Property<DateTime>("time")
                        .HasColumnType("datetime2");

                    b.HasKey("bookingId");

                    b.HasIndex("ID");

                    b.HasIndex("roomId");

                    b.ToTable("Bookings");
                });

            modelBuilder.Entity("ASI.Basecode.Data.Models.Notification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime?>("BookingDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("userId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ASI.Basecode.Data.Models.RecurringIdTracker", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"), 1L, 1);

                    b.HasKey("id");

                    b.ToTable("RecurringIdTrackers");
                });

            modelBuilder.Entity("ASI.Basecode.Data.Models.Room", b =>
                {
                    b.Property<int>("roomId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("roomId"), 1L, 1);

                    b.Property<string>("availableFacilities")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("createdBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("roomCapacity")
                        .HasColumnType("int");

                    b.Property<string>("roomLocation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("roomName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("roomId");

                    b.ToTable("Rooms");
                });

            modelBuilder.Entity("ASI.Basecode.Data.Models.User", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"), 1L, 1);

                    b.Property<string>("createdBy")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("createdTime")
                        .HasColumnType("datetime");

                    b.Property<int>("defaultBookingDuration")
                        .HasColumnType("int");

                    b.Property<string>("email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("enableNotifications")
                        .HasColumnType("bit");

                    b.Property<string>("name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("password")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("role")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("updatedBy")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("updatedTime")
                        .HasColumnType("datetime");

                    b.Property<string>("userID")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.HasKey("ID");

                    b.HasIndex(new[] { "userID" }, "UQ__Users__1788CC4D5F4A160F")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ASI.Basecode.Data.Models.Booking", b =>
                {
                    b.HasOne("ASI.Basecode.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("ID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ASI.Basecode.Data.Models.Room", "Room")
                        .WithMany()
                        .HasForeignKey("roomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Room");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}

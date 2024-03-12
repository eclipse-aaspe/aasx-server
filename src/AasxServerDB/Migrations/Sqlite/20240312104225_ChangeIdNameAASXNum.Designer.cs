﻿// <auto-generated />
using AasxServerDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    [DbContext(typeof(SqliteAasContext))]
    [Migration("20240312104225_ChangeIdNameAASXNum")]
    partial class ChangeIdNameAASXNum
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("AasxServerDB.AASSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AASId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("AASNum")
                        .HasColumnType("INTEGER");

                    b.Property<long>("AASXId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AssetKind")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GlobalAssetId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("IdShort")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AASNum");

                    b.ToTable("AASSets");
                });

            modelBuilder.Entity("AasxServerDB.AASXSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AASX")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("AASXSets");
                });

            modelBuilder.Entity("AasxServerDB.DValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotation")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("ParentSMENum")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMENum");

                    b.ToTable("DValueSets");
                });

            modelBuilder.Entity("AasxServerDB.IValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotation")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("ParentSMENum")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Value")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMENum");

                    b.ToTable("IValueSets");
                });

            modelBuilder.Entity("AasxServerDB.SMESet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("IdShort")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("ParentSMENum")
                        .HasColumnType("INTEGER");

                    b.Property<long>("SMENum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SMEType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("SMNum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SemanticId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ValueType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SMENum");

                    b.HasIndex("SMNum");

                    b.ToTable("SMESets");
                });

            modelBuilder.Entity("AasxServerDB.SMSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("AASNum")
                        .HasColumnType("INTEGER");

                    b.Property<long>("AASXId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IdShort")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SMId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("SMNum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SemanticId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SMNum");

                    b.ToTable("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.SValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotation")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("ParentSMENum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMENum");

                    b.ToTable("SValueSets");
                });
#pragma warning restore 612, 618
        }
    }
}

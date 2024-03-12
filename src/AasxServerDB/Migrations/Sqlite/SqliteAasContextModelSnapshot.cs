﻿// <auto-generated />
using AasxServerDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AasxServerStandardBib.Migrations.Sqlite
{
    [DbContext(typeof(SqliteAasContext))]
    partial class SqliteAasContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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

                    b.Property<int>("AASXId")
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

                    b.HasIndex("Id");

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

                    b.Property<int>("ParentSMEId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMEId");

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

                    b.Property<int>("ParentSMEId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Value")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMEId");

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

                    b.Property<int>("ParentSMEId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SMEType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("SMId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SemanticId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ValueType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("SMESets");
                });

            modelBuilder.Entity("AasxServerDB.SMSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AASId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AASXId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IdShort")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SMId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SemanticId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id");

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

                    b.Property<int>("ParentSMEId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMEId");

                    b.ToTable("SValueSets");
                });
#pragma warning restore 612, 618
        }
    }
}

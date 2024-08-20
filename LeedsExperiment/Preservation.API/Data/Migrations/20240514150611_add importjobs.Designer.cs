﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Preservation.API.Data;

#nullable disable

namespace Preservation.API.Data.Migrations
{
    [DbContext(typeof(PreservationContext))]
    [Migration("20240514150611_add importjobs")]
    partial class addimportjobs
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Preservation.API.Data.Entities.DepositEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("created_by");

                    b.Property<DateTime?>("DateExported")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_exported");

                    b.Property<DateTime?>("DatePreserved")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_preserved");

                    b.Property<DateTime?>("LastModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_modified");

                    b.Property<string>("LastModifiedBy")
                        .HasColumnType("text")
                        .HasColumnName("last_modified_by");

                    b.Property<string>("PreservationPath")
                        .HasColumnType("text")
                        .HasColumnName("preservation_path");

                    b.Property<string>("S3Root")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("s3root");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("status");

                    b.Property<string>("SubmissionText")
                        .HasColumnType("text")
                        .HasColumnName("submission_text");

                    b.Property<string>("VersionExported")
                        .HasColumnType("text")
                        .HasColumnName("version_exported");

                    b.Property<string>("VersionSaved")
                        .HasColumnType("text")
                        .HasColumnName("version_saved");

                    b.HasKey("Id")
                        .HasName("pk_deposits");

                    b.ToTable("deposits", (string)null);
                });

            modelBuilder.Entity("Preservation.API.Data.Entities.ImportJobEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("BinariesAdded")
                        .HasColumnType("jsonb")
                        .HasColumnName("binaries_added");

                    b.Property<string>("BinariesDeleted")
                        .HasColumnType("jsonb")
                        .HasColumnName("binaries_deleted");

                    b.Property<string>("BinariesPatched")
                        .HasColumnType("jsonb")
                        .HasColumnName("binaries_patched");

                    b.Property<string>("ContainersAdded")
                        .HasColumnType("jsonb")
                        .HasColumnName("containers_added");

                    b.Property<string>("ContainersDeleted")
                        .HasColumnType("jsonb")
                        .HasColumnName("containers_deleted");

                    b.Property<DateTime?>("DateBegun")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_begun");

                    b.Property<DateTime?>("DateFinished")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_finished");

                    b.Property<DateTime?>("DateSubmitted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_submitted")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Deposit")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("deposit");

                    b.Property<string>("DigitalObject")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("digital_object");

                    b.Property<string>("Errors")
                        .HasColumnType("jsonb")
                        .HasColumnName("errors");

                    b.Property<string>("NewVersion")
                        .HasColumnType("text")
                        .HasColumnName("new_version");

                    b.Property<string>("OriginalImportJobId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("original_import_job_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("status");

                    b.HasKey("Id")
                        .HasName("pk_import_jobs");

                    b.ToTable("import_jobs", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}

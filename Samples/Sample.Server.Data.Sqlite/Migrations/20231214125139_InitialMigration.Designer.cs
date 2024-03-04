﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sample.Server.Data.Sqlite;

#nullable disable

namespace Sample.Server.Data.Sqlite.Migrations
{
    [DbContext(typeof(ServerSqliteDatabase))]
    [Migration("20231214125139_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true);

            modelBuilder.Entity("Sample.Shared.Storage.Server.AccountEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("AccountId");

                    b.Property<long>("AddressId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("AccountAddressId");

                    b.Property<Guid>("AddressSyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("AccountAddressSyncId");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("AccountCreatedOn");

                    b.Property<string>("EmailAddress")
                        .HasColumnType("TEXT")
                        .HasColumnName("AccountEmailAddress");

                    b.Property<string>("ExternalId")
                        .HasColumnType("TEXT")
                        .HasColumnName("AccountExternalId");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER")
                        .HasColumnName("AccountIsDeleted");

                    b.Property<DateTime>("LastLoginDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("AccountLastLoginDate");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("AccountModifiedOn");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("AccountName");

                    b.Property<string>("Nickname")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("AccountNickname");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT")
                        .HasColumnName("AccountPasswordHash");

                    b.Property<string>("Roles")
                        .HasColumnType("TEXT")
                        .HasColumnName("AccountRoles");

                    b.Property<Guid>("SyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("AccountSyncId");

                    b.HasKey("Id");

                    b.HasIndex("AddressId")
                        .HasDatabaseName("IX_Accounts_AddressId");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("IX_Accounts_Name");

                    b.HasIndex("Nickname")
                        .IsUnique()
                        .HasDatabaseName("IX_Accounts_Nickname");

                    b.HasIndex("SyncId")
                        .IsUnique()
                        .HasDatabaseName("IX_Accounts_SyncId");

                    b.HasIndex("AddressId", "ExternalId")
                        .IsUnique()
                        .HasDatabaseName("IX_Accounts_AddressId_ExternalId");

                    b.ToTable("Accounts", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.AddressEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("AddressId");

                    b.Property<int?>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("AccountSyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("AddressAccountSyncId");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("AddressCity");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("AddressCreatedOn");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER")
                        .HasColumnName("AddressIsDeleted");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("AddressLineOne");

                    b.Property<string>("Line2")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("AddressLineTwo");

                    b.Property<long?>("LinkedAddressId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("AddressLinkedAddressId");

                    b.Property<Guid?>("LinkedAddressSyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("AddressLinkedAddressSyncId");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("AddressModifiedOn");

                    b.Property<string>("Postal")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT")
                        .HasColumnName("AddressPostal");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT")
                        .HasColumnName("AddressState");

                    b.Property<Guid>("SyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("AddressSyncId");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("LinkedAddressId")
                        .HasDatabaseName("IX_Address_LinkedAddressId");

                    b.HasIndex("SyncId")
                        .IsUnique()
                        .HasDatabaseName("IX_Address_SyncId");

                    b.ToTable("Addresses", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.FoodEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("IX_Foods_Name");

                    b.ToTable("Foods", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.FoodRelationshipEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<int>("ChildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ChildId");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<int>("ParentId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ParentId");

                    b.Property<double>("Quantity")
                        .HasColumnType("REAL")
                        .HasColumnName("Quantity");

                    b.HasKey("Id");

                    b.HasIndex("ChildId")
                        .HasDatabaseName("IX_FoodRelationships_ChildId");

                    b.HasIndex("ParentId")
                        .HasDatabaseName("IX_FoodRelationships_ParentId");

                    b.ToTable("FoodRelationships", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.GroupEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT")
                        .HasColumnName("Description");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.HasKey("Id");

                    b.ToTable("Groups", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.GroupMemberEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<int>("GroupId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("GroupId");

                    b.Property<int>("MemberId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("MemberId");

                    b.Property<Guid>("MemberSyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("MemberSyncId");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT")
                        .HasColumnName("Role");

                    b.HasKey("Id");

                    b.HasIndex("GroupId")
                        .HasDatabaseName("IX_GroupMembers_GroupId");

                    b.HasIndex("MemberId")
                        .HasDatabaseName("IX_GroupMembers_MemberId");

                    b.HasIndex("MemberSyncId")
                        .HasDatabaseName("IX_GroupMembers_MemberSyncId");

                    b.ToTable("GroupMembers", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.LogEventEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(250)
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("AcknowledgedOn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Level")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LoggedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("SyncId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("LogEvents", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.PetEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.Property<int>("OwnerId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("OwnerId");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<string>("TypeId")
                        .HasMaxLength(25)
                        .HasColumnType("TEXT")
                        .HasColumnName("TypeId");

                    b.HasKey("Name", "OwnerId");

                    b.HasIndex("OwnerId")
                        .HasDatabaseName("IX_Pets_OwnerId");

                    b.HasIndex("TypeId")
                        .HasDatabaseName("IX_Pets_TypeId");

                    b.HasIndex("Name", "OwnerId")
                        .IsUnique();

                    b.ToTable("Pets", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.PetTypeEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(25)
                        .HasColumnType("TEXT")
                        .HasColumnName("PetTypeId");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<string>("Type")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT")
                        .HasColumnName("Type");

                    b.HasKey("Id");

                    b.ToTable("PetType", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.SettingEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("CreatedOn");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER")
                        .HasColumnName("IsDeleted");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("ModifiedOn");

                    b.Property<string>("ServerName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.Property<string>("ServerValue")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("Value");

                    b.Property<Guid>("SyncId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("SyncId");

                    b.HasKey("Id");

                    b.HasIndex("ServerName")
                        .IsUnique()
                        .HasDatabaseName("IX_Settings_Name");

                    b.HasIndex("SyncId")
                        .IsUnique()
                        .HasDatabaseName("IX_Settings_SyncId");

                    b.ToTable("Settings", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.TrackerPathConfigurationEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CompletedOnName")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("DataName")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name01")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name02")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name03")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name04")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name05")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name06")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name07")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name08")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name09")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("PathName")
                        .IsRequired()
                        .HasMaxLength(896)
                        .HasColumnType("TEXT");

                    b.Property<string>("PathType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StartedOnName")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("SyncId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Type01")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type02")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type03")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type04")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type05")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type06")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type07")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type08")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type09")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SyncId")
                        .IsUnique()
                        .HasDatabaseName("IX_TrackerPathConfigurations_SyncId");

                    b.ToTable("TrackerPathConfigurations", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.TrackerPathEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CompletedOn")
                        .HasColumnType("datetime2");

                    b.Property<int>("ConfigurationId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Data")
                        .HasColumnType("TEXT");

                    b.Property<long>("ElapsedTicks")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2");

                    b.Property<long?>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("StartedOn")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("SyncId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Value01")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value02")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value03")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value04")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value05")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value06")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value07")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value08")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value09")
                        .HasMaxLength(900)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ConfigurationId");

                    b.HasIndex("ParentId");

                    b.HasIndex("SyncId")
                        .IsUnique()
                        .HasDatabaseName("IX_TrackerPaths_SyncId");

                    b.ToTable("TrackerPaths", "dbo");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.AccountEntity", b =>
                {
                    b.HasOne("Sample.Shared.Storage.Server.AddressEntity", "Address")
                        .WithMany("Accounts")
                        .HasForeignKey("AddressId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Address");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.AddressEntity", b =>
                {
                    b.HasOne("Sample.Shared.Storage.Server.AccountEntity", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Sample.Shared.Storage.Server.AddressEntity", "LinkedAddress")
                        .WithMany("LinkedAddresses")
                        .HasForeignKey("LinkedAddressId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Account");

                    b.Navigation("LinkedAddress");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.FoodRelationshipEntity", b =>
                {
                    b.HasOne("Sample.Shared.Storage.Server.FoodEntity", "Child")
                        .WithMany("ParentRelationships")
                        .HasForeignKey("ChildId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Sample.Shared.Storage.Server.FoodEntity", "Parent")
                        .WithMany("ChildRelationships")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Child");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.GroupMemberEntity", b =>
                {
                    b.HasOne("Sample.Shared.Storage.Server.GroupEntity", "Group")
                        .WithMany("Members")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Sample.Shared.Storage.Server.AccountEntity", "Member")
                        .WithMany("Groups")
                        .HasForeignKey("MemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Group");

                    b.Navigation("Member");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.PetEntity", b =>
                {
                    b.HasOne("Sample.Shared.Storage.Server.AccountEntity", "Owner")
                        .WithMany("Pets")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Sample.Shared.Storage.Server.PetTypeEntity", "Type")
                        .WithMany("Types")
                        .HasForeignKey("TypeId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Owner");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.TrackerPathEntity", b =>
                {
                    b.HasOne("Sample.Shared.Storage.Server.TrackerPathConfigurationEntity", "Configuration")
                        .WithMany("Paths")
                        .HasForeignKey("ConfigurationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Sample.Shared.Storage.Server.TrackerPathEntity", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Configuration");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.AccountEntity", b =>
                {
                    b.Navigation("Groups");

                    b.Navigation("Pets");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.AddressEntity", b =>
                {
                    b.Navigation("Accounts");

                    b.Navigation("LinkedAddresses");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.FoodEntity", b =>
                {
                    b.Navigation("ChildRelationships");

                    b.Navigation("ParentRelationships");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.GroupEntity", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.PetTypeEntity", b =>
                {
                    b.Navigation("Types");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.TrackerPathConfigurationEntity", b =>
                {
                    b.Navigation("Paths");
                });

            modelBuilder.Entity("Sample.Shared.Storage.Server.TrackerPathEntity", b =>
                {
                    b.Navigation("Children");
                });
#pragma warning restore 612, 618
        }
    }
}

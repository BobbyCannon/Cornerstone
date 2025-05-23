﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Server.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Foods",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEvents",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", maxLength: 250, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AcknowledgedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    LoggedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PetType",
                schema: "dbo",
                columns: table => new
                {
                    PetTypeId = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetType", x => x.PetTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackerPathConfigurations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompletedOnName = table.Column<string>(type: "TEXT", nullable: true),
                    DataName = table.Column<string>(type: "TEXT", nullable: true),
                    Name01 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name02 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name03 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name04 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name05 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name06 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name07 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name08 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Name09 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    PathName = table.Column<string>(type: "TEXT", maxLength: 896, nullable: false),
                    PathType = table.Column<string>(type: "TEXT", nullable: false),
                    IsException = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedOnName = table.Column<string>(type: "TEXT", nullable: true),
                    Type01 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type02 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type03 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type04 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type05 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type06 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type07 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type08 = table.Column<int>(type: "INTEGER", nullable: false),
                    Type09 = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerPathConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodRelationships",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodRelationships_Foods_ChildId",
                        column: x => x.ChildId,
                        principalSchema: "dbo",
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoodRelationships_Foods_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "dbo",
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrackerPaths",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    ElapsedTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    IsException = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<long>(type: "INTEGER", nullable: true),
                    StartedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value01 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value02 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value03 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value04 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value05 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value06 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value07 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value08 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    Value09 = table.Column<string>(type: "TEXT", maxLength: 900, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerPaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerPaths_TrackerPathConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalSchema: "dbo",
                        principalTable: "TrackerPathConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackerPaths_TrackerPaths_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "dbo",
                        principalTable: "TrackerPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "dbo",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountAddressId = table.Column<long>(type: "INTEGER", nullable: true),
                    AccountAddressSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountEmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AccountExternalId = table.Column<string>(type: "TEXT", nullable: true),
                    AccountLastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AccountNickname = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AccountPasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    AccountRoles = table.Column<string>(type: "TEXT", nullable: true),
                    AccountCreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountIsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccountModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                schema: "dbo",
                columns: table => new
                {
                    AddressId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    AddressAccountSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddressCity = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AddressLineOne = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AddressLineTwo = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AddressLinkedAddressId = table.Column<long>(type: "INTEGER", nullable: true),
                    AddressLinkedAddressSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddressPostal = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AddressState = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AddressCreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddressIsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddressModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddressSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_Addresses_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "dbo",
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Addresses_AddressLinkedAddressId",
                        column: x => x.AddressLinkedAddressId,
                        principalSchema: "dbo",
                        principalTable: "Addresses",
                        principalColumn: "AddressId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Accounts_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "dbo",
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "dbo",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pets",
                schema: "dbo",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TypeId = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => new { x.Name, x.OwnerId });
                    table.ForeignKey(
                        name: "FK_Pets_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "dbo",
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pets_PetType_TypeId",
                        column: x => x.TypeId,
                        principalSchema: "dbo",
                        principalTable: "PetType",
                        principalColumn: "PetTypeId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AddressId",
                schema: "dbo",
                table: "Accounts",
                column: "AccountAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AddressId_ExternalId",
                schema: "dbo",
                table: "Accounts",
                columns: new[] { "AccountAddressId", "AccountExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Name",
                schema: "dbo",
                table: "Accounts",
                column: "AccountName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Nickname",
                schema: "dbo",
                table: "Accounts",
                column: "AccountNickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SyncId",
                schema: "dbo",
                table: "Accounts",
                column: "AccountSyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Address_LinkedAddressId",
                schema: "dbo",
                table: "Addresses",
                column: "AddressLinkedAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_SyncId",
                schema: "dbo",
                table: "Addresses",
                column: "AddressSyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_AccountId",
                schema: "dbo",
                table: "Addresses",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRelationships_ChildId",
                schema: "dbo",
                table: "FoodRelationships",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRelationships_ParentId",
                schema: "dbo",
                table: "FoodRelationships",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_Name",
                schema: "dbo",
                table: "Foods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId",
                schema: "dbo",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberId",
                schema: "dbo",
                table: "GroupMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberSyncId",
                schema: "dbo",
                table: "GroupMembers",
                column: "MemberSyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_Name_OwnerId",
                schema: "dbo",
                table: "Pets",
                columns: new[] { "Name", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pets_OwnerId",
                schema: "dbo",
                table: "Pets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_TypeId",
                schema: "dbo",
                table: "Pets",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Name",
                schema: "dbo",
                table: "Settings",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_SyncId",
                schema: "dbo",
                table: "Settings",
                column: "SyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackerPathConfigurations_SyncId",
                schema: "dbo",
                table: "TrackerPathConfigurations",
                column: "SyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackerPaths_ConfigurationId",
                schema: "dbo",
                table: "TrackerPaths",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerPaths_ParentId",
                schema: "dbo",
                table: "TrackerPaths",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerPaths_SyncId",
                schema: "dbo",
                table: "TrackerPaths",
                column: "SyncId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Addresses_AccountAddressId",
                schema: "dbo",
                table: "Accounts",
                column: "AccountAddressId",
                principalSchema: "dbo",
                principalTable: "Addresses",
                principalColumn: "AddressId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Addresses_AccountAddressId",
                schema: "dbo",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "FoodRelationships",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "GroupMembers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LogEvents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pets",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TrackerPaths",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Foods",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PetType",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TrackerPathConfigurations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Addresses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "dbo");
        }
    }
}

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Client.Data.Sql.Migrations
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
                name: "Addresses",
                schema: "dbo",
                columns: table => new
                {
                    AddressId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddressCity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLastClientUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddressLineOne = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AddressLineTwo = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AddressPostal = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    AddressState = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    AddressCreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddressIsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    AddressModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddressSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.AddressId);
                });

            migrationBuilder.CreateTable(
                name: "LogEvents",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastClientUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastClientUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "dbo",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountAddressId = table.Column<long>(type: "bigint", nullable: true),
                    AccountAddressSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountEmailAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccountLastClientUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountCreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountIsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    AccountModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountSyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Accounts_Addresses_AccountAddressId",
                        column: x => x.AccountAddressId,
                        principalSchema: "dbo",
                        principalTable: "Addresses",
                        principalColumn: "AddressId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountAddressId",
                schema: "dbo",
                table: "Accounts",
                column: "AccountAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LastClientUpdate",
                schema: "dbo",
                table: "Accounts",
                column: "AccountLastClientUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SyncId",
                schema: "dbo",
                table: "Accounts",
                column: "AccountSyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_LastClientUpdate",
                schema: "dbo",
                table: "Addresses",
                column: "AddressLastClientUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_SyncId",
                schema: "dbo",
                table: "Addresses",
                column: "AddressSyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogEvents_LastClientUpdate",
                schema: "dbo",
                table: "LogEvents",
                column: "LastClientUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_LogEvents_SyncId",
                schema: "dbo",
                table: "LogEvents",
                column: "SyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_LastClientUpdate",
                schema: "dbo",
                table: "Settings",
                column: "LastClientUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_SyncId",
                schema: "dbo",
                table: "Settings",
                column: "SyncId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LogEvents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Addresses",
                schema: "dbo");
        }
    }
}

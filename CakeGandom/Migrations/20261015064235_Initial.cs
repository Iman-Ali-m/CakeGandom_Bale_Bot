using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CakeGandom.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "MenuButtons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CallbackData = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsSuperOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuButtons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuButtons_MenuButtons_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuButtons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BaleUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FirstSeen = table.Column<long>(type: "bigint", nullable: false),
                    LastSeen = table.Column<long>(type: "bigint", nullable: false),
                    IsRegistered = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    IsSuperUser = table.Column<bool>(type: "bit", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TempData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "CakeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuButtonId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CakeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CakeItems_MenuButtons_MenuButtonId",
                        column: x => x.MenuButtonId,
                        principalTable: "MenuButtons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PhoneEncrypted = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    BaleUsernameContact = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RegistrationStep = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CakeMedias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CakeItemId = table.Column<int>(type: "int", nullable: false),
                    PhotoPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CakeMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CakeMedias_CakeItems_CakeItemId",
                        column: x => x.CakeItemId,
                        principalTable: "CakeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BotSettings",
                columns: new[] { "Key", "Value" },
                values: new object[,]
                {
                    { "contact_text", "برای ارتباط با ما به آیدی زیر پیام دهید:\n@Pyton_lerning" },
                    { "help_text", "راهنمای ربات:\nاز دکمه ثبت نام برای تکمیل پروفایل استفاده کنید\nاز منوی کیک می‌توانید کیک‌های موجود را ببینید\nدر صورت نیاز از دکمه ارتباط با ما استفاده کنید" }
                });

            migrationBuilder.InsertData(
                table: "MenuButtons",
                columns: new[] { "Id", "CallbackData", "CreatedAt", "IsActive", "IsSuperOnly", "Level", "ParentId", "Position", "Title" },
                values: new object[,]
                {
                    { 1, "menu_register", 0L, true, false, 1, null, 1, "ثبت نام" },
                    { 2, "menu_cakes", 0L, true, false, 1, null, 2, "منوی کیک" },
                    { 3, "menu_profile", 0L, true, false, 1, null, 3, "پروفایل" },
                    { 4, "menu_help", 0L, true, false, 1, null, 4, "راهنما" },
                    { 5, "menu_contact", 0L, true, false, 1, null, 5, "ارتباط با ما" },
                    { 6, "menu_user_panel", 0L, true, true, 1, null, 6, "⬇️⬇️⬇️پنل کاربری⬇️⬇️⬇️" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CakeItems_MenuButtonId_Position",
                table: "CakeItems",
                columns: new[] { "MenuButtonId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CakeMedias_CakeItemId",
                table: "CakeMedias",
                column: "CakeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuButtons_Level_ParentId_Position",
                table: "MenuButtons",
                columns: new[] { "Level", "ParentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuButtons_ParentId",
                table: "MenuButtons",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotSettings");

            migrationBuilder.DropTable(
                name: "CakeMedias");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "CakeItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MenuButtons");
        }
    }
}

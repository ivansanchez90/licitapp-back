using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicitApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Uid = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Zone = table.Column<string>(type: "text", nullable: false),
                    BusinessName = table.Column<string>(type: "text", nullable: true),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    PushToken = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stats_total_licitaciones = table.Column<int>(type: "integer", nullable: false),
                    stats_total_ofertas = table.Column<int>(type: "integer", nullable: false),
                    stats_total_cierres = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: true),
                    OfertaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Read = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructorId = table.Column<string>(type: "text", nullable: false),
                    ConstructorName = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DeliveryZone = table.Column<string>(type: "text", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WinningOfferId = table.Column<Guid>(type: "uuid", nullable: true),
                    OfertasCount = table.Column<int>(type: "integer", nullable: false),
                    CorralonesNotifiedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_solicitudes_users_ConstructorId",
                        column: x => x.ConstructorId,
                        principalTable: "users",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "materiales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_materiales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_materiales_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ofertas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorralonId = table.Column<string>(type: "text", nullable: false),
                    CorralonName = table.Column<string>(type: "text", nullable: false),
                    CorralonRating = table.Column<double>(type: "double precision", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShippingType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DeliveryHours = table.Column<int>(type: "integer", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsBestPrice = table.Column<bool>(type: "boolean", nullable: false),
                    IsFastDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ofertas_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ofertas_users_CorralonId",
                        column: x => x.CorralonId,
                        principalTable: "users",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_materiales_SolicitudId",
                table: "materiales",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_Read",
                table: "notifications",
                columns: new[] { "UserId", "Read" });

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_CorralonId",
                table: "ofertas",
                column: "CorralonId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_SolicitudId",
                table: "ofertas",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_SolicitudId_Status",
                table: "ofertas",
                columns: new[] { "SolicitudId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_ConstructorId",
                table: "solicitudes",
                column: "ConstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_CreatedAt",
                table: "solicitudes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_DeliveryZone_Status",
                table: "solicitudes",
                columns: new[] { "DeliveryZone", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_users_Zone",
                table: "users",
                column: "Zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "materiales");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "ofertas");

            migrationBuilder.DropTable(
                name: "solicitudes");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

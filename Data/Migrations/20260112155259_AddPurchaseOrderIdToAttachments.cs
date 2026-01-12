using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpProcure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderIdToAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "Attachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_PurchaseOrderId",
                table: "Attachments",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_PurchaseOrders_PurchaseOrderId",
                table: "Attachments",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_PurchaseOrders_PurchaseOrderId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_PurchaseOrderId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "Attachments");
        }
    }
}

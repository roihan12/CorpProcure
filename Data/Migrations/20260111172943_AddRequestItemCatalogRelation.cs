using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpProcure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestItemCatalogRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestItems_Items_ItemId",
                table: "RequestItems");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestItems_Items_ItemId",
                table: "RequestItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestItems_Items_ItemId",
                table: "RequestItems");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestItems_Items_ItemId",
                table: "RequestItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id");
        }
    }
}

namespace DealerDatabase.Data.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddDealerDirectorMergeNote : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MergeNote",
            table: "DealerDirectors",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MergeNote",
            table: "DealerDirectors");
    }
}

namespace DealerDatabase.Data.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Dealers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                LegalCompanyName = table.Column<string>(type: "TEXT", nullable: true),
                CompanyNumber = table.Column<string>(type: "TEXT", nullable: true),
                IncorporationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompanyStatus = table.Column<string>(type: "TEXT", nullable: true),
                RegisteredAddressLine1 = table.Column<string>(type: "TEXT", nullable: true),
                RegisteredAddressLine2 = table.Column<string>(type: "TEXT", nullable: true),
                RegisteredAddressLine3 = table.Column<string>(type: "TEXT", nullable: true),
                RegisteredCity = table.Column<string>(type: "TEXT", nullable: true),
                RegisteredCounty = table.Column<string>(type: "TEXT", nullable: true),
                RegisteredPostcode = table.Column<string>(type: "TEXT", nullable: true),
                TradingAddressLine1 = table.Column<string>(type: "TEXT", nullable: true),
                TradingAddressLine2 = table.Column<string>(type: "TEXT", nullable: true),
                TradingAddressLine3 = table.Column<string>(type: "TEXT", nullable: true),
                TradingCity = table.Column<string>(type: "TEXT", nullable: true),
                TradingCounty = table.Column<string>(type: "TEXT", nullable: true),
                TradingPostcode = table.Column<string>(type: "TEXT", nullable: true),
                PrimaryPhone = table.Column<string>(type: "TEXT", nullable: true),
                PrimaryEmail = table.Column<string>(type: "TEXT", nullable: true),
                PrimaryWebsite = table.Column<string>(type: "TEXT", nullable: true),
                FcaReferenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                FcaStatus = table.Column<string>(type: "TEXT", nullable: true),
                IcoRegistrationNumber = table.Column<string>(type: "TEXT", nullable: true),
                IcoExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                SafStatus = table.Column<string>(type: "TEXT", nullable: true),
                SafExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                VatNumber = table.Column<string>(type: "TEXT", nullable: true),
                VatValidationStatus = table.Column<string>(type: "TEXT", nullable: true),
                FinanceCalculator = table.Column<string>(type: "TEXT", nullable: true),
                FinanceLendersJson = table.Column<string>(type: "TEXT", nullable: true),
                RepresentativeApr = table.Column<string>(type: "TEXT", nullable: true),
                InventoryCount = table.Column<int>(type: "INTEGER", nullable: true),
                AvgListedPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                AvgSoldPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                AvgDaysInStock = table.Column<int>(type: "INTEGER", nullable: true),
                SoldLast30Days = table.Column<int>(type: "INTEGER", nullable: true),
                VehicleTypes = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Dealers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DealerSourceRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                SourceType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                SourceKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                RawDataJson = table.Column<string>(type: "TEXT", nullable: false),
                MatchConfidence = table.Column<double>(type: "REAL", nullable: false),
                MatchEvidenceJson = table.Column<string>(type: "TEXT", nullable: false),
                ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DealerSourceRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_DealerSourceRecords_Dealers_DealerId",
                    column: x => x.DealerId,
                    principalTable: "Dealers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DealerFieldSources",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                SourceRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DealerFieldSources", x => x.Id);
                table.ForeignKey(
                    name: "FK_DealerFieldSources_Dealers_DealerId",
                    column: x => x.DealerId,
                    principalTable: "Dealers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DealerFieldSources_DealerSourceRecords_SourceRecordId",
                    column: x => x.SourceRecordId,
                    principalTable: "DealerSourceRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DealerTradingNames",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                SourceRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DealerTradingNames", x => x.Id);
                table.ForeignKey(
                    name: "FK_DealerTradingNames_Dealers_DealerId",
                    column: x => x.DealerId,
                    principalTable: "Dealers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DealerTradingNames_DealerSourceRecords_SourceRecordId",
                    column: x => x.SourceRecordId,
                    principalTable: "DealerSourceRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DealerDirectors",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                SourceRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                Role = table.Column<string>(type: "TEXT", nullable: true),
                Occupation = table.Column<string>(type: "TEXT", nullable: true),
                Nationality = table.Column<string>(type: "TEXT", nullable: true),
                AppointedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                ResignedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DealerDirectors", x => x.Id);
                table.ForeignKey(
                    name: "FK_DealerDirectors_Dealers_DealerId",
                    column: x => x.DealerId,
                    principalTable: "Dealers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DealerDirectors_DealerSourceRecords_SourceRecordId",
                    column: x => x.SourceRecordId,
                    principalTable: "DealerSourceRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DealerDirectors_DealerId_Name_Role",
            table: "DealerDirectors",
            columns: new[] { "DealerId", "Name", "Role" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DealerDirectors_SourceRecordId",
            table: "DealerDirectors",
            column: "SourceRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_DealerFieldSources_DealerId_FieldName_SourceRecordId",
            table: "DealerFieldSources",
            columns: new[] { "DealerId", "FieldName", "SourceRecordId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DealerFieldSources_SourceRecordId",
            table: "DealerFieldSources",
            column: "SourceRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_Dealers_CompanyNumber",
            table: "Dealers",
            column: "CompanyNumber");

        migrationBuilder.CreateIndex(
            name: "IX_Dealers_FcaReferenceNumber",
            table: "Dealers",
            column: "FcaReferenceNumber");

        migrationBuilder.CreateIndex(
            name: "IX_Dealers_RegisteredPostcode",
            table: "Dealers",
            column: "RegisteredPostcode");

        migrationBuilder.CreateIndex(
            name: "IX_Dealers_TradingPostcode",
            table: "Dealers",
            column: "TradingPostcode");

        migrationBuilder.CreateIndex(
            name: "IX_Dealers_VatNumber",
            table: "Dealers",
            column: "VatNumber");

        migrationBuilder.CreateIndex(
            name: "IX_DealerSourceRecords_DealerId",
            table: "DealerSourceRecords",
            column: "DealerId");

        migrationBuilder.CreateIndex(
            name: "IX_DealerSourceRecords_SourceType_SourceKey",
            table: "DealerSourceRecords",
            columns: new[] { "SourceType", "SourceKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DealerTradingNames_DealerId_SourceRecordId_Name",
            table: "DealerTradingNames",
            columns: new[] { "DealerId", "SourceRecordId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DealerTradingNames_SourceRecordId",
            table: "DealerTradingNames",
            column: "SourceRecordId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DealerDirectors");

        migrationBuilder.DropTable(
            name: "DealerFieldSources");

        migrationBuilder.DropTable(
            name: "DealerTradingNames");

        migrationBuilder.DropTable(
            name: "DealerSourceRecords");

        migrationBuilder.DropTable(
            name: "Dealers");
    }
}

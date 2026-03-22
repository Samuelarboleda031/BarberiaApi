using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberiaApi.Migrations
{
    /// <inheritdoc />
    public partial class Saldo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Ventas','SaldoAFavorUsado') IS NULL
BEGIN
    ALTER TABLE dbo.Ventas 
    ADD SaldoAFavorUsado DECIMAL(18,2) NULL CONSTRAINT DF_Ventas_SaldoAFavorUsado DEFAULT(0);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Ventas','SaldoAFavorUsado') IS NOT NULL
BEGIN
    DECLARE @ConstraintName NVARCHAR(256);
    SELECT @ConstraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Ventas') AND c.name = 'SaldoAFavorUsado';

    IF @ConstraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.Ventas DROP CONSTRAINT ' + QUOTENAME(@ConstraintName));

    ALTER TABLE dbo.Ventas DROP COLUMN SaldoAFavorUsado;
END
");
        }
    }
}

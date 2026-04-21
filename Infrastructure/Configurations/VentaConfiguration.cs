using BarberiaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberiaApi.Infrastructure.Configurations;

/// <summary>
/// Configuración Fluent API de EF Core para la entidad Venta.
/// Centraliza restricciones de columna sin depender de DataAnnotations en la entidad.
/// </summary>
public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Total).HasPrecision(18, 2);
        builder.Property(v => v.Subtotal).HasPrecision(18, 2);
        builder.Property(v => v.IVA).HasPrecision(18, 2);
        builder.Property(v => v.Descuento).HasPrecision(18, 2);
        builder.Property(v => v.SaldoAFavorUsado).HasPrecision(18, 2);
        builder.Property(v => v.Estado).HasMaxLength(50);
        builder.Property(v => v.MetodoPago).HasMaxLength(50);
        builder.Property(v => v.TipoVenta).HasMaxLength(50);
    }
}

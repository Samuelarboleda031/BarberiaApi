using BarberiaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberiaApi.Infrastructure.Configurations;

/// <summary>
/// Configuración Fluent API de EF Core para la entidad Cliente.
/// </summary>
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Telefono).HasMaxLength(20);
        builder.Property(c => c.Direccion).HasMaxLength(200);
        builder.Property(c => c.Barrio).HasMaxLength(100);

        builder.HasOne(c => c.Usuario)
               .WithMany()
               .HasForeignKey(c => c.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

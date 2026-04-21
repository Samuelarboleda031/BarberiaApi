using BarberiaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberiaApi.Infrastructure.Configurations;

/// <summary>
/// Configuración Fluent API de EF Core para la entidad Barbero.
/// </summary>
public class BarberoConfiguration : IEntityTypeConfiguration<Barbero>
{
    public void Configure(EntityTypeBuilder<Barbero> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Especialidad).HasMaxLength(100).HasDefaultValue("General");
        builder.Property(b => b.Telefono).HasMaxLength(20);
        builder.Property(b => b.Direccion).HasMaxLength(200);
        builder.Property(b => b.Barrio).HasMaxLength(100);

        builder.HasOne(b => b.Usuario)
               .WithMany()
               .HasForeignKey(b => b.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

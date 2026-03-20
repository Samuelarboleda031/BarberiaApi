using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Infrastructure.Data;

public partial class BarberiaContext : DbContext
{
    public BarberiaContext()
    {
    }

    public BarberiaContext(DbContextOptions<BarberiaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agendamiento> Agendamientos { get; set; }
    public virtual DbSet<AgendamientoProducto> AgendamientoProductos { get; set; }
    public virtual DbSet<AgendamientoServicio> AgendamientoServicios { get; set; }
    public virtual DbSet<Barbero> Barberos { get; set; }
    public virtual DbSet<Categoria> Categorias { get; set; }
    public virtual DbSet<Cliente> Clientes { get; set; }
    public virtual DbSet<Compra> Compras { get; set; }
    public virtual DbSet<DetalleCompra> DetalleCompras { get; set; }
    public virtual DbSet<DetalleVenta> DetalleVentas { get; set; }
    public virtual DbSet<Devolucion> Devoluciones { get; set; }
    public virtual DbSet<EntregasInsumo> EntregasInsumos { get; set; }
    public virtual DbSet<DetalleEntregasInsumo> DetalleEntregasInsumos { get; set; }
    public virtual DbSet<HorariosBarbero> HorariosBarberos { get; set; }
    public virtual DbSet<Modulos> Modulos { get; set; }
    public virtual DbSet<Paquete> Paquetes { get; set; }
    public virtual DbSet<DetallePaquete> DetallePaquetes { get; set; }
    public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<Proveedor> Proveedores { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<RolesModulos> RolesModulos { get; set; }
    public virtual DbSet<Servicio> Servicios { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Venta> Ventas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agendamiento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Agendamientos");

            entity.Property(e => e.FechaHora).HasColumnType("datetime");
            entity.Property(e => e.Estado).HasMaxLength(50).HasDefaultValue("Pendiente");
            entity.Property(e => e.Notas).IsUnicode(false);
            entity.Property(e => e.Precio).HasPrecision(10, 2);

            entity.HasOne(d => d.Barbero).WithMany(p => p.Agendamientos).HasForeignKey(d => d.BarberoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Cliente).WithMany(p => p.Agendamientos).HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Servicio).WithMany(p => p.Agendamientos).HasForeignKey(d => d.ServicioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Paquete).WithMany(p => p.Agendamientos).HasForeignKey(d => d.PaqueteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgendamientoProducto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AgendamientoProductos");

            entity.HasOne(d => d.Agendamiento)
                .WithMany(p => p.AgendamientoProductos)
                .HasForeignKey(d => d.AgendamientoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgendamientoServicio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AgendamientoServicios");

            entity.HasOne(d => d.Agendamiento)
                .WithMany(p => p.AgendamientoServicios)
                .HasForeignKey(d => d.AgendamientoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Servicio)
                .WithMany()
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Barbero>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Barberos");

            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.Especialidad).HasMaxLength(100);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaContratacion).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UsuarioId).IsRequired();

            entity.HasOne(b => b.Usuario)
                .WithOne(u => u.Barbero)
                .HasForeignKey<Barbero>(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Categorias");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Estado).HasDefaultValue(true);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Clientes");

            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.FechaNacimiento).HasColumnType("date");
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UsuarioId).IsRequired();

            entity.HasOne(c => c.Usuario)
                .WithOne(u => u.Cliente)
                .HasForeignKey<Cliente>(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Compras");

            entity.Property(e => e.NumeroFactura).HasMaxLength(50);
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.FechaFactura).HasColumnType("date");
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.IVA).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Descuento).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.MetodoPago).HasMaxLength(50);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Completada");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.Compras).HasForeignKey(d => d.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Usuario).WithMany(p => p.Compras).HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DetalleCompras");

            entity.Property(e => e.CantidadVentas).HasDefaultValue(0).IsRequired();
            entity.Property(e => e.CantidadInsumos).HasDefaultValue(0).IsRequired();
            entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2).HasComputedColumnSql("([Cantidad]*[PrecioUnitario])");

            entity.HasOne(d => d.Compra).WithMany(p => p.DetalleCompras).HasForeignKey(d => d.CompraId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Producto).WithMany(p => p.DetalleCompras).HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DetalleVentas");

            entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2).HasComputedColumnSql("([Cantidad]*[PrecioUnitario])");

            entity.HasOne(d => d.Venta).WithMany(p => p.DetalleVenta).HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Producto).WithMany(p => p.DetalleVenta).HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Servicio).WithMany(p => p.DetalleVenta).HasForeignKey(d => d.ServicioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Paquete).WithMany(p => p.DetalleVenta).HasForeignKey(d => d.PaqueteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Devolucion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Devoluciones");

            entity.Property(e => e.MotivoCategoria).HasMaxLength(100);
            entity.Property(e => e.MotivoDetalle).HasMaxLength(250);
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.MontoDevuelto).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Activo");
            entity.Property(e => e.SaldoAFavor).HasPrecision(18, 2).HasDefaultValue(0m);

            entity.HasOne(d => d.Venta).WithMany(p => p.Devoluciones).HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Usuario).WithMany(p => p.Devoluciones).HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Producto).WithMany(p => p.Devoluciones).HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Cliente).WithMany(p => p.Devoluciones).HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Barbero).WithMany().HasForeignKey(d => d.BarberoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Entrega).WithMany(p => p.Devoluciones).HasForeignKey(d => d.EntregaId).OnDelete(DeleteBehavior.Restrict);

            entity.HasCheckConstraint("CK_Devolucion_Tipo",
                "((VentaId IS NOT NULL AND BarberoId IS NULL AND EntregaId IS NULL AND ClienteId IS NOT NULL) " +
                "OR (VentaId IS NULL AND BarberoId IS NOT NULL AND EntregaId IS NOT NULL AND ClienteId IS NULL))");
        });

        modelBuilder.Entity<EntregasInsumo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EntregasInsumos");

            entity.Property(e => e.Fecha).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ValorTotal).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Entregado");

            entity.HasOne(d => d.Barbero).WithMany(p => p.EntregasInsumos).HasForeignKey(d => d.BarberoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Usuario).WithMany(p => p.EntregasInsumos).HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleEntregasInsumo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DetalleEntregasInsumos");

            entity.Property(e => e.PrecioHistorico).HasPrecision(18, 2);

            entity.HasOne(d => d.Entrega).WithMany(p => p.DetalleEntregasInsumos).HasForeignKey(d => d.EntregaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Producto).WithMany(p => p.DetalleEntregasInsumos).HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HorariosBarbero>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("HorariosBarberos");

            entity.Property(e => e.HoraInicio).HasColumnType("time");
            entity.Property(e => e.HoraFin).HasColumnType("time");
            entity.Property(e => e.Estado).HasDefaultValue(true);

            entity.HasOne(d => d.Barbero).WithMany(p => p.Horarios).HasForeignKey(d => d.BarberoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Modulos>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Modulos");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Estado).HasDefaultValue(true);
        });

        modelBuilder.Entity<Paquete>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Paquetes");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Precio).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasDefaultValue(true);
        });

        modelBuilder.Entity<DetallePaquete>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DetallePaquetes");

            entity.HasOne(d => d.Paquete).WithMany(p => p.DetallePaquetes).HasForeignKey(d => d.PaqueteId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Servicio).WithMany(p => p.DetallePaquetes).HasForeignKey(d => d.ServicioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Producto).WithMany(p => p.DetallePaquetes).HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Productos");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Marca).HasMaxLength(100);
            entity.Property(e => e.PrecioVenta).HasPrecision(18, 2);
            entity.Property(e => e.PrecioCompra).HasPrecision(18, 2);
            entity.Property(e => e.StockVentas).HasDefaultValue(0).IsRequired();
            entity.Property(e => e.StockInsumos).HasDefaultValue(0).IsRequired();
            entity.Property(e => e.StockTotal).HasDefaultValue(0).IsRequired();
            entity.Property(e => e.Estado).HasDefaultValue(true);

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos).HasForeignKey(d => d.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Proveedores");

            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.NIT).HasMaxLength(50);
            entity.HasIndex(e => e.NIT).IsUnique();
            entity.Property(e => e.Contacto).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            entity.Property(e => e.Correo).HasMaxLength(150);
            entity.Property(e => e.Direccion).HasMaxLength(200);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.TipoProveedor).HasMaxLength(20);
            entity.Property(e => e.NumeroIdentificacion).HasMaxLength(50);
            entity.Property(e => e.TipoIdentificacion).HasMaxLength(20);
            entity.Property(e => e.CorreoContacto).HasMaxLength(150);
            entity.Property(e => e.TelefonoContacto).HasMaxLength(50);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Roles");

            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Estado).HasDefaultValue(true);
        });

        modelBuilder.Entity<RolesModulos>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("RolesModulos");

            entity.Property(e => e.PuedeVer).HasDefaultValue(true);
            entity.Property(e => e.PuedeCrear).HasDefaultValue(false);
            entity.Property(e => e.PuedeEditar).HasDefaultValue(false);
            entity.Property(e => e.PuedeEliminar).HasDefaultValue(false);

            entity.HasOne(d => d.Rol).WithMany(p => p.RolesModulos).HasForeignKey(d => d.RolId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Modulo).WithMany(p => p.RolesModulos).HasForeignKey(d => d.ModuloId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Servicios");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Precio).HasPrecision(18, 2);
            entity.Property(e => e.DuracionMinutos).HasDefaultValue(30);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.Imagen).HasMaxLength(500);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Usuarios");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Correo).HasMaxLength(150);
            entity.HasIndex(e => e.Correo).IsUnique();
            entity.Property(e => e.TipoDocumento).HasMaxLength(50);
            entity.Property(e => e.Documento).HasMaxLength(50);
            entity.HasIndex(e => e.Documento).IsUnique();
            entity.Property(e => e.FotoPerfil).HasMaxLength(500);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios).HasForeignKey(d => d.RolId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Ventas");

            entity.Property(e => e.Fecha).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.IVA).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Descuento).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.MetodoPago).HasMaxLength(50).HasDefaultValue("Efectivo");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Completada");
            entity.Property(e => e.TipoVenta).HasMaxLength(50).HasDefaultValue("Venta Invitado");
            entity.Property(e => e.ClienteNombre).HasMaxLength(200);
            entity.Property(e => e.SaldoAFavorUsado).HasPrecision(18, 2).HasDefaultValue(0m);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Ventas).HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Cliente).WithMany(p => p.Venta).HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.Barbero).WithMany(p => p.Venta).HasForeignKey(d => d.BarberoId).OnDelete(DeleteBehavior.SetNull);
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

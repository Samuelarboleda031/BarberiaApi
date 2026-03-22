using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class ImageService : IImageService
{
    private readonly BarberiaContext _context;
    private readonly IPhotoService _photoService;

    public ImageService(BarberiaContext context, IPhotoService photoService)
    {
        _context = context;
        _photoService = photoService;
    }

    public async Task<ServiceResult<object>> SubirImagenAsync(IFormFile imagen, int? productoId, int? usuarioId)
    {
        if (imagen == null) return ServiceResult<object>.Fail("No se envió ninguna imagen");
        if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/"))
            return ServiceResult<object>.Fail("El Content-Type debe ser una imagen.");
        var result = await _photoService.AddPhotoAsync(imagen);
        if (result.Error != null) return ServiceResult<object>.Fail(result.Error.Message);
        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Fail("Error al subir la imagen");

        if (productoId.HasValue)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == productoId.Value);
            if (producto == null) return ServiceResult<object>.NotFound($"Producto {productoId.Value} no encontrado");
            producto.ImagenProduc = url; await _context.SaveChangesAsync();
        }
        else if (usuarioId.HasValue)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
            if (usuario == null) return ServiceResult<object>.NotFound($"Usuario {usuarioId.Value} no encontrado");
            usuario.FotoPerfil = url; usuario.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
        }
        return ServiceResult<object>.Ok(new { url, publicId = result.PublicId, productoId, usuarioId });
    }

    public async Task<ServiceResult<object>> EliminarImagenProductoAsync(int id, bool borrarCloud)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto == null) return ServiceResult<object>.NotFound();
        var url = producto.ImagenProduc;
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Ok(new { eliminado = false, mensaje = "El producto no tiene imagen" });
        string? publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) : null;
        if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
        producto.ImagenProduc = null; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { eliminado = true, publicId });
    }

    public async Task<ServiceResult<object>> EliminarImagenUsuarioAsync(int id, bool borrarCloud)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return ServiceResult<object>.NotFound();
        var url = usuario.FotoPerfil;
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Ok(new { eliminado = false, mensaje = "El usuario no tiene foto" });
        string? publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) : null;
        if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
        usuario.FotoPerfil = null; usuario.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { eliminado = true, publicId });
    }

    public async Task<ServiceResult<object>> SubirImagenProductoAsync(int id, IFormFile imagen)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto == null) return ServiceResult<object>.NotFound();
        if (imagen == null || imagen.Length == 0) return ServiceResult<object>.Fail("Imagen requerida");
        if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/")) return ServiceResult<object>.Fail("Content-Type inválido");
        var res = await _photoService.AddPhotoAsync(imagen);
        if (res.Error != null) return ServiceResult<object>.Fail(res.Error.Message);
        var url = res.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Fail("Error al subir");
        producto.ImagenProduc = url; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { url, publicId = res.PublicId });
    }

    public async Task<ServiceResult<object>> EliminarImagenProductoDirectaAsync(int id, bool borrarCloud)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto == null) return ServiceResult<object>.NotFound();
        var url = producto.ImagenProduc;
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Ok(new { eliminado = false });
        string publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) ?? "" : "";
        if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
        producto.ImagenProduc = null; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { eliminado = true, publicId });
    }

    public async Task<ServiceResult<object>> SubirImagenServicioAsync(int id, IFormFile imagen)
    {
        var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
        if (servicio == null) return ServiceResult<object>.NotFound();
        if (imagen == null || imagen.Length == 0) return ServiceResult<object>.Fail("Imagen requerida");
        if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/")) return ServiceResult<object>.Fail("Content-Type inválido");
        var res = await _photoService.AddPhotoAsync(imagen);
        if (res.Error != null) return ServiceResult<object>.Fail(res.Error.Message);
        var url = res.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Fail("Error al subir");
        servicio.Imagen = url; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { url, publicId = res.PublicId });
    }

    public async Task<ServiceResult<object>> EliminarImagenServicioAsync(int id, bool borrarCloud)
    {
        var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
        if (servicio == null) return ServiceResult<object>.NotFound();
        var url = servicio.Imagen;
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Ok(new { eliminado = false });
        string publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) ?? "" : "";
        if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
        servicio.Imagen = null; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { eliminado = true, publicId });
    }

    public async Task<ServiceResult<object>> SubirFotoUsuarioAsync(int id, IFormFile imagen)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return ServiceResult<object>.NotFound();
        if (imagen == null || imagen.Length == 0) return ServiceResult<object>.Fail("Imagen requerida");
        if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/")) return ServiceResult<object>.Fail("Content-Type inválido");
        var res = await _photoService.AddPhotoAsync(imagen);
        if (res.Error != null) return ServiceResult<object>.Fail(res.Error.Message);
        var url = res.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Fail("Error al subir");
        usuario.FotoPerfil = url; usuario.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { url, publicId = res.PublicId });
    }

    public async Task<ServiceResult<object>> EliminarFotoUsuarioAsync(int id, bool borrarCloud)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return ServiceResult<object>.NotFound();
        var url = usuario.FotoPerfil;
        if (string.IsNullOrWhiteSpace(url)) return ServiceResult<object>.Ok(new { eliminado = false });
        string publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) ?? "" : "";
        if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
        usuario.FotoPerfil = null; usuario.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { eliminado = true, publicId });
    }

    private static string? ExtraerPublicIdDesdeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var marker = "/upload/";
            var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var after = path[(idx + marker.Length)..].Trim('/');
            var segments = after.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return null;
            var start = 0;
            if (segments[0].Length > 1 && segments[0][0] == 'v' && long.TryParse(segments[0][1..], out _)) start = 1;
            if (start >= segments.Length) return null;
            var last = segments[^1];
            var nameNoExt = System.IO.Path.GetFileNameWithoutExtension(last);
            var leading = segments.Length - start > 1 ? string.Join('/', segments[start..^1]) + "/" : string.Empty;
            return leading + nameNoExt;
        }
        catch { return null; }
    }
}

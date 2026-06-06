using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace BarberiaApi.Infrastructure.Services
{
    // NOTA ARQUITECTÓNICA: Esta interfaz idealmente viviría en Application/Interfaces,
    // pero Application YA depende de Infrastructure (BarberiaContext, servicios), así que
    // mover IPhotoService a Application crearía una dependencia circular de proyectos
    // (Infrastructure -> Application -> Infrastructure). Resolverlo requiere primero
    // romper la dependencia Application -> Infrastructure (mover el DbContext / servicios),
    // un refactor mayor fuera del alcance actual. Se deja documentado como deuda (BE-A7).
    public interface IPhotoService
    {
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);
        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}

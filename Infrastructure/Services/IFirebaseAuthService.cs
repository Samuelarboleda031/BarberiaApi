namespace BarberiaApi.Infrastructure.Services
{
    // Abstracción para operar sobre Firebase Authentication desde la capa de aplicación
    // sin acoplarla al SDK. Mismo patrón que IPhotoService (ver nota arquitectónica BE-A7).
    public interface IFirebaseAuthService
    {
        /// <summary>
        /// Borra el usuario de Firebase Authentication identificándolo por su correo.
        /// Si el usuario no existe en Firebase (o el SDK no está inicializado) no lanza
        /// excepción: la operación es idempotente y no debe bloquear el borrado en SQL.
        /// </summary>
        /// <returns>true si se borró un usuario en Firebase; false si no había nada que borrar.</returns>
        Task<bool> DeleteUserByEmailAsync(string email);
    }
}

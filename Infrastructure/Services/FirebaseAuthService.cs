using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly ILogger<FirebaseAuthService> _logger;

        public FirebaseAuthService(ILogger<FirebaseAuthService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> DeleteUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Si Firebase Admin no se inicializó (ver Program.cs), no hay nada que hacer.
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("Firebase Admin no inicializado; se omite el borrado de {Email} en Firebase.", email);
                return false;
            }

            try
            {
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(userRecord.Uid);
                _logger.LogInformation("Usuario {Email} (uid {Uid}) borrado de Firebase Authentication.", email, userRecord.Uid);
                return true;
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                // El correo no existe en Firebase: nada que borrar. Operación idempotente.
                _logger.LogInformation("El correo {Email} no existe en Firebase; no hay nada que borrar.", email);
                return false;
            }
        }
    }
}

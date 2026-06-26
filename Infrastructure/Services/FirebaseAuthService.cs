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
        public async Task<bool> UpdateUserEmailAsync(string oldEmail, string newEmail)
        {
            if (string.IsNullOrWhiteSpace(oldEmail) || string.IsNullOrWhiteSpace(newEmail) || oldEmail == newEmail)
                return false;

            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("Firebase Admin no inicializado; se omite la actualización de correo en Firebase de {OldEmail} a {NewEmail}.", oldEmail, newEmail);
                return false;
            }

            try
            {
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(oldEmail);
                
                var args = new UserRecordArgs()
                {
                    Uid = userRecord.Uid,
                    Email = newEmail,
                    EmailVerified = false // Se requiere que verifique nuevamente si tu flujo lo exige
                };

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
                _logger.LogInformation("Correo de usuario {Uid} actualizado en Firebase de {OldEmail} a {NewEmail}.", userRecord.Uid, oldEmail, newEmail);
                return true;
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                _logger.LogWarning("El correo {OldEmail} no existe en Firebase; no se pudo actualizar a {NewEmail}.", oldEmail, newEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando el correo en Firebase de {OldEmail} a {NewEmail}.", oldEmail, newEmail);
                return false;
            }
        }
    }
}

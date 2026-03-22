using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace BarberiaApi.Authorization
{
    public class ValidPasswordRequirement : IAuthorizationRequirement { }

    public class ValidPasswordHandler : AuthorizationHandler<ValidPasswordRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ValidPasswordRequirement requirement)
        {
            // Si no está autenticado, dejamos que el [Authorize] normal lo rechace
            if (!context.User.Identity.IsAuthenticated) return Task.CompletedTask;

            // 1. Validar Cambio Obligatorio (Primer Login)
            var requiresChangeClaim = context.User.FindFirst("requiresPasswordChange")?.Value;
            if (requiresChangeClaim == "true" || requiresChangeClaim == "True")
            {
                context.Fail(new AuthorizationFailureReason(this, "Debe cambiar su contraseña temporal."));
                return Task.CompletedTask;
            }

            // 2. Validar Expiración (90 días)
            var updatedAtClaim = context.User.FindFirst("passwordUpdatedAt")?.Value;
            if (long.TryParse(updatedAtClaim, out long unixTime))
            {
                var lastUpdate = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                var daysPassed = (DateTimeOffset.UtcNow - lastUpdate).TotalDays;
                
                if (daysPassed > 90)
                {
                    context.Fail(new AuthorizationFailureReason(this, "Su contraseña ha expirado (más de 90 días)."));
                    return Task.CompletedTask;
                }
            }
            else
            {
                // Opcional: Si el token no tiene la fecha (usuarios muy viejos), rechazarlo
                context.Fail(new AuthorizationFailureReason(this, "Token no contiene fecha de actualización de contraseña."));
                return Task.CompletedTask; 
            }

            // 3. Si todo está en orden, permitimos el acceso
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}

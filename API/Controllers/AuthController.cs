using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("api/users")]
        [Authorize(Roles = "Admin")] // Solo administradores pueden crear usuarios
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto req)
        {
            // 1. Crear usuario en Firebase
            var args = new UserRecordArgs()
            {
                Email = req.Email,
                Password = "TemporaryPassword123!", // Enviar esto al email del usuario
                DisplayName = req.Name,
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

            // 2. Setear las propiedades de seguridad (Custom Claims)
            var claims = new Dictionary<string, object>()
            {
                // Obligar cambio en primer login
                { "requiresPasswordChange", true },
                // Registrar fecha exacta (Epoch/Unix Timestamp) de este momento
                { "passwordUpdatedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userRecord.Uid, claims);

            // 3. Opcional: Guardar en tu SQL de SQL Server
            // await _db.Clientes.AddAsync(new Cliente { AuthId = userRecord.Uid, ... });

            return Ok(new { Message = "User created", Uid = userRecord.Uid });
        }

        [HttpPost("api/auth/password-changed")]
        [Authorize] // Requiere que esté autenticado con su token (aunque sea temporal)
        public async Task<IActionResult> ConfirmPasswordChange()
        {
            // 1. Obtener el UID del token JWT actual
            var uid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(uid)) 
                return Unauthorized();

            // 2. Obtener claims actuales del usuario
            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            var claims = userRecord.CustomClaims?.ToDictionary(k => k.Key, v => v.Value) 
                         ?? new Dictionary<string, object>();

            // 3. Actualizar claims (quita la bandera, actualiza la fecha a HOY)
            claims["requiresPasswordChange"] = false;
            claims["passwordUpdatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);

            return Ok(new { Message = "Políticas de contraseña actualizadas." });
        }
    }
}

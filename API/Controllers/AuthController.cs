using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
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
    [EnableRateLimiting("auth")] // FE-A2 / Fase 4 — límite de 10 req/min por IP en endpoints de auth
    public class AuthController : ControllerBase
    {
        [HttpPost("api/users")]
        [Authorize(Roles = "Admin")] // Solo administradores pueden crear usuarios
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto req)
        {
            // 1. Crear usuario en Firebase
            // Generar contraseña temporal aleatoria y segura (no hardcodeada)
        var tempPassword = $"Tmp{Guid.NewGuid():N}"[..16] + "!2";

        var args = new UserRecordArgs()
            {
                Email = req.Email,
                Password = tempPassword, // Se envía por email al usuario; no se almacena aquí
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

        // FE-A3: verificación server-side del captcha (Cloudflare Turnstile).
        // El token generado por el widget del cliente se valida aquí contra Cloudflare
        // usando la secret key (Turnstile:SecretKey en User Secrets / variables de entorno).
        // Mientras no haya secret configurada, el endpoint responde success=true con
        // configured=false (andamiaje) para no bloquear el login en desarrollo.
        /// <summary>
        /// Asigna custom claims de rol a un usuario de Firebase.
        /// Solo puede ser llamado con una clave de admin configurada en appsettings.
        /// </summary>
        [HttpPost("api/auth/set-role-claims")]
        [AllowAnonymous]
        public async Task<IActionResult> SetRoleClaims(
            [FromBody] SetRoleClaimsDto req,
            [FromServices] IConfiguration config)
        {
            // Clave secreta para proteger este endpoint (configurar en User Secrets: AdminSetupKey)
            var adminKey = config["AdminSetupKey"];
            if (string.IsNullOrWhiteSpace(adminKey) || req.AdminKey != adminKey)
                return Unauthorized(new { error = "Clave de administración inválida." });

            if (string.IsNullOrWhiteSpace(req.Uid))
                return BadRequest(new { error = "UID requerido." });

            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(req.Uid);
            var claims = userRecord.CustomClaims?.ToDictionary(k => k.Key, v => v.Value)
                         ?? new Dictionary<string, object>();

            // Limpiar claims de rol anteriores
            claims.Remove("admin");
            claims.Remove("super_admin");
            claims.Remove("barbero");

            switch (req.Rol?.ToLower())
            {
                case "super_admin":
                    claims["super_admin"] = true;
                    claims["admin"] = true;
                    break;
                case "admin":
                    claims["admin"] = true;
                    break;
                case "barbero":
                    claims["barbero"] = true;
                    break;
                // cliente: sin claims especiales
            }

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(req.Uid, claims);
            return Ok(new { message = $"Claims actualizados para {userRecord.Email}", uid = req.Uid, claims });
        }

        [HttpPost("api/auth/verify-captcha")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCaptcha(
            [FromBody] CaptchaVerifyDto req,
            [FromServices] IHttpClientFactory httpFactory,
            [FromServices] IConfiguration config)
        {
            var secret = config["Turnstile:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                return Ok(new { success = true, configured = false });

            if (string.IsNullOrWhiteSpace(req?.Token))
                return BadRequest(new { success = false, error = "Token de captcha requerido" });

            var client = httpFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = req.Token
            });
            var resp = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", form);
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ok = doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }
    }
}

using System;
using System.Text.RegularExpressions;

namespace BarberiaApi.Application.Helpers
{
    public static class ValidationHelper
    {
        private static readonly Regex SoloLetrasYEspaciosRegex = new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]+$", RegexOptions.Compiled);

        public static bool ValidarSoloLetras(string? texto, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(texto)) return true;

            if (!SoloLetrasYEspaciosRegex.IsMatch(texto))
            {
                error = "Este campo solo permite letras y espacios, sin números ni caracteres especiales.";
                return false;
            }

            return true;
        }

        public static bool ValidarUrlImagen(string? url, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(url)) return true;

            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _))
            {
                error = "La URL de la imagen no es valida";
                return false;
            }

            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    error = "La URL absoluta de la imagen debe ser valida (http:// o https://)";
                    return false;
                }
            }
            else if (!url.StartsWith("/"))
            {
                error = "La URL relativa de la imagen debe comenzar con / (ej: /assets/images/imagen.jpg)";
                return false;
            }

            return true;
        }
    }
}

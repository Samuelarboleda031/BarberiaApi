using System;

namespace BarberiaApi.Helpers
{
    public static class ValidationHelper
    {
        public static bool ValidarUrlImagen(string? url, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(url)) return true;

            // Aceptar URLs relativas (como /assets/images/imagen.jpg) o URLs absolutas
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _))
            {
                error = "La URL de la imagen no es válida";
                return false;
            }

            // Si es URL absoluta, debe ser http/https
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    error = "La URL absoluta de la imagen debe ser válida (http:// o https://)";
                    return false;
                }
            }
            // Si es URL relativa, debe comenzar con / (estándar del sistema)
            else if (!url.StartsWith("/"))
            {
                error = "La URL relativa de la imagen debe comenzar con / (ej: /assets/images/imagen.jpg)";
                return false;
            }

            return true;
        }
    }
}

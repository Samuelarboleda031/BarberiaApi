using System;

namespace BarberiaApi.Application.Helpers
{
    public static class ValidationHelper
    {
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

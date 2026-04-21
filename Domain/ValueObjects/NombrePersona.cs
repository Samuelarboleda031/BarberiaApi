namespace BarberiaApi.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que representa el nombre completo de una persona.
/// Garantiza que Nombre y Apellido nunca sean null y expone
/// el nombre completo formateado como propiedad calculada.
/// </summary>
public record NombrePersona(string Nombre, string Apellido)
{
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public override string ToString() => NombreCompleto;
}

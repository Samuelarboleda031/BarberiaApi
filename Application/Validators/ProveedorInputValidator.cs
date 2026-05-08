using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="ProveedorCreateInput"/>.
/// Aplica reglas condicionales según TipoProveedor (Natural / Juridico) y validaciones de formato.
/// </summary>
public class ProveedorCreateInputValidator : AbstractValidator<ProveedorCreateInput>
{
    // Regex de formatos
    private const string NombreRegex = @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü .'\-&]{2,150}$";
    private const string TelefonoRegex = @"^[0-9+\-() ]{7,20}$";

    public ProveedorCreateInputValidator()
    {
        // Reglas comunes
        RuleFor(x => x.TipoProveedor)
            .NotEmpty().WithMessage("El tipo de proveedor es requerido")
            .Must(t => t == "Natural" || t == "Juridico")
            .WithMessage("TipoProveedor debe ser 'Natural' o 'Juridico'");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .Matches(NombreRegex).WithMessage("El nombre solo puede contener letras y espacios (2-150 caracteres)");

        RuleFor(x => x.Identificacion)
            .NotEmpty().WithMessage("La identificación es requerida")
            .Must((input, ident) => IdentificacionEsValida(ident, input.TipoIdentificacionProveedor))
            .WithMessage("La identificación no tiene un formato válido para el tipo seleccionado");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es requerido")
            .EmailAddress().WithMessage("El correo no tiene un formato válido")
            .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .Matches(TelefonoRegex).WithMessage("El teléfono solo puede contener números, +, -, espacios y paréntesis (7-20 caracteres)")
            .Must(t => CountDigits(t) >= 7 && CountDigits(t) <= 15)
            .WithMessage("El teléfono debe tener entre 7 y 15 dígitos");

        // Reglas exclusivas para Jurídico
        When(x => x.TipoProveedor == "Juridico", () =>
        {
            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección es requerida para proveedores jurídicos")
                .MaximumLength(200).WithMessage("La dirección no puede exceder 200 caracteres");

            RuleFor(x => x.Ciudad)
                .NotEmpty().WithMessage("La ciudad es requerida para proveedores jurídicos")
                .MaximumLength(100).WithMessage("La ciudad no puede exceder 100 caracteres");

            RuleFor(x => x.Departamento)
                .NotEmpty().WithMessage("El departamento es requerido para proveedores jurídicos")
                .MaximumLength(100).WithMessage("El departamento no puede exceder 100 caracteres");

            RuleFor(x => x.RepresentanteLegal)
                .NotEmpty().WithMessage("El representante legal es requerido para proveedores jurídicos")
                .Matches(NombreRegex).WithMessage("El nombre del representante solo puede contener letras y espacios (2-150 caracteres)");

            RuleFor(x => x.IdentificacionRepresentante)
                .NotEmpty().WithMessage("La identificación del representante es requerida para proveedores jurídicos")
                .Must((input, ident) => IdentificacionEsValida(ident, input.TipoIdentificacionRepresentante))
                .WithMessage("La identificación del representante no tiene un formato válido");

            RuleFor(x => x.CorreoRepresentante)
                .NotEmpty().WithMessage("El correo del representante es requerido para proveedores jurídicos")
                .EmailAddress().WithMessage("El correo del representante no tiene un formato válido")
                .MaximumLength(150).WithMessage("El correo del representante no puede exceder 150 caracteres");

            RuleFor(x => x.TelefonoRepresentante)
                .NotEmpty().WithMessage("El teléfono del representante es requerido para proveedores jurídicos")
                .Matches(TelefonoRegex).WithMessage("El teléfono del representante solo puede contener números, +, -, espacios y paréntesis")
                .Must(t => CountDigits(t) >= 7 && CountDigits(t) <= 15)
                .WithMessage("El teléfono del representante debe tener entre 7 y 15 dígitos");
        });
    }

    private static int CountDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var count = 0;
        foreach (var c in input) if (char.IsDigit(c)) count++;
        return count;
    }

    private static bool IdentificacionEsValida(string? identificacion, string? tipo)
    {
        if (string.IsNullOrWhiteSpace(identificacion)) return false;
        var val = identificacion.Trim();
        switch ((tipo ?? string.Empty).ToUpperInvariant())
        {
            case "NIT":
                return System.Text.RegularExpressions.Regex.IsMatch(val, @"^\d{9,10}(-\d)?$");
            case "PASAPORTE":
                return System.Text.RegularExpressions.Regex.IsMatch(val, @"^[A-Z0-9]{6,15}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            case "CC":
            case "CE":
                return System.Text.RegularExpressions.Regex.IsMatch(val, @"^\d{6,12}$");
            case "TI":
                return System.Text.RegularExpressions.Regex.IsMatch(val, @"^\d{8,11}$");
            default:
                return val.Length >= 6 && val.Length <= 20;
        }
    }
}

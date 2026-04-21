using FluentValidation;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

public class CategoriaValidator : AbstractValidator<Categoria>
{
    public CategoriaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre de la categoría es requerido");
    }
}

public class ServicioValidator : AbstractValidator<Servicio>
{
    public ServicioValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del servicio es requerido");
        RuleFor(x => x.Precio).GreaterThan(0).WithMessage("El precio debe ser mayor a 0");
    }
}

public class ProveedorValidator : AbstractValidator<Proveedor>
{
    public ProveedorValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del proveedor es requerido");
        RuleFor(x => x.NIT).NotEmpty().WithMessage("El NIT es requerido");
    }
}

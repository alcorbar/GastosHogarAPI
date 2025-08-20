using FluentValidation;
using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Validators
{
    public class CrearGastoRequestValidator : AbstractValidator<CrearGastoRequest>
    {
        public CrearGastoRequestValidator()
        {
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("ID de usuario inválido");

            RuleFor(x => x.GrupoId)
                .GreaterThan(0).WithMessage("ID de grupo inválido");

            RuleFor(x => x.Importe)
                .GreaterThan(0).WithMessage("El importe debe ser mayor que 0")
                .LessThanOrEqualTo(999999.99m).WithMessage("El importe es demasiado alto");

            RuleFor(x => x.CategoriaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría");

            RuleFor(x => x.Descripcion)
                .NotEmpty().WithMessage("La descripción es requerida")
                .Length(3, 500).WithMessage("La descripción debe tener entre 3 y 500 caracteres");

            RuleFor(x => x.Tienda)
                .MaximumLength(200).WithMessage("El nombre de la tienda es demasiado largo");

            RuleFor(x => x.Notas)
                .MaximumLength(500).WithMessage("Las notas son demasiado largas");
        }
    }
}
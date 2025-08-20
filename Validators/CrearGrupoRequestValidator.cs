using FluentValidation;
using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Validators
{
    public class CrearGrupoRequestValidator : AbstractValidator<CrearGrupoRequest>
    {
        public CrearGrupoRequestValidator()
        {
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("ID de usuario inválido");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del grupo es requerido")
                .Length(3, 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("El nombre solo puede contener letras, números, espacios, guiones y guiones bajos");

            RuleFor(x => x.Descripcion)
                .MaximumLength(500).WithMessage("La descripción es demasiado larga");
        }
    }
}
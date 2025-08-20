using FluentValidation;
using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Usuario)
                .NotEmpty().WithMessage("El nombre de usuario es requerido")
                .Length(2, 50).WithMessage("El usuario debe tener entre 2 y 50 caracteres");

            RuleFor(x => x.Pin)
                .NotEmpty().WithMessage("El PIN es requerido")
                .Length(4, 8).WithMessage("El PIN debe tener entre 4 y 8 caracteres")
                .Matches(@"^\d+$").WithMessage("El PIN solo puede contener números");
        }
    }
}
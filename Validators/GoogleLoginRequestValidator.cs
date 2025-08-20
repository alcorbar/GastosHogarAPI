using FluentValidation;
using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Validators
{
    public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
    {
        public GoogleLoginRequestValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty().WithMessage("El token de Google es requerido");

            RuleFor(x => x.DeviceId)
                .NotEmpty().WithMessage("ID del dispositivo requerido")
                .Length(10, 100).WithMessage("ID del dispositivo inválido");

            RuleFor(x => x.DeviceName)
                .NotEmpty().WithMessage("Nombre del dispositivo requerido")
                .Length(2, 100).WithMessage("Nombre del dispositivo inválido");
        }
    }
}

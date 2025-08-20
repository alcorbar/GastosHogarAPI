namespace GastosHogarAPI.Exceptions
{
    public class ForbiddenException : BusinessException
    {
        public override int StatusCode => 403;
        public override string ErrorCode => "FORBIDDEN";

        public ForbiddenException(string message = "Acceso denegado") : base(message) { }
    }
}
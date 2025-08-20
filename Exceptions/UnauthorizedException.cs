namespace GastosHogarAPI.Exceptions
{
    public class UnauthorizedException : BusinessException
    {
        public override int StatusCode => 401;
        public override string ErrorCode => "UNAUTHORIZED";

        public UnauthorizedException(string message = "No autorizado") : base(message) { }
    }
}
namespace GastosHogarAPI.Exceptions
{
    public class ConflictException : BusinessException
    {
        public override int StatusCode => 409;
        public override string ErrorCode => "CONFLICT";

        public ConflictException(string message) : base(message) { }
    }
}
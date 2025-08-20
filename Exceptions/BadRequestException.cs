namespace GastosHogarAPI.Exceptions
{
    public class BadRequestException : BusinessException
    {
        public override int StatusCode => 400;
        public override string ErrorCode => "BAD_REQUEST";

        public BadRequestException(string message) : base(message) { }
    }
}
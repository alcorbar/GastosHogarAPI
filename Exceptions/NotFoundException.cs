namespace GastosHogarAPI.Exceptions
{
    public class NotFoundException : BusinessException
    {
        public override int StatusCode => 404;
        public override string ErrorCode => "NOT_FOUND";

        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string resource, object key)
            : base($"{resource} con ID '{key}' no fue encontrado") { }
    }
}
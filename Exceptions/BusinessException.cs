namespace GastosHogarAPI.Exceptions
{
    public abstract class BusinessException : Exception
    {
        public abstract int StatusCode { get; }
        public virtual string ErrorCode { get; } = "BUSINESS_ERROR";

        protected BusinessException(string message) : base(message) { }
        protected BusinessException(string message, Exception innerException) : base(message, innerException) { }
    }
}
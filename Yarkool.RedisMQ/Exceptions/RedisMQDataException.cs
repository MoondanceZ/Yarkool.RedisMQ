namespace Yarkool.RedisMQ
{
    internal class RedisMQDataException : Exception
    {
        public RedisMQDataException(string message, object? exceptionData) : base(message)
        {
            SetExceptionData(exceptionData);
        }

        public RedisMQDataException(string message, object? exceptionData, Exception innerException) : base(message, innerException)
        {
            SetExceptionData(exceptionData);
        }

        private void SetExceptionData(object? obj)
        {
            if (obj != null)
                Data.Add("ExceptionData", obj);
        }
    }
}
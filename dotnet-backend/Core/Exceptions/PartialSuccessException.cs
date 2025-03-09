using System;

namespace Infrastructure.Exceptions {
    public class PartialSuccessException : Exception
    {
        public PartialSuccessException() : base("The query returned partial success.") { }
        public PartialSuccessException(string message) : base(message) { }
    }
}

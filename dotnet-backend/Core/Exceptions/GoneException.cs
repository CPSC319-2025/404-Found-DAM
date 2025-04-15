using System;

namespace Infrastructure.Exceptions {
    public class GoneException : Exception
    {
        public GoneException() : base("410 - GONE.") { }
        public GoneException(string message) : base(message) { }
    }
}
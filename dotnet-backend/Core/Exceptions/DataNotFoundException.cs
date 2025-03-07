using System;

namespace Infrastructure.Exceptions {
    public class DataNotFoundException : Exception
    {
        public DataNotFoundException() : base("The query returned no results.") { }
        public DataNotFoundException(string message) : base(message) { }
    }
}

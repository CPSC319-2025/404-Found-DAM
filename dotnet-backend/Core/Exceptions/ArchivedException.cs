using System;

namespace Infrastructure.Exceptions {
    public class ArchivedException : Exception
    {
        public ArchivedException() : base("Archived project available to admin only.") { }
        public ArchivedException(string message) : base(message) { }
    }
}

using System.Runtime.Serialization;
using Dson.IO;

namespace Dson.Text;

public class DsonParseException : DsonIOException
{
    public DsonParseException() {
    }

    protected DsonParseException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public DsonParseException(string? message) : base(message) {
    }

    public DsonParseException(string? message, Exception? innerException) : base(message, innerException) {
    }
}
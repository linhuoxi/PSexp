using System;

namespace PsExporter.Core.PsConnection;

public sealed class PsNotConnectedException : Exception
{
    public PsNotConnectedException(string message) : base(message) { }
    public PsNotConnectedException(string message, Exception inner) : base(message, inner) { }
}

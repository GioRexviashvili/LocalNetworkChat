namespace Lnc.Protocol;

public enum MessageType
{
    Discover,
    Handshake,
    HandshakeOk,
    HandshakeReject,
    Text,
    TextResponse,
    Close,
    Error
}

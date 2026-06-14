using System.Text;

namespace Lnc.Protocol;

public static class LncMessageSerializer
{
    private const string ProtocolVersion = "LNC/1.0";

    public static string Serialize(LncMessage message)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"{ProtocolVersion} {ToProtocolType(message.Type)}");

        foreach (var header in message.Headers)
        {
            builder.AppendLine($"{header.Key}: {header.Value}");
        }

        if (!string.IsNullOrEmpty(message.Body))
        {
            builder.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(message.Body)}");
        }

        builder.AppendLine();

        if (!string.IsNullOrEmpty(message.Body))
        {
            builder.Append(message.Body);
        }

        return builder.ToString();
    }

    private static string ToProtocolType(MessageType type)
    {
        return type switch
        {
            MessageType.Discover => "DISCOVER",
            MessageType.Handshake => "HANDSHAKE",
            MessageType.HandshakeOk => "HANDSHAKE_OK",
            MessageType.HandshakeReject => "HANDSHAKE_REJECT",
            MessageType.Text => "TEXT",
            MessageType.TextResponse => "TEXT_RESPONSE",
            MessageType.Close => "CLOSE",
            MessageType.Error => "ERROR",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
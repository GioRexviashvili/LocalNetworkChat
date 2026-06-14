namespace Lnc.Protocol;

public static class LncMessageParser
{
    private const string ProtocolVersion = "LNC/1.0";

    public static LncMessage Parse(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            throw new FormatException("Message is empty.");

        var parts = rawMessage.Split("\n\n", 2, StringSplitOptions.None);

        var headerLines = parts[0]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        if (headerLines.Count == 0)
            throw new FormatException("Message start line is missing.");

        var startLine = headerLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (startLine.Length != 2)
            throw new FormatException("Invalid start line format.");

        if (startLine[0] != ProtocolVersion)
            throw new FormatException("Unsupported protocol version.");

        var message = new LncMessage
        {
            Type = FromProtocolType(startLine[1])
        };

        for (var i = 1; i < headerLines.Count; i++)
        {
            var line = headerLines[i];
            var separatorIndex = line.IndexOf(':');

            if (separatorIndex <= 0)
                throw new FormatException($"Invalid header format: {line}");

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            message.Headers[key] = value;
        }

        if (parts.Length == 2)
        {
            message.Body = parts[1];
        }

        return message;
    }

    private static MessageType FromProtocolType(string type)
    {
        return type switch
        {
            "DISCOVER" => MessageType.Discover,
            "HANDSHAKE" => MessageType.Handshake,
            "HANDSHAKE_OK" => MessageType.HandshakeOk,
            "HANDSHAKE_REJECT" => MessageType.HandshakeReject,
            "TEXT" => MessageType.Text,
            "TEXT_RESPONSE" => MessageType.TextResponse,
            "CLOSE" => MessageType.Close,
            "ERROR" => MessageType.Error,
            _ => throw new FormatException($"Unknown message type: {type}")
        };
    }
}
using System.Net;
using System.Net.Sockets;
using System.Text;
using Lnc.Protocol;

const int discoveryPort = ProtocolConstants.DiscoveryPort;
const int tcpPort = ProtocolConstants.TcpPort;

Console.Write("Enter recipient nickname: ");
var recipientNickname = Console.ReadLine();

if (string.IsNullOrWhiteSpace(recipientNickname))
{
    Console.WriteLine("Recipient nickname is required.");
    return;
}

var activeRequest = new DiscoveryRequest
{
    RequestId = Guid.NewGuid(),
    Deadline = DateTime.UtcNow.AddMinutes(2),
    TcpPort = tcpPort
};

var tcpListener = new TcpListener(IPAddress.Any, tcpPort);
tcpListener.Start();

Console.WriteLine($"TCP listener started on port {tcpPort}.");

var discoverMessage = new LncMessage
{
    Type = MessageType.Discover,
    Headers =
    {
        ["Version"] = ProtocolConstants.Version,
        ["Nickname"] = recipientNickname,
        ["Deadline"] = activeRequest.Deadline.ToString("O"),
        ["Tcp-Port"] = activeRequest.TcpPort.ToString(),
        ["Request-Id"] = activeRequest.RequestId.ToString()
    }
};

var rawDiscoveryMessage = LncMessageSerializer.Serialize(discoverMessage);
var discoveryBytes = Encoding.UTF8.GetBytes(rawDiscoveryMessage);

using var udpClient = new UdpClient();
udpClient.EnableBroadcast = true;

await udpClient.SendAsync(
    discoveryBytes,
    discoveryBytes.Length,
    new IPEndPoint(IPAddress.Broadcast, discoveryPort));

Console.WriteLine("Discovery message was broadcasted.");
Console.WriteLine($"Request Id: {activeRequest.RequestId}");
Console.WriteLine("Waiting for TCP connection...");

using var tcpClient = await tcpListener.AcceptTcpClientAsync();

Console.WriteLine("Recipient connected by TCP.");

var stream = tcpClient.GetStream();

var buffer = new byte[4096];
var bytesRead = await stream.ReadAsync(buffer);
var rawHandshake = Encoding.UTF8.GetString(buffer, 0, bytesRead);

var handshakeMessage = LncMessageParser.Parse(rawHandshake);


if (handshakeMessage.Type != MessageType.Handshake)
{
    await SendMessageAsync(stream, new LncMessage
    {
        Type = MessageType.HandshakeReject,
        Headers = { ["Reason"] = "Expected HANDSHAKE message." }
    });

    return;
}

var handshakeVersion = handshakeMessage.Headers.GetValueOrDefault("Version");

if (handshakeVersion != ProtocolConstants.Version)
{
    await SendMessageAsync(stream, new LncMessage
    {
        Type = MessageType.HandshakeReject,
        Headers = { ["Reason"] = "Unsupported protocol version." }
    });

    return;
}

var requestIdValue = handshakeMessage.Headers.GetValueOrDefault("Request-Id");

if (!Guid.TryParse(requestIdValue, out var receivedRequestId))
{
    await SendMessageAsync(stream, new LncMessage
    {
        Type = MessageType.HandshakeReject,
        Headers = { ["Reason"] = "Invalid Request-Id format." }
    });

    return;
}

if (receivedRequestId != activeRequest.RequestId)
{
    await SendMessageAsync(stream, new LncMessage
    {
        Type = MessageType.HandshakeReject,
        Headers = { ["Reason"] = "Unknown Request-Id." }
    });

    return;
}

if (DateTime.UtcNow > activeRequest.Deadline)
{
    await SendMessageAsync(stream, new LncMessage
    {
        Type = MessageType.HandshakeReject,
        Headers = { ["Reason"] = "Deadline expired." }
    });

    return;
}

await SendMessageAsync(stream, new LncMessage
{
    Type = MessageType.HandshakeOk,
    Headers = { ["Message"] = "Handshake accepted." }
});

Console.WriteLine("Handshake accepted.");
Console.WriteLine();

while (true)
{
    Console.Write("Enter message or type /close: ");
    var text = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(text))
    {
        Console.WriteLine("Message cannot be empty.");
        continue;
    }

    if (text.Equals("/close", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Sending CLOSE request...");

        await SendMessageAsync(stream, new LncMessage
        {
            Type = MessageType.Close,
            Body = "Conversation finished."
        });

        var closeResponse = await ReadMessageAsync(stream);

        Console.WriteLine($"Received close response: {closeResponse.Type}");
        Console.WriteLine("Connection closed.");
        break;
    }

    var textMessage = new LncMessage
    {
        Type = MessageType.Text,
        Body = text
    };

    await SendMessageAsync(stream, textMessage);

    var response = await ReadMessageAsync(stream);

    if (response.Type != MessageType.TextResponse)
    {
        Console.WriteLine($"Expected TEXT_RESPONSE but received {response.Type}");
        break;
    }

    Console.WriteLine($"Recipient response: {response.Body}");
    Console.WriteLine();
}

tcpListener.Stop();

static async Task SendMessageAsync(NetworkStream stream, LncMessage message)
{
    var rawMessage = LncMessageSerializer.Serialize(message);
    var bytes = Encoding.UTF8.GetBytes(rawMessage);

    await stream.WriteAsync(bytes);
}

static async Task<LncMessage> ReadMessageAsync(NetworkStream stream)
{
    var buffer = new byte[4096];
    var bytesRead = await stream.ReadAsync(buffer);

    var rawMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

    return LncMessageParser.Parse(rawMessage);
}
using System.Net.Sockets;
using System.Text;
using Lnc.Protocol;

const int discoveryPort = 5051;

Console.Write("Enter your nickname: ");
var myNickname = Console.ReadLine();

if (string.IsNullOrWhiteSpace(myNickname))
{
    Console.WriteLine("Nickname is required.");
    return;
}

using var udpClient = new UdpClient(discoveryPort);

Console.WriteLine($"Listening for discovery messages on UDP port {discoveryPort}...");
Console.WriteLine();

DiscoveryInfo? discoveryInfo = null;

while (discoveryInfo is null)
{
    var result = await udpClient.ReceiveAsync();
    var rawMessage = Encoding.UTF8.GetString(result.Buffer);

    try
    {
        var message = LncMessageParser.Parse(rawMessage);

        if (message.Type != MessageType.Discover)
            continue;

        var nickname = message.Headers.GetValueOrDefault("Nickname");

        if (!string.Equals(nickname, myNickname, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Ignored discovery for nickname: {nickname}");
            continue;
        }

        discoveryInfo = new DiscoveryInfo
        {
            InitiatorIp = result.RemoteEndPoint.Address,
            RequestId = Guid.Parse(message.Headers["Request-Id"]),
            Deadline = DateTime.Parse(message.Headers["Deadline"]),
            TcpPort = int.Parse(message.Headers["Tcp-Port"])
        };

        Console.WriteLine("Discovery message received.");
        Console.WriteLine($"Initiator IP: {discoveryInfo.InitiatorIp}");
        Console.WriteLine($"Request Id: {discoveryInfo.RequestId}");
        Console.WriteLine($"TCP Port: {discoveryInfo.TcpPort}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Invalid discovery message: {ex.Message}");
    }
}

using var tcpClient = new TcpClient();

Console.WriteLine("Connecting to Initiator by TCP...");
await tcpClient.ConnectAsync(discoveryInfo.InitiatorIp, discoveryInfo.TcpPort);

Console.WriteLine("Connected to Initiator.");

var stream = tcpClient.GetStream();

var handshakeMessage = new LncMessage
{
    Type = MessageType.Handshake,
    Headers =
    {
        ["Request-Id"] = discoveryInfo.RequestId.ToString()
    }
};

await SendMessageAsync(stream, handshakeMessage);

var buffer = new byte[4096];
var bytesRead = await stream.ReadAsync(buffer);
var rawResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

var responseMessage = LncMessageParser.Parse(rawResponse);

if (responseMessage.Type == MessageType.HandshakeOk)
{
    Console.WriteLine("Handshake accepted by Initiator.");
}
else if (responseMessage.Type == MessageType.HandshakeReject)
{
    Console.WriteLine("Handshake rejected.");
    Console.WriteLine($"Reason: {responseMessage.Headers.GetValueOrDefault("Reason")}");
}
else
{
    Console.WriteLine($"Unexpected response: {responseMessage.Type}");
}

static async Task SendMessageAsync(NetworkStream stream, LncMessage message)
{
    var rawMessage = LncMessageSerializer.Serialize(message);
    var bytes = Encoding.UTF8.GetBytes(rawMessage);

    await stream.WriteAsync(bytes);
}
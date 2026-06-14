using System.Net;

namespace Lnc.Protocol;

public class DiscoveryInfo
{
    public Guid RequestId { get; set; }

    public DateTime Deadline { get; set; }

    public int TcpPort { get; set; }

    public IPAddress InitiatorIp { get; set; } = IPAddress.None;
}
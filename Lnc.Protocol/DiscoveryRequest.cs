namespace Lnc.Protocol;

public class DiscoveryRequest
{
    public Guid RequestId { get; set; }

    public DateTime Deadline { get; set; }

    public int TcpPort { get; set; }
}
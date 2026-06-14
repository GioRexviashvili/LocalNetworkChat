namespace Lnc.Protocol;

public class LncMessage
{
    public MessageType Type { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();

    public string Body { get; set; } = string.Empty;
}
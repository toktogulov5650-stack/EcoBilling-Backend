namespace EcoBilling.Api.Configuration;

public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; init; }

    public int ForwardLimit { get; init; } = 1;

    public IReadOnlyList<string> KnownProxies { get; init; } = [];
}

namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// The rule
/// </summary>
/// <author>João M F Rebelo</author>
public class Rule
{

    /// <summary>
    /// Rule name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule type (ACCEPT, REJECT, DROP)
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// List of ports to apply rule.
    /// Ex: 80,443,8080
    /// </summary>
    public string Ports { get; set; } = string.Empty;

    /// <summary>
    /// List of protocols to apply rule.
    /// The specified protocol can be one of tcp, udp, udplite, icmp, icmpv6,esp, ah, sctp, mh or the special keyword "all"
    /// </summary>
    public string Protocols { get; set; } = string.Empty;

    /// <summary>
    /// IP version to apply rule,
    /// ipv4 or ipv6 for both use both or empty,
    /// </summary>
    public IPV IPV { get; set; }

    /// <summary>
    /// IPTables chain to apply rule
    /// </summary>
    public Chain Chain { get; set; }

    /// <summary>
    /// Rule interval apply/update
    /// </summary>
    public string Interval { get; set; } = string.Empty;

    /// <summary>
    /// Timezone to apply rule if is fixed time of day
    /// </summary>
    public TimeZoneInfo? Timezone { get; set; } = null;

    /// <summary>
    /// Domains to apply rule
    /// </summary>
    public List<string> Domains { get; } = [];

}

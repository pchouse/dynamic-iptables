namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// The IP version where the rule is applied
/// </summary>
/// <author>João M F Rebelo</author>
public enum IPV
{
    /// <summary>
    /// Rules only apply to ipv4
    /// </summary>
    IPV4,

    /// <summary>
    /// Rule only apply to ipv6
    /// </summary>
    IPV6,

    /// <summary>
    /// Rule apply to both ipv4 and ipv6
    /// </summary>
    BOTH
}

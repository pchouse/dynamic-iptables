namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// Type action in iptables for the rule
/// </summary>
/// <author>João M F Rebelo</author>
public enum Type
{
    /// <summary>
    /// Allow traffic
    /// </summary>
    ACCEPT,

    /// <summary>
    /// Deny traffic
    /// </summary>
    REJECT,

    /// <summary>
    /// Drop traffic
    /// </summary>
    DROP
}

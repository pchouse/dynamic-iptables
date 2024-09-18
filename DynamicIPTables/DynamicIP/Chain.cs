namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// The chain where the rule is applied
/// </summary>
/// <author>João M F Rebelo</author>
public enum Chain
{
    /// <summary>
    /// Iptables INPUT chain
    /// </summary>
    INPUT,

    /// <summary>
    /// Iptables OUTPUT chain
    /// </summary>
    OUTPUT
}

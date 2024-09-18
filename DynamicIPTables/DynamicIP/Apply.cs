
using Coravel.Invocable;
namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// Schedule invocable Apply the rule to the system
/// </summary>
/// <author>João M F Rebelo</author>
internal class Apply : IInvocable
{

    private readonly Logger _logger;

    private readonly DNS _dns;

    private readonly NetFilter _netFilter;

    public Rule? Rule { get; set; } = null;

    public Apply(Logger logger, DNS dns, NetFilter netFilter, Rule rule)
    {
        Rule = rule;
        _logger = logger;
        _dns = dns;
        _netFilter = netFilter;
        _logger.Information("Apply new instance to rule: {rule}", Rule.Name);
    }

    /// <summary>
    /// Apply the rule
    /// </summary>
    /// <returns></returns>
    public Task Invoke()
    {
        return Task.Run(async () =>
        {

            if (Rule == null)
            {
                _logger.Warning("Rule is null, nothing to do");
                return Task.CompletedTask;
            }

            _logger.Information("Apply Rule {rule} invoked", Rule!.Name);

            _logger.Debug("Rule {rule} is IPV: {ipv}", Rule!.Name, Rule!.IPV);

            _logger.Debug("Going to create ipset for rule {Name}", Rule!.Name);

            try
            {

                await _netFilter.CreateIPSetIfNotExistAsync(Rule!);

                foreach (var ipvEnum in Enum.GetValues<IPV>())
                {
                    if (ipvEnum == IPV.BOTH) continue;
                    if (ipvEnum != Rule!.IPV && Rule.IPV != IPV.BOTH) continue;

                    var dnsIP = new List<string>();

                    foreach (var domain in Rule.Domains)
                    {
                        var addressFamily = ipvEnum == IPV.IPV4 ?
                                System.Net.Sockets.AddressFamily.InterNetwork :
                                System.Net.Sockets.AddressFamily.InterNetworkV6;

                        var ipList = await _dns.GetIPAsync(domain, addressFamily);

                        _logger.Debug("{addressFamily} for domain {domain}: {ipList}", addressFamily, domain, ipList);

                        ipList.ForEach(ip => dnsIP.Add(ip));
                    };


                    _logger.Information("Going to add {ipvEnum} to ipset for rule {Name}", ipvEnum, Rule.Name);

                    var ipSet = await _netFilter.ListIPsOfIPSetAsync(Rule.Name, ipvEnum);

                    var ipDelete = ipSet.Except(dnsIP).ToArray();

                    _logger.Debug("Rule {Name} IPs to delete: {ipDelete}", Rule.Name, ipDelete);

                    var ipAdd = dnsIP.Except(ipSet).ToArray();

                    _logger.Debug("Rule {Name} IPs to add: {ipAdd}", Rule.Name, ipAdd);

                    await _netFilter.RemoveIPFromIPSetIfExistAsync(Rule.Name, ipDelete, IPV.IPV4);
                    await _netFilter.AddIPToIPSetIfNotExistAsync(Rule.Name, ipAdd, IPV.IPV4);

                    await _netFilter.CreateIPTablesRuleIfNotExistAsync(Rule);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Error running rule {Name}, with message {Message}",
                    Rule.Name,
                     ex.Message
                );
            }

            return Task.CompletedTask;
        });
    }
}

using System.Net;
using System.Net.Sockets;

namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// Get the IP address from a domain
/// </summary>
/// <param name="logger">Logger</param>
/// <author>João M F Rebelo</author>
internal class DNS(Logger logger)
{

    /// <summary>
    /// Get the IP address from a domain
    /// </summary>
    /// <param name="domain">The domain name</param>
    /// <param name="addressFamily">The Address family IPv4 or IPv6</param>
    /// <returns></returns>
    public async Task<List<string>> GetIPAsync(string domain, AddressFamily addressFamily)
    {

        logger.Information(
            "Getting IPv{version}  from domain {domain}",
           addressFamily == AddressFamily.InterNetworkV6 ? "6" : "4", domain
        );

        var ips = new List<string>();

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain, addressFamily);

            foreach (var address in addresses)
            {
                if (address.AddressFamily == addressFamily)
                {
                    logger.Debug("IP {address} for domain {domain}", address, domain);
                    ips.Add(address.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error getting ipv{addressFamily} from domain {domain}: {ex.Message}");
        }

        logger.Debug(
            "End getting IPv{version} from domain {domain}",
            addressFamily == AddressFamily.InterNetworkV6 ? "6" : "4", domain
        );

        return ips;
    }

}

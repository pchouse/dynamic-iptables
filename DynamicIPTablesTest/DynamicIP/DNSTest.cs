using PChouse.DynamicIPTables.DynamicIP;
using System.Net.Sockets;
using Serilog;

namespace PChouse.DynamicIPTablesTest.DynamicIP;

[TestClass]
public class DNSTest
{
    
    [TestMethod]
    public void TestGetIPv4()
    {
        var dns = new DNS(new LoggerConfiguration().CreateLogger());
        var ips = dns.GetIPAsync("acme-v02.api.letsencrypt.org", AddressFamily.InterNetwork).GetAwaiter().GetResult();
        Assert.IsTrue(ips.Count > 0);
    }

    [TestMethod]
    public void TestGetIPv6()
    {
        var dns = new DNS(new LoggerConfiguration().CreateLogger());
        var ips = dns.GetIPAsync("acme-v02.api.letsencrypt.org", AddressFamily.InterNetworkV6).GetAwaiter().GetResult();
        Assert.IsTrue(ips.Count > 0);
    }
}
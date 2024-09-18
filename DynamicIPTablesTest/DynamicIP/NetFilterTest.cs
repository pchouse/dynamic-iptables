using IniFile;
using PChouse.DynamicIPTables.DynamicIP;
using Serilog;

namespace PChouse.DynamicIPTablesTest.DynamicIP;

[TestClass]
public class NetFilterTest
{

    private static Ini? s_ini;

    private static Serilog.Core.Logger? s_logger;

    [ClassInitialize]
    public static void Setup(TestContext context)
    {
        s_ini = [
            new Section("Commands"){
                new Property("ipset", "/usr/sbin/ipset"),
                new Property("iptables", "/usr/sbin/iptables"),
                new Property("ip6tables", "/usr/sbin/ip6tables")
            }
        ];

        var logConfig = new LoggerConfiguration();
        logConfig.WriteTo.Console();
        logConfig.MinimumLevel.Debug();
        s_logger = logConfig.CreateLogger();

        s_logger.Information($"Start test {context.TestName}");

    }

    [TestMethod]
    public void TestCreateListAndDestroyIPSetAndIPTableRulesAsync()
    {

        var shellExec = new ShellExec(s_logger!, s_ini!);
        var netFilter = new NetFilter(s_logger!, shellExec);

        var name = "test";

        netFilter.CreateIPSetIfNotExistAsync(name, IPV.BOTH).GetAwaiter().GetResult();

        var ipSets = netFilter.ListIPSetsAsync().GetAwaiter().GetResult();

        var ipv4SetName = netFilter.BuildSetName(name, IPV.IPV4);
        var ipv6SetName = netFilter.BuildSetName(name, IPV.IPV6);

        Assert.IsTrue(ipSets.Contains(ipv4SetName));
        Assert.IsTrue(ipSets.Contains(ipv6SetName));

        var ipv4 = new string[] { "10.10.10.10", "9.9.9.9" };
        var ipv6 = new string[] {
            "2001:db8:85a3:4444:9999:8a2e:370:7334",
            "2001:db8:85a3:9999:4444:8a2e:370:7335"
        };

        netFilter.AddIPToIPSetIfNotExistAsync(name, ipv4, IPV.IPV4).GetAwaiter().GetResult();

        var ipv4InSet = netFilter.ListIPsOfIPSetAsync(name, IPV.IPV4).GetAwaiter().GetResult();

        Assert.IsTrue(ipv4InSet.Count == ipv4.Length);
        ipv4InSet.ForEach(ip => Assert.IsTrue(ipv4.Contains(ip)));

        netFilter.AddIPToIPSetIfNotExistAsync(name, ipv6, IPV.IPV6).GetAwaiter().GetResult();

        var ipv6InSet = netFilter.ListIPsOfIPSetAsync(name, IPV.IPV6).GetAwaiter().GetResult();

        Assert.IsTrue(ipv6InSet.Count == ipv6.Length);
        ipv6InSet.ForEach(ip => Assert.IsTrue(ipv6.Contains(ip)));

        var rule = new Rule()
        {
            Name = name,
            Chain = Chain.INPUT,
            Protocols = "tcp,udp",
            Ports = "9090,8080",
            Type = DynamicIPTables.DynamicIP.Type.ACCEPT,
            IPV = IPV.BOTH
        };

        netFilter.CreateIPTablesRuleIfNotExistAsync(rule).GetAwaiter().GetResult();

        foreach (var protocol in rule.Protocols.Split(','))
        {
            Assert.IsTrue(netFilter.CheckIPTablesRuleAsync(rule, IPV.IPV4, protocol).GetAwaiter().GetResult());
            Assert.IsTrue(netFilter.CheckIPTablesRuleAsync(rule, IPV.IPV6, protocol).GetAwaiter().GetResult());
        };

        netFilter.DestroyIPTablesRuleIfExistAsync(rule).GetAwaiter().GetResult();

        foreach (var protocol in rule.Protocols.Split(','))
        {
            Assert.IsFalse(netFilter.CheckIPTablesRuleAsync(rule, IPV.IPV4, protocol).GetAwaiter().GetResult());
            Assert.IsFalse(netFilter.CheckIPTablesRuleAsync(rule, IPV.IPV6, protocol).GetAwaiter().GetResult());
        };

        netFilter.DestroyIPSetIfExistAsync(name, IPV.BOTH).GetAwaiter().GetResult();

        ipSets = netFilter.ListIPSetsAsync().GetAwaiter().GetResult();

        Assert.IsFalse(ipSets.Contains(ipv4SetName));
        Assert.IsFalse(ipSets.Contains(ipv6SetName));
    }

    [TestMethod]
    public void TestAddAndRemoveIPV4ToSet()
    {

        var shellExec = new ShellExec(s_logger!, s_ini!);
        var netFilter = new NetFilter(s_logger!, shellExec);

        var name = "test";

        netFilter.CreateIPSetIfNotExistAsync(name, IPV.IPV4).GetAwaiter().GetResult();

        var ipv4 = "9.9.9.9";

        netFilter.AddIPToIPSetIfNotExistAsync(name, [ipv4], IPV.IPV4).GetAwaiter().GetResult();

        var ips = netFilter.ListIPsOfIPSetAsync(name, IPV.IPV4).GetAwaiter().GetResult();

        Assert.IsTrue(ips.Contains(ipv4));

        netFilter.RemoveIPFromIPSetIfExistAsync(name, new string[] { ipv4 }, IPV.IPV4).GetAwaiter().GetResult();

        ips = netFilter.ListIPsOfIPSetAsync(name, IPV.IPV4).GetAwaiter().GetResult();

        Assert.IsFalse(ips.Contains(ipv4));
    }

    [TestMethod]

    public void TestAddAndRemoveIPV6ToSet()
    {
        var shellExec = new ShellExec(s_logger!, s_ini!);
        var netFilter = new NetFilter(s_logger!, shellExec);

        var name = "test";

        netFilter.CreateIPSetIfNotExistAsync(name, IPV.IPV6).GetAwaiter().GetResult();

        var ipv6 = "2001:db8:85a3:4444:9999:8a2e:370:7334";

        netFilter.AddIPToIPSetIfNotExistAsync(name, new string[] { ipv6 }, IPV.IPV6).GetAwaiter().GetResult();

        var ips = netFilter.ListIPsOfIPSetAsync(name, IPV.IPV6).GetAwaiter().GetResult();

        Assert.IsTrue(ips.Contains(ipv6));

        netFilter.RemoveIPFromIPSetIfExistAsync(name, [ipv6], IPV.IPV6).GetAwaiter().GetResult();

        ips = netFilter.ListIPsOfIPSetAsync(name, IPV.IPV6).GetAwaiter().GetResult();

        Assert.IsFalse(ips.Contains(ipv6));
    }

    [TestMethod]
    public void TestCreateIPSetIfNotExistAsyncIPVBoth()
    {
        var shellExec = new ShellExec(s_logger!, s_ini!);
        var netFilter = new NetFilter(s_logger!, shellExec);

        var rule = new Rule()
        {
            Name = "test",
            Chain = Chain.INPUT,
            Protocols = "tcp,udp",
            Ports = "9090,8080",
            Type = DynamicIPTables.DynamicIP.Type.ACCEPT,
            IPV = IPV.BOTH
        };

        netFilter.CreateIPSetIfNotExistAsync(rule).GetAwaiter().GetResult();

        var ipSets = netFilter.ListIPSetsAsync().GetAwaiter().GetResult();

        var ipv4SetName = netFilter.BuildSetName(rule.Name, IPV.IPV4);
        var ipv6SetName = netFilter.BuildSetName(rule.Name, IPV.IPV6);

        Assert.IsTrue(ipSets.Contains(ipv4SetName));
        Assert.IsTrue(ipSets.Contains(ipv6SetName));

        netFilter.DestroyIPSetIfExistAsync(rule.Name, IPV.BOTH).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void TestCreateIPSetIfNotExistAsyncIPV4()
    {
        var shellExec = new ShellExec(s_logger!, s_ini!);
        var netFilter = new NetFilter(s_logger!, shellExec);

        var rule = new Rule()
        {
            Name = "test",
            Chain = Chain.INPUT,
            Protocols = "tcp,udp",
            Ports = "9090,8080",
            Type = DynamicIPTables.DynamicIP.Type.ACCEPT,
            IPV = IPV.IPV4
        };

        netFilter.DestroyIPSetIfExistAsync(rule.Name, IPV.BOTH).GetAwaiter().GetResult();

        netFilter.CreateIPSetIfNotExistAsync(rule).GetAwaiter().GetResult();

        var ipSets = netFilter.ListIPSetsAsync().GetAwaiter().GetResult();

        var ipv4SetName = netFilter.BuildSetName(rule.Name, IPV.IPV4);
        var ipv6SetName = netFilter.BuildSetName(rule.Name, IPV.IPV6);

        Assert.IsTrue(ipSets.Contains(ipv4SetName));
        Assert.IsFalse(ipSets.Contains(ipv6SetName));

        netFilter.DestroyIPSetIfExistAsync(rule.Name, IPV.BOTH).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void TestCreateIPSetIfNotExistAsyncIPV6()
    {
        var shellExec = new ShellExec(s_logger!, s_ini!);
        var netFilter = new NetFilter(s_logger!, shellExec);

        var rule = new Rule()
        {
            Name = "test",
            Chain = Chain.INPUT,
            Protocols = "tcp,udp",
            Ports = "9090,8080",
            Type = DynamicIPTables.DynamicIP.Type.ACCEPT,
            IPV = IPV.IPV6
        };

        netFilter.DestroyIPSetIfExistAsync(rule.Name, IPV.BOTH).GetAwaiter().GetResult();

        netFilter.CreateIPSetIfNotExistAsync(rule).GetAwaiter().GetResult();

        var ipSets = netFilter.ListIPSetsAsync().GetAwaiter().GetResult();

        var ipv4SetName = netFilter.BuildSetName(rule.Name, IPV.IPV4);
        var ipv6SetName = netFilter.BuildSetName(rule.Name, IPV.IPV6);

        Assert.IsFalse(ipSets.Contains(ipv4SetName));
        Assert.IsTrue(ipSets.Contains(ipv6SetName));

        netFilter.DestroyIPSetIfExistAsync(rule.Name, IPV.BOTH).GetAwaiter().GetResult();
    }

}

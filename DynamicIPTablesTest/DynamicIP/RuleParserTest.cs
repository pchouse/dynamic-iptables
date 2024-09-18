using IniFile;

using PChouse.DynamicIPTables.DynamicIP;

using Serilog;

namespace PChouse.DynamicIPTablesTest.DynamicIP;

[TestClass]
public class RuleParserTest
{

    [TestMethod]
    [DataRow("ACCEPT", "22,80", "tcp,udp", "ipv4", "INPUT", "5m", "Asia/Seoul", "example.com")]
    [DataRow("REJECT", "22,80", "udp", "ipv6", "OUTPUT", "1h", "Asia/Seoul", "example.com,pchouse.pt")]
    [DataRow("DROP", "80", "all", "both", "OUTPUT", "19H09", "Asia/Seoul", "example.com")]
    public void ParseAsyncTest(
        string type,
        string ports,
        string protocols,
        string ipv,
        string chain,
        string interval,
        string timezone,
        string domains
    )
    {
        var logConfig = new LoggerConfiguration();
        logConfig.WriteTo.Console();
        logConfig.MinimumLevel.Debug();
        var logger = logConfig.CreateLogger();

        var ruleParser = new RuleParser(logger, "");

        var section = new Section("General")
        {
            new Property("type", type),
            new Property("ports", ports),
            new Property("protocols", protocols),
            new Property("ipv", ipv),
            new Property("chain", chain),
            new Property("interval", interval),
            new Property("timezone", timezone),
            new Property("domains", domains)
        };
        
        var rule = ruleParser.ParseAsync(section, "test").GetAwaiter().GetResult();

        Assert.AreEqual(type, rule.Type.ToString());
        Assert.AreEqual(ports, rule.Ports);
        Assert.AreEqual(protocols, rule.Protocols);
        Assert.AreEqual(ipv, rule.IPV.ToString().ToLower());
        Assert.AreEqual(chain, rule.Chain.ToString());
        Assert.AreEqual(interval, rule.Interval);
        Assert.AreEqual(timezone, rule.Timezone?.Id);

        var domainsStack = domains.Split(",");
        foreach (var domain in domainsStack)
        {
            Assert.IsTrue(rule.Domains.Contains(domain));
        }
    }

}
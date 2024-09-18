using System.Text.RegularExpressions;

namespace PChouse.DynamicIPTables.DynamicIP;

internal class RuleParser(Logger logger, string configFolderPath)
{
    /// <summary>
    /// Allowed protocols
    /// </summary>
    private static readonly string[] ProtocolsAllowed = [
             "tcp", "udp", "udplite", "icmp", "icmpv6", "esp", "ah", "sctp", "mh", "all"
    ];

    /// <summary>
    /// Parse all rules from /etc/dynamic-iptables/rules.d
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task<List<Rule>> ParseRulesAsync(CancellationToken cancellationToken)
    {

        return Task.Run(async () =>
        {

            var rulesList = new List<Rule>();

            logger.Information("Start parsing rules");

            // Load all *.conf files from /etc/dynamic-iptables/rules.d

            var rulesPath = Path.Combine(configFolderPath, "rules.d");

            logger.Debug("Rules path: {rulesPath}", rulesPath);

            var rules = Directory.GetFiles(rulesPath, "*.conf", SearchOption.TopDirectoryOnly);

            foreach (var ruleConfigPath in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ruleFileName = Path.GetFileName(ruleConfigPath);
                var ruleFileNameShort = ruleFileName.Replace(".conf", "");

                // RegExp to verify if file name only have characters
                if (!Regex.IsMatch(ruleFileNameShort, "^([a-zA-Z]-?)+$"))
                {
                    logger.Fatal("Invalid rule file name: '{fileName}', only can have characters [a-zA-Z]", ruleFileName);
                    throw new Exception($"Invalid rule file name: '{ruleFileName}', only can have characters [a-zA-Z]");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    logger.Warning("Cancellation token requested");
                    break;
                }

                logger.Information("Loading rule file: {rule}", ruleConfigPath);

                var ini = new IniFile.Ini(ruleConfigPath, new IniFile.IniLoadSettings
                {
                    CaseSensitive = false,
                    IgnoreBlankLines = true,
                    IgnoreComments = true,
                }) ?? throw new Exception($"Configuration file not found at: {ruleConfigPath}");


                var generalSection = ini["General"] ??
                    throw new Exception($"Missing 'General' section in configuration file {ruleFileName}");

                var rule = await ParseAsync(generalSection, ruleFileNameShort);

                rulesList.Add(rule);
            }

            return rulesList;

        }, cancellationToken);
    }

    /// <summary>
    /// Parse a rule from a general section
    /// </summary>
    /// <param name="generalSection"></param>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task<Rule> ParseAsync(IniFile.Section generalSection, string ruleName)
    {
        return Task.Run(() =>
        {
            var rule = new Rule
            {
                Name = ruleName
            };

            if (!Enum.TryParse<Type>(generalSection["type"].ToString()?.Trim() ?? "", true, out var type))
            {
                var msg = $"Invalid rule type: {generalSection["type"]} in rule {ruleName}";
                logger.Fatal(msg);
                throw new Exception(msg);
            }

            rule.Type = type;

            rule.Ports = generalSection["ports"].ToString()?.Trim(',')?.Replace(" ", "") ?? "";

            if (Regex.IsMatch(rule.Ports, "^([^0-9],?)+$"))
            {
                var msg = $"Invalid ports: {rule.Ports} in rule {ruleName}";
                logger.Fatal(msg);
                throw new Exception(msg);
            }

            rule.Ports.Split(',').ToList().ForEach(port =>
            {
                var portNumber = int.Parse(port.Trim());
                if (portNumber < 1 || portNumber > 65535)
                {
                    var msg = $"Invalid port: {portNumber} in rule {ruleName}";
                    logger.Fatal(msg);
                    throw new Exception(msg);
                }
            });

            rule.Protocols = generalSection["protocols"].ToString()?.Trim(',')?.Replace(" ", "") ?? "all";
            rule.Protocols = rule.Protocols.ToLower();

            if (string.IsNullOrWhiteSpace(rule.Protocols)) rule.Protocols = "all";

            rule.Protocols.Split(',').ToList().ForEach(protocol =>
            {
                if (!ProtocolsAllowed.Contains(protocol))
                {
                    var msg = $"Invalid protocol: {protocol} in rule {ruleName}";
                    logger.Fatal(msg);
                    throw new Exception(msg);
                }
            });

            if (!Enum.TryParse<IPV>(generalSection["ipv"].ToString()?.Trim() ?? "", true, out var ipv))
            {
                var msg = $"Invalid rule ipv: {generalSection["ipv"]} in rule {ruleName}";
                logger.Fatal(msg);
                throw new Exception(msg);
            }

            rule.IPV = ipv;

            if (!Enum.TryParse<Chain>(generalSection["chain"].ToString()?.Trim(), true, out var chain))
            {
                var msg = $"Invalid rule chain: {generalSection["chain"]} in rule {ruleName}";
                logger.Fatal(msg);
                throw new Exception(msg);
            }

            rule.Chain = chain;

            rule.Interval = generalSection["interval"].ToString()?.Trim() ?? "";

            if (string.IsNullOrEmpty(rule.Interval) || !Regex.IsMatch(rule.Interval, "^([0-9]+(s|m|h))|([0-9]{2}H[0-9]{2})$"))
            {
                var msg = $"Invalid rule interval: {rule.Interval ?? "empty"} in rule {ruleName}";
                logger.Fatal(msg);
                throw new Exception(msg);
            }


            var domains = generalSection["domains"].ToString()?.Trim()?.Replace(" ", "") ?? "";

            if (string.IsNullOrEmpty(domains))
            {
                var msg = $"No domains in rule {ruleName}";
                logger.Fatal(msg);
                throw new Exception(msg);
            }

            domains.ToString().Split(',').ToList().ForEach(domain =>
            {
                if (!Regex.IsMatch(domain, "^([a-zA-Z0-9-]+\\.)+[a-zA-Z0-9-]+$"))
                {
                    var msg = $"Invalid domain: {domain} in rule {ruleName}";
                    logger.Fatal(msg);
                    throw new Exception(msg);
                }
                logger.Debug($"Domain: {domain} add to rule {rule.Name}");
                rule.Domains.Add(domain);
            });

            try{
            rule.Timezone = TimeZoneInfo.FindSystemTimeZoneById(
                generalSection["timezone"].ToString() ?? ""
            );
            }catch(Exception ex){
                logger.Error("Timezone not found: {ex.Message}");
                throw new Exception($"Error finding timezone: {ex.Message}");
            }

            return rule;
        });
    }

}

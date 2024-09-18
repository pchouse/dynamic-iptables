namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// Linux NetFilter operations: ipset, iptables, ip6tables
/// </summary>
/// <author>João M F Rebelo</author>
internal class NetFilter(Logger logger, ShellExec shellExec)
{
    /// <summary>
    /// The iptables operation type, 
    /// define if the rule is to be inserted, deleted or checked if exists
    /// </summary>
    internal enum OperationType
    {
        INSERT,
        DELETE,
        CHECK
    }

    /// <summary>
    /// Build the ipset set name fro the rule name and family
    /// </summary>
    /// <param name="name">The rule name</param>
    /// <param name="family">The IPv</param>
    /// <returns>The set name</returns>
    public string? BuildSetName(string name, IPV ipv)
    {
        if (ipv == IPV.BOTH || string.IsNullOrWhiteSpace(name))
        {
            logger.Error(
                "Not possible to destroy ipset set name is empty for rule name {name} and ipv",
                name,
                ipv
            );
            return null;
        };

        return $"DIP-{name.ToUpper()}-{ipv.ToString().ToUpper()}";
    }

    /// <summary>
    /// Create a new ipset set if not exist
    /// </summary>
    /// <param name="name">The rule name whom the set is to be created</param>
    /// <param name="ipv">IP versions</param>
    /// <returns></returns>
    public Task CreateIPSetIfNotExistAsync(string name, IPV ipv)
    {
        return Task.Run(async () =>
        {

            logger.Debug(
                "Create ipset set for rule {name}, ipv: {ipv}", name, ipv
            );

            foreach (var ipvEnum in Enum.GetValues<IPV>())
            {
                if (ipvEnum == IPV.BOTH) continue;
                if (ipvEnum != ipv && ipv != IPV.BOTH) continue;

                var setName = BuildSetName(name, ipvEnum);

                if (setName == null) return;

                logger.Information(
                    "Create if not exist ipset set name: {setName}, family: {ipvEnum}, for rule name '{name}'",
                    setName,
                    ipvEnum,
                    name
                );

                var ipsetFamily = ipvEnum == IPV.IPV4 ? "inet" : "inet6";

                var command = $"{shellExec.IPSetPath} create -exist {setName} hash:ip family {ipsetFamily}";

                using var process = await shellExec.ExecuteAsync(command);
            }
        });
    }

    /// <summary>
    /// Create all ipset for rule set if not exist
    /// </summary>
    /// <param name="rule">The rule</param>
    /// <returns></returns>
    public Task CreateIPSetIfNotExistAsync(Rule rule)
    {
        return Task.Run(async () =>
        {
            foreach (var ipvEnum in Enum.GetValues<IPV>())
            {
                if (ipvEnum == IPV.BOTH) continue;
                if (ipvEnum != rule.IPV && rule.IPV != IPV.BOTH) continue;
                await CreateIPSetIfNotExistAsync(rule.Name, ipvEnum);
            }
        });
    }

    /// <summary>
    /// List all existent ipset sets
    /// </summary>
    /// <returns>List of all set names</returns>
    public Task<string[]> ListIPSetsAsync()
    {
        return Task.Run(async () =>
        {
            var command = $"{shellExec.IPSetPath} --list -n -sorted";

            using var process = await shellExec.ExecuteAsync(command);

            if (process.ExitCode != 0)
            {
                return [];
            }

            using var output = process.StandardOutput;

            var ipSets = (await output.ReadToEndAsync()).Trim();

            logger.Debug("Existent ipset sets: {ipSet}", ipSets);

            return string.IsNullOrWhiteSpace(ipSets) ? [] : ipSets.Split("\n");
        });
    }

    /// <summary>
    /// Get all ips in a ipset set
    /// </summary>
    /// <param name="name">The rule name whom the set belongs</param>
    /// <param name="ipv"></param>
    /// <returns>The ips list in the set</returns>
    public Task<List<string>> ListIPsOfIPSetAsync(string name, IPV ipv)
    {
        return Task.Run(async () =>
        {
            var ips = new List<string>();

            if (ipv == IPV.BOTH)
            {
                logger.Error("IPV.BOTH is not supported for ListIPsOfIPSetAsync");
                return ips;
            }

            var setName = BuildSetName(name, ipv);

            logger.Debug("List IPs of ipset set {setName}", setName);

            var command = $"{shellExec.IPSetPath} list {setName} -sorted";

            using var process = await shellExec.ExecuteAsync(command);

            if (process.ExitCode != 0) return ips;

            var start = false;
            using var reader = process.StandardOutput;
            while (!reader.EndOfStream)
            {
                var line = (await reader.ReadLineAsync())?.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (!start && line.Contains("Members:"))
                {
                    start = true;
                    continue;
                }

                if (start) ips.Add(line);
            }

            logger.Debug("IPs of ipset set {setName}: {ips}", setName, ips);

            return ips;
        });
    }

    /// <summary>
    /// Destroy a ipset set
    /// </summary>
    /// <param name="name">The name of rule whom the set is to destroyed</param>
    /// <param name="ipv">IP versions</param>
    /// <returns></returns>
    public Task DestroyIPSetIfExistAsync(string name, IPV ipv)
    {
        return Task.Run(async () =>
        {

            logger.Debug(
                "Destroy id exist ipset of rule name: {name}, ipv: {ipv}", name, ipv
            );

            foreach (var ipvEnum in Enum.GetValues<IPV>())
            {
                if (ipvEnum == IPV.BOTH) continue;
                if (ipvEnum != ipv && ipv != IPV.BOTH) continue;

                var setName = BuildSetName(name, ipvEnum);

                if (setName == null) return;

                logger.Information("Going to destroy ipset: {setName}", setName);

                var command = $"{shellExec.IPSetPath} destroy {setName} -exist";

                using var process = await shellExec.ExecuteAsync(command);
            }
        });
    }

    /// <summary>
    /// Destroy all ipset sets of a rule
    /// </summary>
    /// <param name="rule"></param>
    /// <returns></returns>
    public Task DestroyIPSetIfExistAsync(Rule rule)
    {
        return Task.Run(async () =>
        {
            foreach (var ipvEnum in Enum.GetValues<IPV>())
            {
                if (ipvEnum == IPV.BOTH) continue;
                if (ipvEnum != rule.IPV && rule.IPV != IPV.BOTH) continue;
                await DestroyIPSetIfExistAsync(rule.Name, ipvEnum);
            }
        });
    }

    /// <summary>
    /// Add IPs to a ipset set
    /// </summary>
    /// <param name="name">The name of rule</param>
    /// <param name="ips">List of ipv4 or ipv6 to be added, multiport both is not supported</param>
    /// <param name="ipv">IPV4 or IPV6, both is not allowed</param>
    /// <returns></returns>
    public Task AddIPToIPSetIfNotExistAsync(string name, string[] ips, IPV ipv)
    {
        return Task.Run(async () =>
        {

            if (ipv == IPV.BOTH)
            {
                logger.Error("IPV.BOTH is not supported for AddIPToIPSetAsync");
                return;
            }

            if(ips.Length == 0)
            {
                logger.Information("No IPs to add to ipset set {name}", name);
                return;
            }

            var setName = BuildSetName(name, ipv);

            logger.Debug("Start adding ips: {ip} to ipset set {setName}", ips, setName);

            foreach (var ip in ips)
            {

                logger.Information("Add IP {ip} to ipset set {setName}", ip, setName);

                var command = $"{shellExec.IPSetPath} add {setName} {ip} -exist";

                using var process = await shellExec.ExecuteAsync(command);
            }
        });
    }

    /// <summary>
    /// Remove IPs from a ipset set
    /// </summary>
    /// <param name="name">The name of rule</param>
    /// <param name="ips">List of ipv4 or ipv6 to be removed, both is not supported</param>
    /// <param name="ipv">IPV4 or IPV6, both is not allowed</param>
    /// <returns></returns>
    public Task RemoveIPFromIPSetIfExistAsync(string name, string[] ips, IPV ipv)
    {
        return Task.Run(async () =>
        {

            if (ipv == IPV.BOTH)
            {
                logger.Error("IPV.BOTH is not supported for RemoveIPFromIPSetAsync");
                return;
            }

            if(ips.Length == 0)
            {
                logger.Information("No IPs to remove from ipset set {name}", name);
                return;
            }

            var setName = BuildSetName(name, ipv);

            logger.Debug("Start removing ips: {ip} from ipset set {setName}", ips, setName);

            foreach (var ip in ips)
            {
                logger.Information("Remove IP {ip} from ipset set {setName}", ip, setName);

                var command = $"{shellExec.IPSetPath} del {setName} {ip} -exist";

                using var process = await shellExec.ExecuteAsync(command);
            }
        });
    }

    /// <summary>
    /// Build the iptables command
    /// </summary>
    /// <param name="rule">The rule</param>
    /// <param name="ipv">IP version, ipv4 or ipv6, both are not allowed</param>
    /// <param name="operationType">The operation type</param>
    /// <param name="protocol">The protocol, can be one of: tcp, udp, udplite, icmp, icmpv6, esp, ah, sctp, mh, all</param>
    /// <returns>The command</returns>
    public string? BuildIPTablesCommand(Rule rule, IPV ipv, OperationType operationType, string protocol)
    {
        var setName = BuildSetName(rule.Name, ipv);
        var match = rule.Chain == Chain.OUTPUT ? "dst" : "src";
        var ports = string.IsNullOrWhiteSpace(rule.Ports) ? "" : $" --match multiport --dports {rule.Ports}";
        var operation = operationType.Expand();
        var path = ipv == IPV.IPV4 ? shellExec.IPTablesPath : shellExec.IP6TablesPath;

        if (operation == null || setName == null || ipv == IPV.BOTH) return null;

        return $"{path} -t filter {operation} {rule.Chain} -p {protocol}{ports} -m set --match-set {setName} {match} -j {rule.Type}";
    }

    /// <summary>
    /// Check if a rule exists in iptables
    /// </summary>
    /// <param name="rule">The rule</param>
    /// <param name="ipv">ipv4 or ipv6, both are not allowed</param>
    /// <param name="protocol">The protocol, can be one of: tcp, udp, udplite, icmp, icmpv6, esp, ah, sctp, mh, all</param>
    /// <returns>True if the rule exists</returns>
    /// <exception cref="Exception">If the ipv is IPV.BOTH or if was not possible to build the command</exception>
    public Task<bool> CheckIPTablesRuleAsync(Rule rule, IPV ipv, string protocol)
    {
        return Task.Run(async () =>
        {
            if (ipv == IPV.BOTH)
            {
                throw new Exception("IPV.BOTH is not supported for CheckIPTablesRuleAsync");
            }

            var command = BuildIPTablesCommand(rule, ipv, OperationType.CHECK, protocol) ??
                throw new Exception("Invalid command");

            using var process = await shellExec.ExecuteAsync(command);

            return process.ExitCode == 0;
        });
    }

    /// <summary>
    /// Create a new iptables rule if not exists
    /// </summary>
    /// <param name="rule"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task CreateIPTablesRuleIfNotExistAsync(Rule rule)
    {
        return Task.Run(async () =>
        {
            foreach (var ipvEnum in Enum.GetValues<IPV>())
            {
                if (ipvEnum == IPV.BOTH) continue;
                if (ipvEnum != rule.IPV && rule.IPV != IPV.BOTH) continue;

                foreach (var protocol in rule.Protocols.Split(","))
                {
                    if (string.IsNullOrWhiteSpace(protocol)) continue;
                    if (await CheckIPTablesRuleAsync(rule, ipvEnum, protocol)) continue;

                    var command = BuildIPTablesCommand(rule, ipvEnum, OperationType.INSERT, protocol) ??
                        throw new Exception("Invalid command");

                    using var process = await shellExec.ExecuteAsync(command);
                };
            }
        });
    }

    /// <summary>
    /// Destroy a iptables rule if exists
    /// </summary>
    /// <param name="rule"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task DestroyIPTablesRuleIfExistAsync(Rule rule)
    {
        return Task.Run(async () =>
        {
            // Destroy iptables rule
            foreach (var ipvEnum in Enum.GetValues<IPV>())
            {
                if (ipvEnum == IPV.BOTH) continue;
                if (ipvEnum != rule.IPV && rule.IPV != IPV.BOTH) continue;

                foreach (var protocol in rule.Protocols.Split(","))
                {
                    if (!await CheckIPTablesRuleAsync(rule, ipvEnum, protocol)) return;

                    var command = BuildIPTablesCommand(rule, ipvEnum, OperationType.DELETE, protocol) ??
                        throw new Exception("Invalid command");

                    using var process = await shellExec.ExecuteAsync(command);
                };
            }
        });
    }

}

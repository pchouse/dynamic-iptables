/// <summary>MIT License</summary>
/// <Copyright> (c) 2024 Reflexão Estudos e Sistemas Informáticos, LDA (PChouse - https://github.com/pchouse)</Copyright>
/// <author>João M F Rebelo (https://github.com/joaomfrebelo)</author>

using Coravel;
using PChouse.DynamicIPTables;
using PChouse.DynamicIPTables.DynamicIP;

try
{
    Console.WriteLine("DynamicIPTables starting...");

    if (!OperatingSystem.IsLinux())
    {
        throw new PlatformNotSupportedException("This application is only supported on Linux");
    }

    if(Environment.UserName == "root")
    {
        Console.WriteLine("");
        Console.WriteLine("***** This application should not be run as root *****");
        Console.WriteLine("");
        Console.WriteLine("In /ets/sudoers.d/ add the following line:");
        Console.WriteLine("dynamiciptables ALL=(root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/ip6tables, /usr/sbin/ipset");
        Console.WriteLine("Create a user and a group 'dynamiciptables'");
        Console.WriteLine("sudo useradd dynamiciptables -U -M -g dynamiciptables -s /sbin/nologin");
        Console.WriteLine("In systemd service file set the ExecUser to 'dynamiciptables'");
        Console.WriteLine("Then run the application as the user 'dynamiciptables'");
        Console.WriteLine("In the systemd service file set the User to 'dynamiciptables' (User=dynamiciptables)");
        Console.WriteLine("and the Group to 'dynamiciptables' (Group=dynamiciptables)");
        Console.WriteLine("");

        throw new Exception("This application should not be run as root");
    }

    Console.WriteLine($"DynamicIPTables running as user: {Environment.UserName}");

    var trueValues = new string[] { "yes", "true", "1" };

    var builder = Host.CreateDefaultBuilder(args);
    // use systemd
    builder.UseSystemd();
    // Configure the services
    builder.ConfigureServices((hostContext, services) =>
    {
        // Is development environment
        var isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
        var configFolderPath = Path.Combine(
            isDevelopment ? (Environment.GetEnvironmentVariable("PROJECT_FOLDER") ?? "") : "/",
            "etc/dynamic-iptables"
        );

        // Add the configuration folder path to the services
        services.AddSingleton(configFolderPath);

        // Add the configuration file to the services
        var iniFilePath =  Path.Combine(configFolderPath, "dynamic-iptables.conf");

        var ini = new IniFile.Ini(iniFilePath, new IniFile.IniLoadSettings
        {
            IgnoreComments = true,
        }) ?? throw new Exception($"Configuration file not found at: {iniFilePath}");

        Console.WriteLine($"Configuration file loaded from: {iniFilePath}");

        services.AddSingleton(ini);
        
        // Add scheduler service
        services.AddScheduler();

        // Add DNS service
        services.AddSingleton<DNS>();

        // Add ShellExec service
        services.AddSingleton<ShellExec>();

        // Add NetFilter service
        services.AddSingleton<NetFilter>();

        // Add RuleParser service
        services.AddSingleton<RuleParser>();

        var log = new LoggerConfiguration();
        log.ConfigureLogger(ini);
        services.AddSingleton(log.CreateLogger());

        services.AddHostedService<Worker>();
    });

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

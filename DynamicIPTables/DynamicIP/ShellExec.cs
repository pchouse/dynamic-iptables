using System.Diagnostics;

namespace PChouse.DynamicIPTables.DynamicIP;

/// <summary>
/// Execute shell commands
/// </summary>
/// <author>João M F Rebelo</author>
public class ShellExec
{

    private readonly Logger _logger;

    public readonly string IPSetPath;

    public readonly string IPTablesPath;

    public readonly string IP6TablesPath;

    /// <summary>
    /// Create a new instance of ShellExec
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="ini">Configuration options</param>
    public ShellExec(Logger logger, IniFile.Ini ini)
    {
        _logger = logger;
        _logger.Debug("ShellExec new instance created");
        IPSetPath = ini["Commands"]["ipset"];
        IPTablesPath = ini["Commands"]["iptables"];
        IP6TablesPath = ini["Commands"]["ip6tables"];
    }

    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="startInfo"></param>
    /// <returns>The process after stop running</returns>
    public Task<Process> ExecuteAsync(ProcessStartInfo startInfo)
    {
        return Task.Run(async () =>
        {
            _logger.Debug("Executing command: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);

            var process = new Process();
            process.StartInfo = startInfo;

            process.Start();

            _logger.Debug(
                "Process ID {Id} for command:  {FileName} {Arguments}", 
                process.Id,
                startInfo.FileName, 
                startInfo.Arguments
            );

            if (!process.WaitForExit(TimeSpan.FromSeconds(9)))
            {
                process.Kill();
                
                _logger.Error(
                    "Process kill timeout after 9 seconds, process id: {Id}, process {startInfo}",
                    process.Id,
                    startInfo
                );
            }

            _logger.Debug("Process id {Id} finished", process.Id);

            if(process.ExitCode == 0){
                _logger.Debug(
                    "Process {Id} finished with exit code {ExitCode}", process.Id, process.ExitCode
                );
            } else {
                _logger.Error(
                    "Process {Id} finished with exit code {ExitCode}", process.Id, process.ExitCode
                );
                
                using var output = process.StandardError;
                var error = await output.ReadToEndAsync();
                _logger.Error(error);   
            }

            return process;
        });
    }

    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="arguments">The command</param>
    /// <returns>The process after stop running</returns>
    public async Task<Process> ExecuteAsync(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sudo",
            Arguments = $"-S {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        return await ExecuteAsync(startInfo);
    }

}

using System.Text.RegularExpressions;
using Coravel.Scheduling.Schedule.Interfaces;
using PChouse.DynamicIPTables.DynamicIP;

namespace PChouse.DynamicIPTables;

/// <summary>
/// Extension methods
/// </summary>
/// <author>João M F Rebelo</author>
internal static class ExtensionMethods
{

    /// <summary>
    /// Configure logger
    /// </summary>
    /// <param name="log">Logger configuration</param>
    /// <param name="ini">Configurations</param>
    /// <exception cref="Exception"></exception>
    internal static void ConfigureLogger(this LoggerConfiguration log, IniFile.Ini ini)
    {
        if (!ini.Contains("Log"))
        {
            throw new Exception($"Missing 'Log' section in configuration file");
        }

        var logConfig = ini["Log"];


        if (bool.TryParse(logConfig["console"].ToString()?.ToLower() ?? "false", out var logToConsole))
        {
            if (logToConsole)
            {
                log.WriteTo.Async(c => c.Console());
            }
        }
        else
        {
            throw new Exception("Invalid value for 'console' in 'Log' section of config file. Please set a bool value (Ex: console=true).");
        }

        var logFilePath = logConfig["file"].ToString();

        // verify directory of file logFilePath is writable
        if (!string.IsNullOrEmpty(logFilePath))
        {
            var logFileDir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logFileDir))
            {
                if (!Directory.Exists(logFileDir))
                {
                    throw new Exception($"Directory '{logFileDir}' does not exist to write log.");
                }

                try
                {
                    // check if directory is writable
                    using var fs = File.Create(Path.Combine(logFileDir, "test.log"), 1, FileOptions.DeleteOnClose);
                    fs.Write([0], 0, 1);
                    fs.Close();
                }
                catch (Exception)
                {
                    throw new Exception($"Directory '{logFileDir}' is not writable to write log.");
                }
            }

            log.WriteTo.Async(c => c.File(logFilePath));

            try
            {
                log.MinimumLevel.Is(Enum.Parse<Serilog.Events.LogEventLevel>(logConfig["level"].ToString(), true));
            }
            catch (Exception)
            {
                throw new Exception($"Invalid value for 'level' in 'Log' section of config file. Please set a valid log level (Ex: level=Information).");
            }
            
        }

        if (!string.IsNullOrEmpty(logConfig["file"].ToString()))
        {
            log.WriteTo.Async(c => c.File(
                logConfig["file"].ToString(),
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(30)
                )
            );
        }
    }

    /// <summary>
    /// Schedule rule
    /// </summary>
    /// <param name="scheduler">Scheduler</param>
    /// <param name="rule">The rule to be schedule</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static Task<IScheduledEventConfiguration> ScheduleRuleAsync(this IScheduler scheduler, Rule rule)
    {
        return  Task.Run<IScheduledEventConfiguration>(() => {
            
            var scheduleEvent = scheduler.ScheduleWithParams<Apply>(rule);


            if (Regex.IsMatch(rule.Interval, @"^[0-9]+m$"))
            {
                
                var minutes = int.Parse(rule.Interval.Replace("m", ""));

                if (minutes < 5)
                {
                    throw new Exception($"Minutes interval must be greater or equal than 5 in rule '{rule.Name}'");
                }

                return scheduleEvent.Cron($"*/{minutes} * * * *").PreventOverlapping(rule.Name);
                
            }

            if (Regex.IsMatch(rule.Interval, @"^[0-9]+h$"))
            {
                var hours = int.Parse(rule.Interval.Replace("h", ""));

                if (hours < 1)
                {
                    throw new Exception($"Hour interval must be greater or equal to 1 in rule '{rule.Name}'");
                }

                return scheduleEvent.Cron($"* */{hours} * * *").PreventOverlapping(rule.Name);
            }

            if (Regex.IsMatch(rule.Interval, @"^[0-9]{2}H[0-9]{2}$"))
            {
                var hour = int.Parse(rule.Interval[..2]);
                var minute = int.Parse(rule.Interval[3..]);

                if (hour > 23)
                {
                    throw new Exception($"Hour in fix hour interval must be less or equal to 23 in rule '{rule.Name}'");
                }
                
                if (hour > 59)
                {
                    throw new Exception($"Minute in fix hour interval must be less or equal to 59 in rule '{rule.Name}'");
                }

                var scheduleEventConfig = scheduleEvent
                              .DailyAt(hour, minute)
                              .PreventOverlapping(rule.Name);

                if(rule.Timezone != null) scheduleEventConfig.Zoned(rule.Timezone);

                return scheduleEventConfig;
            }

            throw new Exception($"Invalid interval '{rule.Interval}' for rule '{rule.Name}'");

        });
    }

    /// <summary>
    /// Expand the operation type to correct iptables command argument
    /// </summary>
    /// <param name="operationType"></param>
    /// <returns></returns>
    public static string? Expand(this NetFilter.OperationType operationType)
    {
        return operationType switch
        {
            NetFilter.OperationType.INSERT => "-I",
            NetFilter.OperationType.DELETE => "-D",
            NetFilter.OperationType.CHECK => "-C",
            _ => null
        };
    }

}
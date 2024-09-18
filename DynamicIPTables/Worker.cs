using System.Text.RegularExpressions;
using Coravel.Scheduling.Schedule.Interfaces;
using PChouse.DynamicIPTables.DynamicIP;

namespace PChouse.DynamicIPTables;

/// <summary>
/// The service worker
/// </summary>
/// <param name="serviceProvider">DI service provider</param>
public class Worker(IServiceProvider serviceProvider) : BackgroundService
{

    /// <summary>
    /// Logger
    /// </summary>
    private readonly Logger _logger = serviceProvider.GetRequiredService<Logger>();
    
    /// <summary>
    /// Scheduler
    /// </summary>
    private readonly IScheduler _scheduler = serviceProvider.GetRequiredService<IScheduler>();
    
    /// <summary>
    /// Rule parser
    /// </summary>
    private readonly RuleParser _ruleParser = serviceProvider.GetRequiredService<RuleParser>();
    
    /// <summary>
    /// DNS provider
    /// </summary>
    private readonly DNS _dns = serviceProvider.GetRequiredService<DNS>();
    
    /// <summary>
    /// Net filter commands provider: iptables and ipset 
    /// </summary>
    private readonly NetFilter _netFilter = serviceProvider.GetRequiredService<NetFilter>();
    
    /// <summary>
    /// The rules
    /// </summary>
    private List<Rule>? _rules = null;

    /// <summary>
    /// Invoked when the service is starting
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Worker start StartAsync");

        _rules = await _ruleParser.ParseRulesAsync(stoppingToken);

        _logger.Debug("Start scheduling rules");


        foreach (var rule in _rules ?? [])
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.Information("Cancellation token requested when creating rules schedule");
                return;
            }

            try
            {
                _logger.Information($"Scheduling rule: {rule.Name}");
                _ = (await _scheduler.ScheduleRuleAsync(rule)).When(
                    () => Task.Run(() => !stoppingToken.IsCancellationRequested
                ));
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
                throw new Exception($"Error scheduling rule: {ex.Message}");
            }
        }

        (_scheduler as Coravel.Scheduling.Schedule.Scheduler)?.OnError((exception) => _logger.Error(
                $"Error running scheduling rules: {exception.Message}"
         ));

        _logger.Debug("End scheduling rules");

        await base.StartAsync(stoppingToken);

        _logger.Information("End invocation of WorkerStart");
    }

    /// <summary>
    /// Invoked when the service is executing
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Worker Execute task start");

        _logger.Debug("Start first run of rules");

        foreach (var rule in _rules ?? [])
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.Information("Cancellation token requested when first running rules");
                continue;
            }

            try
            {
                if (Regex.IsMatch(rule.Interval, @"^[0-9]{2}H[0-9{2}]$"))
                {
                    _logger.Information($"Rule {rule.Name} has not eligible for first run");
                    continue;
                }

                _logger.Information($"First run of rule: {rule.Name}");
                var apply = new Apply(
                    _logger,
                    _dns,
                    _netFilter,
                    rule
                    );
                await apply.Invoke();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
                throw new Exception($"Error applying rule: {ex.Message}");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.Information("DynamicIPTables is running");
            await Task.Delay(1000 * 60 * 60, stoppingToken);
        };
    }

    /// <summary>
    /// Invoked when the service is stopping
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Worker stop invoked");

        try
        {
            foreach (var rule in _rules ?? [])
            {
                _logger.Information($"Removing rule: {rule.Name}");
                await _netFilter.DestroyIPTablesRuleIfExistAsync(rule);
                await Task.Delay(199, CancellationToken.None);
                await _netFilter.DestroyIPSetIfExistAsync(rule);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error when stopping the service: {Message}", ex.Message);
        }

        await base.StopAsync(stoppingToken);

        _logger.Information("Worker stopped");

        _logger.Dispose();
    }

}

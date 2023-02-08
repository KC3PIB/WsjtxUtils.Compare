using Microsoft.Extensions.Configuration;
using Serilog;
using WsjtxUtils.Compare.Common;
using WsjtxUtils.Compare.Common.Settings;

// read configuration file
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("config.json", true, false)
    .AddCommandLine(args)
    .Build();

// setup & create the logger
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

// start the server and run until canceled
await new CompareServer(configuration.Get<CompareSettings?>()).RunAsync(GenerateCancellationTokenSource());

Log.CloseAndFlush();

/// <summary>
/// Creates a <see cref="CancellationTokenSource"/> which will signal
/// the task cancellation on pressing CTRL-C in the console application
/// </summary>
/// <returns></returns>
static CancellationTokenSource GenerateCancellationTokenSource()
{
    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        Log.Information("CTRL-C Canceling...");
        cancellationTokenSource.Cancel();
        e.Cancel = true;
    };
    return cancellationTokenSource;
}
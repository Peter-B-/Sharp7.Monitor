using System.Diagnostics.CodeAnalysis;
using System.Resources;
using System.Text;
using Spectre.Console.Cli;

namespace Sharp7.Monitor;

internal class Program
{
    private static readonly CancellationTokenSource cts = new();

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReadPlcCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReadPlcCommand.Settings))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Spectre.Console.Cli.ExplainCommand", "Spectre.Console.Cli")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Spectre.Console.Cli.VersionCommand", "Spectre.Console.Cli")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Spectre.Console.Cli.XmlDocCommand", "Spectre.Console.Cli")]
    public static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        try
        {
            var app =
                new CommandApp<ReadPlcCommand>()
                    .WithData(cts.Token)
                    .WithDescription("This program connects to a Siemens S7 PLC using RFC1006 and reads the variables specified.");


            app.Configure(config =>
            {
                config.AddExample("192.0.0.10 db100.int12");
                config.AddExample("192.0.0.10 --cpu 2 --rack 1 db100.int12");

                config.SetHelpProvider(new CustomHelpProvider(config.Settings));

                config.SetApplicationName(OperatingSystem.IsWindows() ? "s7mon.exe" : "s7mon");
            });

            return await app.RunAsync(args);
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        finally
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (!cts.IsCancellationRequested)
            // NOTE: cancel event, don't terminate the process
            e.Cancel = true;

        cts.Cancel();
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (cts.IsCancellationRequested)
        {
            // NOTE: SIGINT (cancel key was pressed, this shouldn't ever actually hit however, as we remove the event handler upon cancellation of the `cancellationSource`)
            return;
        }

        cts.Cancel();
    }
}

using System.Text;
using Spectre.Console.Cli;

namespace Sharp7.Monitor;

internal class Program
{
    private static readonly CancellationTokenSource cts = new();

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
                    .WithDescription("This program connects to a PLC and reads the variables specified as command line arguments.");

            app.Configure(config => { config.SetApplicationName("s7mon.exe"); });

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
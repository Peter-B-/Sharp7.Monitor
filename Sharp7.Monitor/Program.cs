using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using JetBrains.Annotations;
using Sharp7.Read;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using Spectre.Console;
using Spectre.Console.Cli;

Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += OnCancelKeyPress;
AppDomain.CurrentDomain.ProcessExit += onProcessExit;


void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    if (!cancellationSource.IsCancellationRequested)
        // NOTE: cancel event, don't terminate the process
        e.Cancel = true;

    cancellationSource.Cancel();
}

void onProcessExit(object? sender, EventArgs e)
{
    if (cancellationSource.IsCancellationRequested)
    {
        // NOTE: SIGINT (cancel key was pressed, this shouldn't ever actually hit however, as we remove the event handler upon cancellation of the `cancellationSource`)
        return;
    }

    cancellationSource.Cancel();
}

await using var t = cancellationSource.Token.Register(() => Console.WriteLine("Cancelled!"));

try
{
    var app = new CommandApp<ReadPlcCommand>();
    app.WithData(cancellationSource.Token);
    return await app.RunAsync(args);
}
finally
{
    Console.WriteLine("all done");
    AppDomain.CurrentDomain.ProcessExit -= onProcessExit;
    Console.CancelKeyPress -= OnCancelKeyPress;
}

internal sealed class ReadPlcCommand : AsyncCommand<ReadPlcCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var token = (CancellationToken) (context.Data ?? CancellationToken.None);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Establishing connection to plc [green]{settings.PlcIp}[/], CPU [green]{settings.CpuMpiAddress}[/], rack [green]{settings.RackNumber}[/].");
        using var plc = new Sharp7Plc(settings.PlcIp, settings.RackNumber, settings.CpuMpiAddress);

        await plc.InitializeAsync();

        // Connect
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .StartAsync("Connecting...", async ctx =>
            {
                var lastState = ConnectionState.Initial;
                ctx.Status(lastState.ToString());

                while (!token.IsCancellationRequested)
                {
                    var state = await plc.ConnectionState.FirstAsync(s => s != lastState).ToTask(token);
                    ctx.Status(state.ToString());

                    if (state == ConnectionState.Connected)
                        return;
                }
            });

        if (token.IsCancellationRequested)
            return 0;

        // Create a table
        var table = new Table();

        table.AddColumn("Variable");
        table.AddColumn("Value");

        foreach (var variable in settings.Variables)
        {
            table.AddRow(variable, "");
        }

        await AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                int i = 0;
                while (!token.IsCancellationRequested)
                {
                    table.Rows.Update(0, 1, new Text((++i).ToString()));
                    ctx.Refresh();
                    await Task.Delay(1000, token);

                }
            });


        
        //for (int i = 0; i < 10; i++)
        //{
        //    await plc.SetValue($"DB{db}.Int6", (short)i);
        //    var value = await plc.GetValue<short>($"DB{db}.Int6");
        //    value.Dump();

        //    await Task.Delay(200);
        //}


        //        AnsiConsole.MarkupLine($"Total file size for [green]{searchPattern}[/] files in [green]{searchPath}[/]: [blue]{totalFileSize:N0}[/] bytes");

        return 0;
    }

    [NoReorder]
    public sealed class Settings : CommandSettings
    {
        [Description("IP address of S7")]
        [CommandArgument(0, "<IP address>")]
        public string PlcIp { get; init; }

        [CommandArgument(1, "[variables]")]
        [Description("Variables to read from S7, like Db200.Int4.\r\nFor format description see https://github.com/evopro-ag/Sharp7Reactive.")]
        public string[] Variables { get; init; }

        [CommandOption("-c|--cpu")]
        [Description("CPU MPI address of S7 instance.\r\nSee https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot.\r\n")]
        [DefaultValue(0)]
        public int CpuMpiAddress { get; init; }

        [CommandOption("-r|--rack")]
        [Description("Rack number of S7 instance.\r\nSee https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot.\r\n")]
        [DefaultValue(0)]
        public int RackNumber { get; init; }

        public override ValidationResult Validate()
        {
            if (!StringHelper.IsValidIp4(PlcIp))
                return ValidationResult.Error($"\"{PlcIp}\" is not a valid IP V4 address");

            if (Variables == null || Variables.Length == 0)
                return ValidationResult.Error("Please supply at least one variable to read");

            return ValidationResult.Success();
        }
    }
}
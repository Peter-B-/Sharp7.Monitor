using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using JetBrains.Annotations;
using Sharp7.Read;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

internal sealed class ReadPlcCommand : AsyncCommand<ReadPlcCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var token = (CancellationToken) (context.Data ?? CancellationToken.None);

        try
        {
            await RunProgram(settings, token);
        }
        catch (TaskCanceledException)
        {
        }

        return 0;
    }

    private static IRenderable FormatCellData(VariableRecord record)
    {
        if (record.Value is IRenderable renderable)
            return renderable;

        if (record.Value is Exception ex)
            return new Text(ex.Message, CustomStyles.Error);

        if (record.Value is byte[] byteArray)
        {
            var text = string.Join(" ", byteArray.Select(b => $"0x{b:X2}"));
            return new Text(text);
        }

        return new Text(record.Value.ToString() ?? "");
    }

    private static async Task RunProgram(Settings settings, CancellationToken token)
    {
        AnsiConsole.MarkupLine($"Connecting to plc [green]{settings.PlcIp}[/], CPU [green]{settings.CpuMpiAddress}[/], rack [green]{settings.RackNumber}[/].");

        using var plc = new Sharp7Plc(settings.PlcIp, settings.RackNumber, settings.CpuMpiAddress);

        await plc.TriggerConnection(token);

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

        token.ThrowIfCancellationRequested();

        using var variableContainer = VariableContainer.Initialize(plc, settings.Variables);

        // Create a table
        var table = new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(foreground: Color.DarkGreen)
        };

        table.AddColumn("Variable");
        table.AddColumn("Value");

        foreach (var record in variableContainer.VariableRecords)
            table.AddRow(record.Address, "[gray]init[/]");

        await AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var record in variableContainer.VariableRecords)
                        table.Rows.Update(
                            record.RowIdx, 1,
                            FormatCellData(record)
                        );

                    ctx.Refresh();

                    await Task.Delay(100, token);
                }
            });
    }

    [NoReorder]
    public sealed class Settings : CommandSettings
    {
        [Description("IP address of S7")]
        [CommandArgument(0, "<IP address>")]
        public required string PlcIp { get; init; }

        [CommandArgument(1, "[variables]")]
        [Description("Variables to read from S7, like Db200.Int4.\r\nFor format description see https://github.com/evopro-ag/Sharp7Reactive.")]
        public required string[] Variables { get; init; }

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

public static class CustomStyles
{
    public static Style Error { get; } = new Style(foreground: Color.Red);
    public static Style Note { get; } = new(foreground: Color.DarkSlateGray1);
}

public class VariableContainer : IDisposable
{
    private readonly IDisposable subscriptions;

    private VariableContainer(IReadOnlyList<VariableRecord> variableRecords, IDisposable subscriptions)
    {
        this.subscriptions = subscriptions;
        VariableRecords = variableRecords;
    }

    public IReadOnlyList<VariableRecord> VariableRecords { get; }

    public void Dispose()
    {
        subscriptions.Dispose();
    }

    public static VariableContainer Initialize(IPlc plc, IReadOnlyList<string> variables)
    {
        var records = variables
            .Select((v, i) => new VariableRecord
            {
                Address = v,
                RowIdx = i,
                Value = new Text("init", CustomStyles.Note)
            })
            .ToList();

        var disposables = new CompositeDisposable();
        foreach (var rec in records)
        {
            try
            {
                var disp =
                    plc.CreateNotification(rec.Address, TransmissionMode.OnChange)
                        .Subscribe(
                            data => rec.Value = data,
                            ex => rec.Value = new Text(ex.Message, CustomStyles.Error)
                        );
                disposables.Add(disp);
            }
            catch (Exception e)
            {
                rec.Value = new Text(e.Message, CustomStyles.Error);
            }
        }

        return new VariableContainer(records, disposables);
    }
}

public class VariableRecord
{
    public required string Address { get; init; }
    public required int RowIdx { get; init; }
    public object Value { get; set; }
}
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using JetBrains.Annotations;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Sharp7.Monitor;

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

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[lightgoldenrod2_1]THANK YOU FOR PARTICIPATING IN THIS ENRICHMENT CENTER ACTIVITY![/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static IRenderable FormatCellData(object value)
    {
        return value switch
        {
            IRenderable renderable => renderable,
            Exception ex => new Text(ex.Message, CustomStyles.Error),
            byte[] byteArray => new Text(string.Join(" ", byteArray.Select(b => $"0x{b:X2}")), CustomStyles.Hex),
            byte => FormatNo(),
            short => FormatNo(),
            ushort => FormatNo(),
            int => FormatNo(),
            uint => FormatNo(),
            long => FormatNo(),
            ulong => FormatNo(),

            _ => new Text(value.ToString() ?? "", CustomStyles.Default)
        };

        Markup FormatNo() => new($"[lightgoldenrod2_1]0x{value:X2}[/]  {value}", CustomStyles.Default);
    }

    private static async Task RunProgram(Settings settings, CancellationToken token)
    {
        AnsiConsole.MarkupLine($"Connecting to plc [green]{settings.PlcIp}[/], CPU [green]{settings.CpuMpiAddress}[/], rack [green]{settings.RackNumber}[/]. ");
        AnsiConsole.MarkupLine("[gray]Press Ctrl + C to cancel.[/]");

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
            BorderStyle = CustomStyles.TableBorder,
        };

        table.AddColumn(new TableColumn(new Text("Variable", CustomStyles.Default)));
        table.AddColumn(new TableColumn(new Text( "Value", CustomStyles.Default)));

        foreach (var record in variableContainer.VariableRecords)
            table.AddRow(new Text( record.Address, CustomStyles.Default), new Text("init", CustomStyles.Note));

        await AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var record in variableContainer.VariableRecords)
                        if (record.HasUpdate(out var value))
                            table.Rows.Update(
                                record.RowIdx, 1,
                                FormatCellData(value)
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
        [Description("Variables to read from S7, like Db200.Int4")]
        public required string[] Variables { get; init; }

        [CommandOption("-c|--cpu")]
        [Description("CPU MPI address of S7 instance")]
        [DefaultValue(0)]
        public int CpuMpiAddress { get; init; }

        [CommandOption("-r|--rack")]
        [Description("Rack number of S7 instance")]
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

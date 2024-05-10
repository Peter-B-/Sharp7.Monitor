using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace Sharp7.Monitor;

internal class CustomHelpProvider(ICommandAppSettings settings) : HelpProvider(settings)
{
    private readonly ICommandAppSettings settings = settings;

    public override IEnumerable<IRenderable> GetFooter(ICommandModel model, ICommandInfo? command)
    {
        var helpStyles = settings.HelpProviderStyles?.Options;

        var variableGrid = GetVariableExamples(helpStyles);

        return
        [
            .. base.GetFooter(model, command),

            new Text("VARIABLE EXAMPLES:", helpStyles?.Header ?? Style.Plain), Text.NewLine,
            new Text("For a detailed format description see https://github.com/evopro-ag/Sharp7Reactive."), Text.NewLine,
            Text.NewLine,
            variableGrid,
            Text.NewLine
        ];
    }

    public override IEnumerable<IRenderable> GetOptions(ICommandModel model, ICommandInfo? command) =>
    [
        ..base.GetOptions(model, command),
        Text.NewLine,
        new Text("You can find details on rack and slot at https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot."), Text.NewLine,
        new Text("If you are using S7 1200 or 1500, you must explicitly allow RFC1006. See https://github.com/fbarresi/Sharp7#s7-12001500-notes."), Text.NewLine,
        Text.NewLine
    ];

    private static IRenderable GetVariableExamples(OptionStyle? helpStyles)
    {
        IReadOnlyList<VariableType> variableTypes =
        [
            new VariableType("bit", "single bit as boolean", "[grey]db3[/].[lightgoldenrod2_1]bit[/][grey]10[/].2"),
            new VariableType("byte", "single byte", "[grey]db3[/].[lightgoldenrod2_1]byte[/][grey]10[/]"),
            new VariableType("", "byte array", "[grey]db3[/].[lightgoldenrod2_1]byte[/][grey]10[/].5"),
            new VariableType("int", "16 bit signed integer", "[grey]db3[/].[lightgoldenrod2_1]int[/][grey]10[/]"),
            new VariableType("uint", "16 bit unsigned integer", "[grey]db3[/].[lightgoldenrod2_1]uint[/][grey]10[/]"),
            new VariableType("dint", "32 bit signed integer", "[grey]db3[/].[lightgoldenrod2_1]dint[/][grey]10[/]"),
            new VariableType("udint", "32 bit unsigned integer", "[grey]db3[/].[lightgoldenrod2_1]udint[/][grey]10[/]"),
            new VariableType("lint", "64 bit signed integer", "[grey]db3[/].[lightgoldenrod2_1]lint[/][grey]10[/]"),
            new VariableType("ulint", "64 bit unsigned integer", "[grey]db3[/].[lightgoldenrod2_1]ulint[/][grey]10[/]"),
            new VariableType("real", "32 bit float", "[grey]db3[/].[lightgoldenrod2_1]real[/][grey]10[/]"),
            new VariableType("lreal", "64 bit float", "[grey]db3[/].[lightgoldenrod2_1]lreal[/][grey]10[/]"),
            new VariableType("string", "ASCII text string", "[grey]db3[/].[lightgoldenrod2_1]string[/][grey]10[/].16"),
            new VariableType("wstring", "UTF-16 text string", "[grey]db3[/].[lightgoldenrod2_1]wstring[/][grey]10[/].16"),
        ];

        var grid = new Grid();
        grid.AddColumn(new GridColumn {Padding = new Padding(4, 4), NoWrap = true});
        grid.AddColumn(new GridColumn {Padding = new Padding(0, 0, 4, 0)});
        grid.AddColumn(new GridColumn {Padding = new Padding(0)});

        grid.AddRow(
            new Text("Identifier", helpStyles?.DefaultValueHeader ?? Style.Plain),
            new Text("Description", helpStyles?.DefaultValueHeader ?? Style.Plain),
            new Text("Example", helpStyles?.DefaultValueHeader ?? Style.Plain)
        );

        foreach (var variableType in variableTypes)
            grid.AddRow(
                new Text(variableType.Identifier),
                new Text(variableType.Description),
                new Markup(variableType.Example)
            );
        return grid;
    }

    internal record VariableType(string Identifier, string Description, string Example);
}

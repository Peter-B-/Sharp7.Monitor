using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sharp7.Monitor;

internal static class RenderUtil
{
    public static IRenderable FormatCellData(object value)
    {
        return value switch
        {
            IRenderable renderable => renderable,
            Exception ex => new Text(ex.Message, CustomStyles.Error),
            byte[] byteArray => FormatByteArray(byteArray),
            byte => FormatNo(),
            short => FormatNo(),
            ushort => FormatNo(),
            int => FormatNo(),
            uint => FormatNo(),
            long => FormatNo(),
            ulong => FormatNo(),

            _ => new Text(value.ToString() ?? "", CustomStyles.Default)
        };

        IRenderable FormatNo() => new Paragraph()
            .Append($"0x{value:X2}", CustomStyles.Hex)
            .Append("   ")
            .Append($"{value}", CustomStyles.Default)
        ;

        IRenderable FormatByteArray(byte[] byteArray) =>
            new Paragraph()
                .Append("0x " + string.Join(" ", byteArray.Select(b => $"{b:X2}")), CustomStyles.Hex)
                .Append(Environment.NewLine)
                .Append(new string(
                            byteArray
                                .Select(b => (char) b)
                                .Select(c => char.IsControl(c) ? '·' : c)
                                .ToArray()
                        )
                );
    }
}

using Spectre.Console;

namespace Sharp7.Monitor;

public static class CustomStyles
{
    public static Style Default { get; } = new(background:Color.Black);

    public static Style Error { get; } = Default.Foreground(Color.Red);
    public static Style Hex { get; } = Default.Foreground(Color.Blue);
    public static Style Note { get; } = Default.Foreground(Color.DarkSlateGray1);
    public static Style TableBorder { get;  } = Default.Foreground(Color.DarkGreen);
}

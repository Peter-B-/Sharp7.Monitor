using Spectre.Console;

public static class CustomStyles
{
    public static Style Error { get; } = new Style(foreground: Color.Red);
    public static Style Note { get; } = new(foreground: Color.DarkSlateGray1);
}
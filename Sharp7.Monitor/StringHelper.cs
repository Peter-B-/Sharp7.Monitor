namespace Sharp7.Read;

public static class StringHelper
{
    public static bool IsValidIp4(string? ipString)
    {
        if (string.IsNullOrWhiteSpace(ipString))
            return false;

        var splitValues = ipString.Split('.');
        return splitValues.Length == 4 && splitValues.All(r => byte.TryParse(r, out _));
    }
}
using System.Diagnostics.CodeAnalysis;

namespace Sharp7.Monitor;

public class VariableRecord
{
    private object value = new();
    private volatile int valueUpdated;
    public required string Address { get; init; }
    public required int RowIdx { get; init; }

    public object Value
    {
        get => value;
        set
        {
            if (!IsEquivalent(this.value, value))
            {
                this.value = value;
                valueUpdated = 1;
            }
        }
    }

    private bool IsEquivalent(object oldValue, object newValue)
    {
        // Special treatmant for byte arrays
        if (oldValue is byte[] oldArray && newValue is byte[] newArray)
        {
            if (oldArray.Length != newArray.Length) return false;

            for (var i = 0; i < oldArray.Length; i++)
                if (oldArray[i] != newArray[i]) return false;

            return true;
        }

        // all other types read from PLC have value compare semantics. 
        return oldValue == newValue;
    }

    public bool HasUpdate([NotNullWhen(true)] out object? newValue)
    {
        if (Interlocked.Exchange(ref valueUpdated, 0) == 1)
        {
            newValue = value;
            return true;
        }
        else
        {
            newValue = null;
            return false;
        }
    }
}

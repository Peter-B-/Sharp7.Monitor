public class VariableRecord
{
    public required string Address { get; init; }
    public required int RowIdx { get; init; }
    public object Value { get; set; }
}
using System.Reactive.Disposables;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;
using Spectre.Console;

namespace Sharp7.Monitor;

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
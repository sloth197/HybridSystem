using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Interfaces;

namespace HybridPortfolio.Sim;

public sealed class PlcStateMachine : IPlcStateMachine
{
    public PlcState CurrentState { get; private set; } = PlcState.Idle;
    public string LastResultSignal { get; private set; } = string.Empty;
    public event Action<PlcState>? StateChanged;

    public void TriggerInspect()
    {
        CurrentState = PlcState.Inspect;
        StateChanged?.Invoke(CurrentState);
    }

    public void SetResult(string resultSignal)
    {
        LastResultSignal = resultSignal;
        CurrentState = PlcState.Result;
        StateChanged?.Invoke(CurrentState);
    }

    public void ReturnToIdle()
    {
        CurrentState = PlcState.Idle;
        StateChanged?.Invoke(CurrentState);
    }
}

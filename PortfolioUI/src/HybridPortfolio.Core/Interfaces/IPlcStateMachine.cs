using HybridPortfolio.Core.Enums;

namespace HybridPortfolio.Core.Interfaces;

public interface IPlcStateMachine
{
    PlcState CurrentState { get; }
    string LastResultSignal { get; }
    event Action<PlcState>? StateChanged;

    void TriggerInspect();
    void SetResult(string resultSignal);
    void ReturnToIdle();
}

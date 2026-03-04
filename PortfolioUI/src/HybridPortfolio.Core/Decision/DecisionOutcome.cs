using HybridPortfolio.Core.Enums;

namespace HybridPortfolio.Core.Decision;

public readonly record struct DecisionOutcome(InspectionFinalResult FinalResult, string Reason);

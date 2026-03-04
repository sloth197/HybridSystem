using HybridPortfolio.Core.Enums;

namespace HybridPortfolio.Core.Decision;

public sealed class DecisionEngine
{
    public double DefectThreshold { get; set; } = 0.55d;

    public DecisionOutcome Decide(DecisionInput input)
    {
        if (!input.Barcode.Success)
        {
            return new DecisionOutcome(
                InspectionFinalResult.Ng,
                "Barcode read failed");
        }

        if (input.Shape.IsDefect && input.Shape.Score >= DefectThreshold)
        {
            return new DecisionOutcome(
                InspectionFinalResult.Ng,
                $"Shape defect detected: {input.Shape.Label} ({input.Shape.Score:F2})");
        }

        if (input.Barcode.Value.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            return new DecisionOutcome(
                InspectionFinalResult.Ng,
                "Barcode lot marked as FAIL");
        }

        return new DecisionOutcome(
            InspectionFinalResult.Ok,
            "All checks passed");
    }
}

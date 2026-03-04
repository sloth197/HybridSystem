using HybridPortfolio.Core.Decision;
using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Tests;

public sealed class DecisionEngineTests
{
    private readonly DecisionEngine _sut = new();

    [Fact]
    public void BarcodeReadFail_ReturnsNg()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: false, value: string.Empty),
            Shape(isDefect: false, label: "Normal", score: 0.10)));

        Assert.Equal(InspectionFinalResult.Ng, outcome.FinalResult);
        Assert.Contains("Barcode read failed", outcome.Reason);
    }

    [Fact]
    public void ShapeDefectAboveThreshold_ReturnsNg()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: true, value: "PRD-OK"),
            Shape(isDefect: true, label: "Crack", score: 0.82)));

        Assert.Equal(InspectionFinalResult.Ng, outcome.FinalResult);
        Assert.Contains("Shape defect detected", outcome.Reason);
    }

    [Fact]
    public void ShapeDefectAtThreshold_ReturnsNg()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: true, value: "PRD-OK"),
            Shape(isDefect: true, label: "Scratch", score: 0.55)));

        Assert.Equal(InspectionFinalResult.Ng, outcome.FinalResult);
    }

    [Fact]
    public void BarcodeContainsFail_Uppercase_ReturnsNg()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: true, value: "PRD-240101-1000-FAIL"),
            Shape(isDefect: false, label: "Normal", score: 0.12)));

        Assert.Equal(InspectionFinalResult.Ng, outcome.FinalResult);
        Assert.Contains("marked as FAIL", outcome.Reason);
    }

    [Fact]
    public void BarcodeContainsFail_Lowercase_ReturnsNg()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: true, value: "prd-240101-1000-fail"),
            Shape(isDefect: false, label: "Normal", score: 0.12)));

        Assert.Equal(InspectionFinalResult.Ng, outcome.FinalResult);
    }

    [Fact]
    public void DefectBelowThreshold_AndBarcodeOk_ReturnsOk()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: true, value: "PRD-240101-1000-OK"),
            Shape(isDefect: true, label: "Scratch", score: 0.44)));

        Assert.Equal(InspectionFinalResult.Ok, outcome.FinalResult);
        Assert.Equal("All checks passed", outcome.Reason);
    }

    [Fact]
    public void NoDefectAndBarcodeOk_ReturnsOk()
    {
        var outcome = _sut.Decide(new DecisionInput(
            Barcode(success: true, value: "PRD-240101-1000-OK"),
            Shape(isDefect: false, label: "Normal", score: 0.08)));

        Assert.Equal(InspectionFinalResult.Ok, outcome.FinalResult);
    }

    private static BarcodeReadResult Barcode(bool success, string value)
    {
        return new BarcodeReadResult
        {
            Success = success,
            Value = value,
            Confidence = success ? 0.95 : 0.32
        };
    }

    private static ShapeInspectionResult Shape(bool isDefect, string label, double score)
    {
        return new ShapeInspectionResult
        {
            IsDefect = isDefect,
            Label = label,
            Score = score
        };
    }
}

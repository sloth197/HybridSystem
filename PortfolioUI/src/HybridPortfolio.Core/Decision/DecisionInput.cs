using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Decision;

public readonly record struct DecisionInput(BarcodeReadResult Barcode, ShapeInspectionResult Shape);

using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Interfaces;

public interface IShapeInspector
{
    ShapeInspectionResult Inspect(VirtualObjectFrame frame);
}

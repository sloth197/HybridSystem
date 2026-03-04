using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Interfaces;

public interface IBarcodeReader
{
    BarcodeReadResult Read(VirtualObjectFrame frame);
}

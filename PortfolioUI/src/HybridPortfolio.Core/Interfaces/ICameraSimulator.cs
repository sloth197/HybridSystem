using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Interfaces;

public interface ICameraSimulator
{
    VirtualObjectFrame CaptureFrame();
}

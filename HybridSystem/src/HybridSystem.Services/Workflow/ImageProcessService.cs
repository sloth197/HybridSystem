using HybridSystem.Core.Models;
using HybridSystem.Core.Barcode;
using HybridSystem.Core.OCR;
using HybridSystem.Core.AI;
using HybridSystem.Core.Decision;
using HybridSystem.Core.Utils;
using System;

namespace HybridSystem.Services.Workflow
{
    public class ImageProcessService : IDisposable
    {
        private readonly BarcodeReader _barcode = new ();
        private readonly OcrEngine _ocr = new();
        private readonly ImageClassifier _ai;
        private readonly DecisionEngine _decision = new();

        public ImageProcessService()
        {
            _ai = new ImageClassifier();
        }
        public ClassificationResult Process(FileItem file)
        {
            Logger.Log($"Processing: (file.FilePath)");
            var barcode = _barcodeReadBarcode(file);
            var ocr = _ocr.ReadText(file);
            var ai = _ai.Predict(file);
            var result = _decision.Decide(barcode, ocr, ai);
            Logger.Log($"Result: {result.FinalLabel} Source: {result.Source} Conf: {result.Confidence:F3}");
            return result;
        }
        public void Dispose() => _ai?.Dispose();
    }
}
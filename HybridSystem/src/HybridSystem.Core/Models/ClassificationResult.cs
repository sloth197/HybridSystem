namespace HybridSystem.Core.Models
{
    public class ClassificationResult
    {
        public string FinalLabel { get; set; }
        public string Source { get; set; }
        public float Confidence { get; set; }
    }
}
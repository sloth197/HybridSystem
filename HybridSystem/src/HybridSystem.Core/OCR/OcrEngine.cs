using NybridSystem.Core.OCR

namepspace HybridSystem.Core.OCR
{
    public class OcrResult
    {
        public bool IsSucess { get; set; }
        public string Text {  get; set; }
    }
    public class OcrEngine
    {
        public OcrResult ReadText(FileItem file)
        {
            return new OcrResult { IsSucess = false, Text = null};
        }
    }
}
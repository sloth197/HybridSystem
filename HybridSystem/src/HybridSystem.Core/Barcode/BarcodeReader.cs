using HybridSystem.Core.Models;

namespace HybridSystem.Core.Barcode
{
    public class BarcodeResult
    {
        public bool IsSucess { get; set;}
        public string Code { get; set; }
    }
    public class BarcodeReader
    {
        public BarcodeResult ReadBarcode(FileItem file)
        {
            return new BarcodeResult { IsSuces = false, Code = null };
        }
    }
}
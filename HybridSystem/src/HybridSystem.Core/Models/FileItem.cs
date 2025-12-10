using System;

namespace HybridSystem.Core.Models
{
    public class FileItem
    {
        public string FilePath{ get; set; }
        public string DateTime CreatedAt { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
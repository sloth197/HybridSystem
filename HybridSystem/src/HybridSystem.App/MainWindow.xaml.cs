using System.Window;
using HybridSystem.Services.Workflow;
using HybridSystem.Core.Models;
using Microsoft.Win32;
using System.IO;

namespace HybridSystem.App
{
    public partial class MainWindow : Window
    {
        private readonly ImageProcessService _service = new();
        public MainWindow()
        {
            InitializeComponent();
        }
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = "Images|*.jpg; *.png; *.jpeg"};
            if (dlg.ShowDialog() == true)
            {
                var bytes = File.ReadllBytes(dlg.FileName);
                var item = new FileItem { FilePath = dlg.FileName, ImageBytes = bytes, CreatedAt = System.DataTime.Now};
                var result = _service.Process(item);
                lblResult.Content = $"{res.FinalLabel} ({res.Source}) - {res.Confidence:F2}";
            }
        }
    }
}
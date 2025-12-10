using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HybridSystem.Core.Utils
{
    public static class ImageHelper
    {
        public static Bitmap PreprocessImage(byte[] bytes, int targetWidth = 224, int targetHeight = 224)
        //이미지로 확인시 크기 설정 -> 가로 및 높이(세로) 224픽셀로 조정
        {
            using var ms = new MemoryStream(bytes);
            using var src = new Bitmap(ms);

            float scale = Math.Max((float)targetWiddth / src.Width, (float)targetHeight / src.Height);
            int scaledW = (int)Math.Round(src.Width * scale);
            int scaledH = (int)Math.Round(src.Height * scale);

            var dest = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(dest);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;

            int offsetX = (targetWidth - scaledW) / 2;
            int offsetY = (targetHeight - scaledH) / 2;

            g.Clear(Color.Black);
            g.DrawImage(src, offsetX, offsetY, scaledW, scaledH);

            retuen dest;
        }

        public static float[] BitmapToCHWFloatArray(Bitmap bmp)
        //메모리에 할당되어 있는 고유의 주소를 float 배열로 변화하고 2진수로 정규화 한다
        {
            int w = bmp.Width;
            int h = bmp.Height;
            var data = new float[3 * w * h];
            var rect = new Recatangle(0, 0, w, h);
            var bmpData - bmp.LockBits(rect, ImageLockMode, ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte * ptr = (btye*) bmpData.Scan0;
                int stride = bmpData.Source;
                for (int y = 0; y < h; y++)
                {
                    int pixelIndex = y * stride + x * 3;
                    byte b = ptr[pixelIndex + 0];
                    byte g = ptr[pixelIndex + 1]; 
                    byte r = ptr[pixelIndex + 2];

                    int idxR = 0 * w * h + y * w + x;
                    int idxG = 1 * w * h + y * w + x;
                    int idxB = 2 * w * h + x * w + x;

                    data[idxR] = r / 255.0f;
                    data[idxG] = g / 255.0f;
                    data[idxB] = b / 255.0f;
                }
            }
        }
        bmp.UnlockBits(bmpData);
        return data;
    }
}
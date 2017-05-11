using System;
using System.Drawing;
using System.IO;

namespace HtmZetaOneDemos
{
    public static class Mnist
    {
        public const int W = 28;
        public const int H = 28;

        public class Digit
        {
            public byte[,] Pixels; // 0(white) - 255(black)
            public byte Label; // '0' - '9'

            public Digit(byte[,] pixels, byte label)
            {
                Pixels = new byte[H, W];
                Array.Copy(pixels, Pixels, H * W);
                Label = label;
            }
        }

        public static Digit[] LoadData(string imageFile, string labelFile)
        {
            const int numImages = 60000;
            var result = new Digit[numImages];
            var pixels = new byte[H, W];
            var fsPixels = new FileStream(imageFile, FileMode.Open);
            var fsLabels = new FileStream(labelFile, FileMode.Open);
            var brImages = new BinaryReader(fsPixels);
            var brLabels = new BinaryReader(fsLabels);
            // skip header
            brImages.ReadBytes(16);
            brLabels.ReadBytes(8);
            for (var di = 0; di < numImages; ++di)
            {
                // get 28x28 pixel values
                for (var i = 0; i < H; ++i)
                {
                    for (var j = 0; j < W; ++j)
                    {
                        var b = brImages.ReadByte();
                        pixels[i, j] = b;
                    }
                }
                // get the label
                var label = brLabels.ReadByte();
                var digit = new Digit(pixels, label);
                result[di] = digit;
            }
            fsPixels.Close();
            brImages.Close();
            fsLabels.Close();
            brLabels.Close();
            return result;
        }

        public static Bitmap ToBitmap(this Digit digit, int magnitude = 1)
        {
            var width = W * magnitude;
            var height = H * magnitude;
            var result = new Bitmap(width, height);
            var gr = Graphics.FromImage(result);
            for (var i = 0; i < H; ++i)
            {
                for (var j = 0; j < W; ++j)
                {
                    var pixelColor = 255 - digit.Pixels[i, j]; // black digits
                    var c = Color.FromArgb(pixelColor, pixelColor, pixelColor);
                    var sb = new SolidBrush(c);
                    gr.FillRectangle(sb, j * magnitude, i * magnitude, magnitude, magnitude);
                }
            }
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace HtmZetaOneDemos
{
    public static class Mnist
    {
        const int H = 28;
        const int W = 28;

        public class Digit
        {
            public byte[,] Pixels;
            public byte Label;

            public Digit(byte[,] pixels, byte label, int n = 2)
            {
                Pixels = new byte[H, W];
                for (var i = 0; i < H; i++)
                {
                    for (var j = 0; j < W; j++)
                    {
                        Pixels[i, j] = (byte) (pixels[i, j] / (256 / n) * (256 / n));
                    }
                }
                Label = label;
            }

            public Bitmap ToBitmap() => Pixels.ToBitmap();
        }

        public class Screen
        {
            public readonly byte[,] Pixels;
            public int Height => Pixels.GetLength(0);
            public int Width => Pixels.GetLength(1);

            public Screen(int w, int h)
            {
                Pixels = new byte[h, w];
            }

            public void Locate(Digit digit, int x, int y)
            {
                for (var i = 0; i < H; i++)
                {
                    if (y + i < 0 || y + i > Height - 1) continue;
                    for (var j = 0; j < W; j++)
                    {
                        if (x + j < 0 || x + j > Width - 1) continue;
                        Pixels[i + y, j + x] = digit.Pixels[i, j];
                    }
                }
            }

            public void Save(string path = null, ImageFormat format = null, int magnitude = 1)
            {
                path = path ?? DateTime.Now.ToLongDateString();
                format = format ?? ImageFormat.Bmp;
                Pixels.ToBitmap(magnitude).Save(path, format);
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

        public static byte[,] DeepClone(this byte[,] self)
        {
            var h = self.GetLength(0);
            var w = self.GetLength(1);
            var pixels = new byte[h, w];
            Array.Copy(self, pixels, h * w);
            return pixels;
        }

        public static byte[,] Clip(this byte[,] self, int x, int y, int w, int h)
        {
            var results = new byte[h, w];
            for (var i = 0; i < h; i++)
            {
                for (var j = 0; j < w; j++)
                {
                    results[i, j] = self[y + h, x + w];
                }
            }
            return results;
        }

        public static Bitmap ToBitmap(this byte[,] pixels, int magnitude = 1)
        {
            var width = pixels.GetLength(1);
            var height = pixels.GetLength(0);
            var result = new Bitmap(width * magnitude, height * magnitude);
            var gr = Graphics.FromImage(result);
            for (var i = 0; i < height; ++i)
            {
                for (var j = 0; j < width; ++j)
                {
                    var pixelColor = 255 - pixels[i, j]; // black digits
                    var c = Color.FromArgb(pixelColor, pixelColor, pixelColor);
                    var brush = new SolidBrush(c);
                    gr.FillRectangle(brush, j * magnitude, i * magnitude, magnitude, magnitude);
                }
            }
            return result;
        }
    }
}
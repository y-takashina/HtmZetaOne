using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmZetaOneDemos
{
    class DigitClassification
    {
        public static void Main(string[] args)
        {
            var root = Path.Combine("..", "..", "data", "mnist");
            var pathImage = Path.Combine(root, "train-images-idx3-ubyte");
            var pathLabel = Path.Combine(root, "train-labels-idx1-ubyte");
            var digits = Mnist.LoadData(pathImage, pathLabel);
            for (var i = 0; i < 5; i++)
            {
                digits[i].ToBitmap().Save(i + "test.png", ImageFormat.Png);
                Console.WriteLine(digits[i].Label);
            }
            Console.WriteLine("Push any key to finish...");
            Console.ReadKey();
        }
    }
}
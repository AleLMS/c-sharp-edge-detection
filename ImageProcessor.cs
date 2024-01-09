using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Threading.Tasks;

namespace PolychromeToGreyscale
{
    internal class ImageProcessor
    {
        /// <summary>
        /// Fast image manipulation method via direct byte manipulation. !!THE INPUT BITMAP MUST HAVE A PIXELFORMAT OF 32bppArgb!!
        /// </summary>
        public static Bitmap ConvertToGreyscaleFast(ref Bitmap bitmap)
        {
            // Setup variables
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            const int BytesPerPixel = 4;                                                // Each pixel takes 1 byte of data per channel
            int[] bgra = new int[4];                                                    // RGBA values, RGB is stored in reverse (?) (BGR)
            int pixelAverageColor;

            Rectangle canvas = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(canvas, ImageLockMode.ReadWrite, pixelFormat);

            unsafe
            {
                byte* bitmapPointer = (byte*)bitmapData.Scan0;
                for (int h = 0; h < bitmap.Height; h++)                                 // Select column
                {
                    for (int w = 0; w < bitmap.Width; w++,                              // Select pixel within column
                         bitmapPointer += BytesPerPixel)                                //Move to the next pixel
                    {
                        for (int i = 0; i < bgra.Length - 1; i++)                       // Loop through the RGBA values of the given pixel
                        {
                            bgra[i] = Convert.ToInt32(bitmapPointer[i]);                // Read BGR values
                            pixelAverageColor = (bgra[0] + bgra[1] + bgra[2]) / 3;      // Calculate pixel monochrome (average) value
                            bitmapPointer[i] = Convert.ToByte(pixelAverageColor);       // Set BGR values
                        }
                    }
                }
            }
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        /// <summary>
        /// Change Bitmap pixel format.
        /// </summary>
        public static Bitmap SetPixelFormat(ref Bitmap input, PixelFormat pixelFormat)
        {
            if (input.PixelFormat == pixelFormat) return input;
            using (Graphics g = Graphics.FromImage(input))
            {
                g.DrawImage(input, new Rectangle(0, 0, input.Width, input.Height));
            }
            return input;
        }

        /// <summary>
        /// Convert Bitmap to a 2D jagged byte array[X,Y][BGRA]
        /// </summary>
        public static Byte[,][] BitmapToJaggedByteArray(Bitmap input)
        {
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            const int BytesPerPixel = 4; // Each pixel takes 1 byte of data per channel

            Rectangle canvas = new Rectangle(0, 0, input.Width, input.Height);
            BitmapData inputData = input.LockBits(canvas, ImageLockMode.ReadOnly, pixelFormat);

            Byte[,][] result = new byte[input.Width, input.Height][];
            for (int a = 0; a < input.Width; a++)
            {
                for (int b = 0; b < input.Height; b++)
                {
                    result[a, b] = new byte[BytesPerPixel];
                }
            }

            unsafe
            {
                int byteArrayWithIndex = 0;
                byte* inputPointer = (byte*)inputData.Scan0;

                for (int h = 0; h < input.Height; h++)
                {
                    for (int w = 0; w < input.Width; w++,
                        inputPointer += BytesPerPixel,
                        byteArrayWithIndex += BytesPerPixel)
                    {
                        if (byteArrayWithIndex >= input.Width * BytesPerPixel) byteArrayWithIndex = 0;
                        for (int i = 0; i < BytesPerPixel; i++)
                        {
                            result[w, h][i] = inputPointer[i];
                        }
                    }
                } 
            }

            input.UnlockBits(inputData);
            input.Dispose();
            return result;
        }

        /// <summary>
        /// Convert a jagged byte array[X,Y][BGRA] to a Bitmap
        /// </summary>
        public static Bitmap JaggedByteArrayToBitmap(byte[,][] input)
        {
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            const int BytesPerPixel = 4;  // Each pixel takes 1 byte of data per channel
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            Bitmap output = new Bitmap(width, height, pixelFormat);
            Rectangle canvas = new Rectangle(0, 0, width, height);
            BitmapData outputData = output.LockBits(canvas, ImageLockMode.ReadOnly, pixelFormat);

            unsafe
            {
                byte* outputPointer = (byte*)outputData.Scan0;

                int byteArrayWidthIndex = 0;

                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++,
                        outputPointer += BytesPerPixel,
                        byteArrayWidthIndex += BytesPerPixel)
                    {
                        if (byteArrayWidthIndex >= width * BytesPerPixel) byteArrayWidthIndex = 0;
                        for (int i = 0; i < BytesPerPixel; i++)
                        {
                            outputPointer[i] = input[w, h][i];
                        }
                    }
                }
            }
            output.UnlockBits(outputData);
            return output;
        }

        /// <summary>
        /// Detect edges. Input must be in a jagged 2d array[X,Y][BGRA]
        /// </summary>
        public static Byte[,][] SobelEdgeDetection(byte[,][] input, int threshold)
        {
            // SET UP VARIABLES
            const int BytesPerPixel = 4;                                                // Each pixel takes 1 byte of data per channel
            int width = input.GetLength(0);
            int height = input.GetLength(1);

            double[,] sobelX = new double[,]                                            // X Sobel matrix
            {
                { -1, 0, +1 },
                { -2, 0, +2 },
                { -1, 0, +1 }
            };

            double[,] sobelY = new double[,]                                            // Y Sobel Matrix
            {
                { +1, +2, +1 },
                {  0,  0,  0 },
                { -1, -2, -1 }
            };

            byte[,][] sobelFilteredBytes = new byte[width, height][];                   // 2D Jagged array [X,Y][BGRA]
            for (int h = 0; h < height; h++)                                            // 
            {                                                                           //
                for (int w = 0; w < width; w++)                                         //
                {                                                                       // Create the BGRA arrays
                    sobelFilteredBytes[w, h] = new byte[BytesPerPixel];                 // for each pixel
                }                                                                       //
            }               
            

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(1, height - 1, parallelOptions, h =>                           // Select row and work on it async, might not be worth the parallelization
            {
                for (int w = 1; w < width - 1; w++)                                     // Select pixel within row
                {
                    double sobelClrX = 0;
                    double sobelClrY = 0;
                    for (int iY = -1; iY <= 1; iY++)                                    // Sobel matrix Y selection
                    {
                        for (int iX = -1; iX <= 1; iX++)                                // Sobel matrix X selection
                        {
                            double bClr = Convert.ToDouble(input[w + iX, h + iY][0]);   // Blue channel
                            double gClr = Convert.ToDouble(input[w + iX, h + iY][1]);   // Green channel
                            double rClr = Convert.ToDouble(input[w + iX, h + iY][2]);   // Red channel
                            double avgClr = (rClr + gClr + bClr) / 3;                   // Greyscale (average of RGB channels)

                            sobelClrX += avgClr * sobelX[iX + 1, iY + 1];               // X Sobel
                            sobelClrY += avgClr * sobelY[iX + 1, iY + 1];               // Y Sobel
                        }
                    }

                    if (sobelClrY < threshold) sobelClrY = 0;                           // Discard values below
                    if (sobelClrX < threshold) sobelClrX = 0;                           // the given threshold

                    double sobelClr = Math.Sqrt(Math.Pow(sobelClrX, 2) + Math.Pow(sobelClrY, 2));
                    if (sobelClr > 255) sobelClr = 255;                                 // Clamp value

                    for (int i = 0; i <= 2; i++)                                        // Assign sobel result to the
                    { sobelFilteredBytes[w, h][i] = Convert.ToByte(sobelClr); }         // BGR channels
                    sobelFilteredBytes[w, h][3] = input[w, h][3];                       // Preserve original alpha
                }
            });

            #region slower(?)
            /*for (int h = 1; h < height - 1; h++)                                      // Select row
            {
                for (int w = 1; w < width - 1; w++)                                     // Select pixel within row
                {
                    double sobelClrX = 0;
                    double sobelClrY = 0;
                    for (int iY = -1; iY <= 1; iY++)                                    // Sobel matrix Y selection
                    {
                        for (int iX = -1; iX <= 1; iX++)                                // Sobel matrix X selection
                        {
                            double bClr = Convert.ToDouble(input[w + iX, h + iY][0]);   // Blue channel
                            double gClr = Convert.ToDouble(input[w + iX, h + iY][1]);   // Green channel
                            double rClr = Convert.ToDouble(input[w + iX, h + iY][2]);   // Red channel
                            double avgClr = (rClr + gClr + bClr) / 3;                   // Greyscale (average of RGB channels)

                            sobelClrX += avgClr * sobelX[iX + 1, iY + 1];               // X Sobel
                            sobelClrY += avgClr * sobelY[iX + 1, iY + 1];               // Y Sobel
                        }
                    }

                    if (sobelClrY < threshold) sobelClrY = 0;                           // Discard values below
                    if (sobelClrX < threshold) sobelClrX = 0;                           // the given threshold

                    double sobelClr = Math.Sqrt(Math.Pow(sobelClrX, 2) + Math.Pow(sobelClrY, 2));
                    if (sobelClr > 255) sobelClr = 255;                                 // Clamp value

                    for (int i = 0; i <= 2; i++)                                        // Assign sobel result to the
                        { sobelFilteredBytes[w, h][i] = Convert.ToByte(sobelClr); }     // BGR channels
                    sobelFilteredBytes[w, h][3] = input[w, h][3];                       // Preserve original alpha
                }
            }*/
            #endregion
            return sobelFilteredBytes;
        }

        /// <summary>
        /// Run sobel filter on a Bitmap
        /// </summary>
        public static Bitmap SobelFilterBitmap(Bitmap input, int threshold)
        {
            SetPixelFormat(ref input, PixelFormat.Format32bppArgb);
            Byte[,][] byteArr = BitmapToJaggedByteArray(input);
            input.Dispose();
            byteArr = SobelEdgeDetection(byteArr, threshold);
            return JaggedByteArrayToBitmap(byteArr);
        }

        // DEPRECATE BELOW THIS POINT
        /// <summary>
        /// Slow image manipulation method. High compatibility.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pixelFormat"></param>
        /// <returns></returns>
        [Obsolete("", false)]
        public static Bitmap ConvertToGreyscale(Bitmap input, PixelFormat pixelFormat)
        {
            // Setup
            int x;
            int y;
            int pixelAverageColor;

            Bitmap output = new Bitmap(input.Width, input.Height, pixelFormat);

            for (x = 0; x < input.Width; x++)                                                                                       // Select row
            {
                for (y = 0; y < input.Height; y++)                                                                                  // Select pixel within row
                {
                    Color pixelColor = input.GetPixel(x, y);                                                                        // Get pixel color 
                    pixelAverageColor = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;                                           // Calculate greyscale (average of RGB)
                    Color greyscaleColor = Color.FromArgb(pixelColor.A, pixelAverageColor, pixelAverageColor, pixelAverageColor);   // Set pixel to grayscale color, preserve alpha
                    output.SetPixel(x, y, greyscaleColor);
                }
            }
            input.Dispose();
            return output;
        }
        public static Byte[,] BitmapToTwoDimensionalByteArray(Bitmap input)
        {
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            const int bytesPerPixel = 4; // Each pixel takes 1 byte of data per channel

            Rectangle canvas = new Rectangle(0, 0, input.Width, input.Height);
            BitmapData inputData = input.LockBits(canvas, ImageLockMode.ReadOnly, pixelFormat);

            Byte[,] result = new byte[input.Width * bytesPerPixel, input.Height];

            unsafe
            {
                int bytearrpix = 0;
                byte* inputPointer = (byte*)inputData.Scan0;
                int totalpixels = 0;

                for (int h = 0; h < input.Height; h++)
                {
                    for (int w = 0; w < input.Width; w++,
                        inputPointer += bytesPerPixel,
                        bytearrpix += bytesPerPixel,
                        totalpixels++)
                    {

                        if (bytearrpix >= input.Width * bytesPerPixel) bytearrpix = 0;
                        for (int i = 0; i < bytesPerPixel; i++)
                        {
                            result[bytearrpix + i, h] = inputPointer[i];
                        }
                    }
                }
            }

            input.UnlockBits(inputData);
            input.Dispose();
            return result;
        }
        [Obsolete("", false)]
        public static Bitmap TwoDimensionalByteArrayToBitmap(Byte[,] input)
        {
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            const int bytesPerPixel = 4;  // Each pixel takes 1 byte of data per channel
            int width = input.GetLength(0) / bytesPerPixel;
            int height = input.GetLength(1);
            Bitmap output = new Bitmap(width, height, pixelFormat);
            Rectangle canvas = new Rectangle(0, 0, width, height);
            BitmapData outputData = output.LockBits(canvas, ImageLockMode.ReadOnly, pixelFormat);

            unsafe
            {
                byte* outputPointer = (byte*)outputData.Scan0;

                int byteArrayWidthIndex = 0;

                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++,
                        outputPointer += bytesPerPixel,
                        byteArrayWidthIndex += bytesPerPixel)
                    {
                        if (byteArrayWidthIndex >= width * bytesPerPixel) byteArrayWidthIndex = 0;
                        for (int i = 0; i < bytesPerPixel; i++)
                        {
                            outputPointer[i] = input[byteArrayWidthIndex + i, h];
                        }
                    }
                }
            }

            output.UnlockBits(outputData);
            return output;
        }
        /// <summary>
        /// Fast image manipulation method via direct byte manipulation. !!THE INPUT BITMAP MUST HAVE A PIXELFORMAT OF 32bppArgb!!
        /// </summary>
        [Obsolete("", false)]
        public static Bitmap ConvertToGreyscaleFast(Bitmap input)
        {
            // Setup variables
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            int pixelAverageColor;
            const int bytesPerPixel = 4;                    // Each pixel takes 1 byte of data per channel
            int[] inputRgba = new int[4];                   // RGBA values, RGB is stored in reverse (?) (BGR)

            Bitmap output = new Bitmap(input.Width, input.Height, pixelFormat);
            Rectangle canvas = new Rectangle(0, 0, output.Width, output.Height);
            BitmapData inputData = input.LockBits(canvas, ImageLockMode.ReadOnly, pixelFormat);
            BitmapData outputData = output.LockBits(canvas, ImageLockMode.ReadWrite, pixelFormat);

            unsafe
            {
                byte* outputPointer = (byte*)outputData.Scan0;
                byte* inputPointer = (byte*)inputData.Scan0;

                for (int h = 0; h < input.Height; h++)                                              // Select column
                {
                    for (int w = 0; w < input.Width; w++,                                           // Select pixel within column
                        inputPointer += bytesPerPixel, outputPointer += bytesPerPixel)              //Move to the next pixel
                    {
                        for (int i = 0; i < inputRgba.Length - 1; i++)                              // Loop through the RGBA values of the given pixel
                        {
                            inputRgba[i] = Convert.ToInt32(inputPointer[i]);                        // Read BGR values
                            pixelAverageColor = (inputRgba[0] + inputRgba[1] + inputRgba[2]) / 3;   // Calculate pixel monochrome (average) value
                            outputPointer[i] = Convert.ToByte(pixelAverageColor);                   // Set BGR values
                        }
                        outputPointer[3] = inputPointer[3];                                         // Preserve original Alpha 
                    }
                }
            }
            output.UnlockBits(outputData);
            input.UnlockBits(inputData);

            input.Dispose();
            return output;
        }
    }

}
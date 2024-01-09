using System;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace PolychromeToGreyscale
{
    internal class Program
    {
        // Setup file manager.
        private static readonly FileManager fileManager = new FileManager()
        {
            InputPath = @"input",
            OutputPath = @"output",
            ValidExtensions = new string[] { ".png", ".jpg", ".gif", ".bmp", ".exif", ".tiff", ".jpeg" }
        };

        static void Main(string[] args)
        {

            // Check if folders exist, if not create them.
            if (args.Length <= 0) CheckIOFolders(fileManager.InputPath);
            CheckIOFolders(fileManager.OutputPath);
            
            // Main loop
            while (true)
            {
                ConsoleOutput.RequestInput("1: Convert input to monochrome\n" +
                                           "2: Run edge detection on input\n" +
                                           "0: Exit application.",
                                           ConsoleColor.White, out int select, true);
                string[] files;
                switch (select)
                {
                    case 1:
                        if (args.Length <= 0) files = UpdateFiles();
                        else files = args;
                        ConvertToMonohcrome(files);
                        break;
                    case 2:
                        if (args.Length <= 0) files = UpdateFiles();
                        else files = args;
                        SobelFilter(files);
                        break;
                    case 0:
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private static string[] UpdateFiles()
        {
            string[] files = new string[0];
            string[] unfilteredFiles;
            while (files.Length <= 0)
            {
                unfilteredFiles = fileManager.GetFiles();           // Get files within input folder.
                files = fileManager.FilterFiles(unfilteredFiles);   // Filter files by extension.
            }
            return files;
        }

        private static void SobelFilter(string[] files)
        {
            // Discard pixels below the given threshold.
            ConsoleOutput.RequestInput("Input threshold(0-255)", ConsoleColor.White, out int threshold);
            if (threshold > 255) threshold = 255;
            else if (threshold < 0) threshold = 0;
            ConsoleOutput.Write("Threshold:" + threshold + "\npress any key to start.", true, true);

            // Track progress.
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int filesProcessed = 0;

            // Loop through the files and process them.
            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(0, files.Length, parallelOptions, i =>
            {
                Bitmap bitmap = new Bitmap(files[i]);
                try
                {
                    // Print file info.
                    ConsoleOutput.Write("Starting work on file #" + i + " on thread ID " + Task.CurrentId +
                                        "\n" + "\tFile resolution:\t" + bitmap.Width + "x" + bitmap.Height +
                                        "\n" + "\tFile name:\t\t" + Path.GetFileName(files[i]), ConsoleColor.White);

                    ImageProcessor.SetPixelFormat(ref bitmap, PixelFormat.Format32bppArgb);                     // Convert the bitmap pixelformat to 32bppArgb if need be.
                    byte[,][] bitmapBytes = ImageProcessor.BitmapToJaggedByteArray(bitmap);                     // Convert the bitmap to a jagged array[X,Y][BGRA].
                    byte[,][] sobelJaggedArray = ImageProcessor.SobelEdgeDetection(bitmapBytes, threshold);     // Run edge detection on the jagged array.
                    bitmap = ImageProcessor.JaggedByteArrayToBitmap(sobelJaggedArray);                          // Convert the jagged array back to a bitmap.

                    // Save the converted bitmap to a file.
                    fileManager.SaveBitmap(ref bitmap, Path.GetFileNameWithoutExtension(files[i]), "_Sobel.png");
                }
                catch(Exception ex)
                {
                    ConsoleOutput.Write(ex.ToString(), ConsoleColor.Red);
                }
                finally
                {
                    bitmap.Dispose();
                }

                // Print process info.
                filesProcessed++;
                ConsoleOutput.Write("Done with file ID_#" + i + "\tTotal files processed: " + filesProcessed + "/" + files.Length, ConsoleColor.Green);
            });

            // Print info, wait for user input to exit.
            sw.Stop();
            ConsoleOutput.Write("\rTime elapsed: " + sw.ElapsedMilliseconds / 1000 + " seconds", ConsoleColor.DarkGray);
            ConsoleOutput.Write("Complete! Press any key to return.", true, true);
        }

        private static void ConvertToMonohcrome(string[] files)
        {
            // Track processing time.
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Loop through files and process them.
            int filesProcessed = 0;
            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(0, files.Length, parallelOptions, i =>
            {
                // Load file to bitmap and convert it from polychrome to monochrome
                // then save it to the output folder.

                Bitmap bitmap = new Bitmap(files[i]);
                // Print file info.
                ConsoleOutput.Write("Starting work on file #" + i + " on thread ID " + Task.CurrentId +
                "\n" + "\tFile resolution:\t" + bitmap.Width + "x" + bitmap.Height +
                "\n" + "\tFile name:\t\t" + Path.GetFileName(files[i]), ConsoleColor.White);

                try
                {
                    // Fast greyscale conversion expects pixel format of 32bppArgb.
                    // Convert the bitmap to 32bppArgb if need be.
                    ImageProcessor.SetPixelFormat(ref bitmap, PixelFormat.Format32bppArgb);
                    ImageProcessor.ConvertToGreyscaleFast(ref bitmap);

                }
                catch (Exception ex)
                {
                    ConsoleOutput.Write(ex.ToString(), ConsoleColor.Magenta);
                    return;
                }

                // Save the converted bitmap to a file.
                fileManager.SaveBitmap(ref bitmap, Path.GetFileNameWithoutExtension(files[i]), "_Greyscale.png");
                bitmap.Dispose();

                // Print process info.
                filesProcessed++;
                ConsoleOutput.Write("Done with file ID_#" + i + "\tTotal files processed: " + filesProcessed + "/" + files.Length, ConsoleColor.Green);
            });

            // Print info, wait for user input to exit.
            sw.Stop();
            ConsoleOutput.Write("\rTime elapsed: " + sw.ElapsedMilliseconds / 1000 + " seconds", ConsoleColor.DarkGray);
            ConsoleOutput.Write("Complete! Press any key to return.", true, true);
        }

        private static void CheckIOFolders(string folderPath)
        {
            if(!fileManager.FolderExist(folderPath))
            {
                try
                {
                    Console.WriteLine("\rNo folder found at " + folderPath + ".");
                    fileManager.CreateFolder(folderPath);
                }
                catch (ApplicationException ex)
                {
                    ConsoleOutput.Write("\rFailed to create folder!\n" + ex, ConsoleColor.Magenta, true, false);
                    Environment.Exit(-1);
                }
            }
        }
    }
}
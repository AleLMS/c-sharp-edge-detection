using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PolychromeToGreyscale
{
    internal class FileManager
    {

        public string InputPath { get; set; }

        public string OutputPath { get; set; }

        public string[] ValidExtensions { get; set; }

        public bool FolderExist(string folderPath)
        {
            // TEMP | Implement later
            if (!Directory.Exists(folderPath))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void CreateFolder(string folderPath)
        {
            Console.WriteLine("\rCreate folder at path: " + folderPath + "? (Y/N)");
            var input = Console.ReadKey();
            if(input.Key == ConsoleKey.Y)
            {
                Directory.CreateDirectory(folderPath);
                return;
            } else
            {
                throw new ApplicationException();
            }
        }

        public string[] SelectFiles(string inputFolder)
        {
            string[] files = Directory.GetFiles(inputFolder);
            return files;
        }

        public string[] FilterFiles(string[] files)
        {
            List<string> filtered = new List<string>();

            foreach (string file in files)
            {
                if (ValidExtensions.Any(Path.GetExtension(file).Contains))
                {
                    filtered.Add(file);
                    continue;
                } 
                else
                {
                    ConsoleOutput.Write(file + " invalid file format, dropping file.", ConsoleColor.Red);
                    continue;
                }
            }
            string[] output = filtered.ToArray();

            ConsoleOutput.Write("Found " + output.Length + " valid files.", ConsoleColor.Green);

            return output;
        }

        public string[] GetFiles()
        {
            // Acquire files
            string[] selectedFiles = SelectFiles(InputPath);

            if (selectedFiles.Length <= 0)
            {
                ConsoleOutput.Write("\rNo valid files found. Press any key to retry.", true, true);
                return selectedFiles;
            }
            else
            {
                ConsoleOutput.Write("\rFound " + selectedFiles.Length + " files.", ConsoleColor.Green);
                return selectedFiles;
            }
        }

        /// <summary>
        /// Save bitmap to a file.
        /// </summary>
        public void SaveBitmap(ref Bitmap bitmap, string file, string suffix)
        {
            string outputName = Path.GetFileNameWithoutExtension(file);
            string fullSaveString = OutputPath + "/" + outputName + suffix;
            bitmap.Save(fullSaveString);
        }

        public FileManager(string aInputPath, string aOutputPath, string[] aValidExtensions)
        {
            InputPath = aInputPath;
            OutputPath = aOutputPath;
            ValidExtensions = aValidExtensions;
        }

        public FileManager()
        {
        }
    }
}

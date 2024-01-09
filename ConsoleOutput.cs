using System;

namespace PolychromeToGreyscale
{
    internal class ConsoleOutput
    {
        private static readonly object _writeLock = new object();

        public static void Write(string message, ConsoleColor color, bool pauseForInput, bool clearConsole)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                if (pauseForInput) Console.ReadKey();
                if (clearConsole) Console.Clear();
                Console.ResetColor();
            }
        }

        public static void Write(string message, ConsoleColor color)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Write(string message, bool pauseForInput, bool clearConsole)
        {
            lock (_writeLock)
            {
                Console.WriteLine(message);
                if (pauseForInput) Console.ReadKey();
                if (clearConsole) Console.Clear();
                Console.ResetColor();
            }
        }

        public static void RequestInput(string message, ConsoleColor color, out int output)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Int32.TryParse(Console.ReadLine(), out output);
            }
        }

        public static void RequestInput(string message, ConsoleColor color, out int output, bool clearConsole)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Int32.TryParse(Console.ReadLine(), out output);
                if(clearConsole) Console.Clear();
            }
        }

        public static void MultiLineWrite(string[] messages, ConsoleColor[] colors)
        {
            lock (_writeLock)
            {
                for(int i = 0; i < messages.Length; i++) { 
                    Console.ForegroundColor = colors[i];
                    Console.WriteLine(messages[i]);
                }
                Console.ResetColor();
            }
        }

        public static void MultiLineWrite(string[] messages, ConsoleColor[] colors, bool clearConsole, bool pauseForInput)
        {
            lock (_writeLock)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    Console.ForegroundColor = colors[i];
                    Console.WriteLine(messages[i]);
                }
                Console.ResetColor();
                if (pauseForInput) Console.ReadKey();
                if (clearConsole) Console.Clear();
            }
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AngelDB
{
    /// <summary>
    /// For all input output work
    /// </summary>
    public static class Monitor
    {
        /// <summary>
        /// Show a message in console
        /// </summary>
        /// <param name="message">Message to display in console</param>
        public static void ShowLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Show messages in color and add new line
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Color"></param>
        public static void ShowLine(string message, ConsoleColor Color)
        {
            Console.ForegroundColor = (ConsoleColor)Color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a message on the console in color
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Color"></param>
        public static void Show(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Show errors strings on console
        /// </summary>
        /// <param name="message"></param>
        public static void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Show warning strings on console
        /// </summary>
        /// <param name="message"></param>
        public static void ShowWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Show messages from server
        /// </summary>
        /// <param name="message"></param>
        public static void ShowServerMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Show error messages from server
        /// </summary>
        /// <param name="message"></param>
        public static void ShowServerError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Wait for input data and shows a prompt in color
        /// </summary>
        /// <param name="promptSignal"></param>
        /// <returns></returns>
        public static string Prompt(string promptSignal)
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(promptSignal);
            Console.ResetColor();
            return Console.ReadLine();

        }

        public static string ReadPassword(string promptSignal) 
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(promptSignal);
            Console.ResetColor();

            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            return pass;

        }

        public static string ReadString()             
        {
            string s = Console.ReadLine();
            return s;
        }
        

    }

}

// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using Pegasus.Common;

    internal class Program
    {
        public static void Main()
        {
            var parser = new Parser();

            var normalColor = Console.ForegroundColor;
            Console.WriteLine("MathParser REPL. Ctrl+C to exit.");
            const string Prompt = "Math> ";
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(Prompt);
                Console.ForegroundColor = normalColor;
                var line = Console.ReadLine();

                try
                {
                    var expression = parser.Parse(line);
                    Console.WriteLine(expression.ToString());
                }
                catch (FormatException ex)
                {
                    var cursor = ex.Data["cursor"] as Cursor;
                    if (cursor != null && cursor.Column != 1)
                    {
                        var indicator = new string(' ', Prompt.Length + cursor.Column - 2) + "^";
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(indicator);
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = normalColor;
                }

                Console.WriteLine();
            }
        }
    }
}

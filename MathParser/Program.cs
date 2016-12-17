// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Drawing;
    using System.Threading;
    using System.Threading.Tasks;
    using Pegasus.Common;

    internal class Program
    {
        public static void Main()
        {
            var parser = new Parser();

            var display = new Lazy<ExpressionDisplay>(() =>
            {
                ExpressionDisplay result = null;
                var semaphore = new SemaphoreSlim(0);
                Task.Factory.StartNew(() =>
                {
                    result = new ExpressionDisplay
                    {
                        Font = new Font("Calibri", 20, FontStyle.Regular),
                    };
                    semaphore.Release();
                    result.ShowDialog();
                    Environment.Exit(0);
                });
                semaphore.Wait();
                return result;
            });

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
                    display.Value.Expression = expression;
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

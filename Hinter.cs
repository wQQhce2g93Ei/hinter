using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HinterLib
{
    public class Hinter
    {
        public static string ReadHintedLine<T, TResult>(
            IEnumerable<T> hintSource,
            Func<T, TResult> hintField,
            string inputRegex = "^([A-Za-z]|[0-9]|_)*$",
            ConsoleColor hintColor = ConsoleColor.DarkGray,
            bool ignoreCase = false,
            bool findAnywhere = false)
        {
            var originalColor = Console.ForegroundColor;
            var originalRow = Console.CursorTop;
            var currentMatchIndex = 0;
            Func<int> currentRow = () => originalRow + currentMatchIndex;

            var userInput = string.Empty;
            var matches = new List<Match>();

            ConsoleKeyInfo input;
            while (ConsoleKey.Enter != (input = Console.ReadKey()).Key)
            {
                if (input.Key == ConsoleKey.Backspace)
                {
                    userInput = userInput.Any() ? userInput.Substring(0, userInput.Length - 1) : string.Empty;
                }
                else if (input.Key == ConsoleKey.DownArrow || input.Key == ConsoleKey.UpArrow)
                {
                    currentMatchIndex = input.Key == ConsoleKey.DownArrow && matches.Count > currentMatchIndex + 1 ? currentMatchIndex + 1 : currentMatchIndex;
                    currentMatchIndex = input.Key == ConsoleKey.UpArrow && currentMatchIndex > 0 ? currentMatchIndex - 1 : currentMatchIndex;

                    SetCurrentCursorPosition(matches[currentMatchIndex], currentRow.Invoke());
                }
                else if (input.Key == ConsoleKey.Tab)
                {
                    break;
                }
                else if (Regex.IsMatch(input.KeyChar.ToString(), inputRegex))
                {
                    currentMatchIndex = 0;
                    userInput += input.KeyChar;
                }

                ClearCurrentWrittenRows(originalRow, originalRow + matches.Count);

                var potentialMatches = hintSource.Select(item => GetMatch(hintField(item).ToString(), userInput, ignoreCase, findAnywhere))
                    .OrderBy(o => o.BeforeMatchText.Length)
                    .ToList();

                matches = string.IsNullOrEmpty(userInput)
                    ? potentialMatches.Take(5).ToList()
                    : potentialMatches.Any(o => o.IsMatch)
                        ? potentialMatches.Where(o => o.IsMatch).Take(5).ToList()
                        : new List<Match> { new Match { MatchText = userInput } };

                foreach (var match in matches)
                {
                    WriteColoredText(match.BeforeMatchText, hintColor);
                    WriteColoredText(match.MatchText, originalColor);
                    WriteColoredText(match.AfterMatchText, hintColor);
                    Console.WriteLine();
                }

                SetCurrentCursorPosition(matches[currentMatchIndex], currentRow.Invoke());
            }

            ClearCurrentWrittenRows(originalRow, originalRow + matches.Count);
            WriteColoredText(matches[currentMatchIndex].FullMatch, originalColor);
            Console.ForegroundColor = originalColor;

            return matches[currentMatchIndex].FullMatch;
        }

        private static void SetCurrentCursorPosition(Match currentMatch, int currentRow)
        {
            Console.SetCursorPosition(currentMatch.BeforeMatchText.Length + currentMatch.MatchText.Length, currentRow);
        }

        private static void ClearCurrentWrittenRows(int originalRow, int lastRow)
        {
            while (lastRow >= originalRow)
            {
                Console.SetCursorPosition(0, lastRow);
                ClearCurrentConsoleLine();
                lastRow--;
            }
        }

        private static void WriteColoredText(string userInput, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(userInput);
        }

        private static Match GetMatch(string item, string userInput, bool ignoreCase, bool findAnywhere)
        {
            var index = ignoreCase ? item.IndexOf(userInput, StringComparison.CurrentCultureIgnoreCase) : item.IndexOf(userInput);
            if (findAnywhere ? index != -1 : index == 0)
            {
                return new Match
                {
                    IsMatch = true,
                    BeforeMatchText = item.Substring(0, index),
                    MatchText = userInput,
                    AfterMatchText = item.Substring(index + userInput.Length)
                };
            }

            return new Match { IsMatch = false, MatchText = userInput };
        }

        private static void ClearCurrentConsoleLine()
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private class Match
        {
            public bool IsMatch { get; set; }
            public string BeforeMatchText { get; set; } = string.Empty;
            public string MatchText { get; set; } = string.Empty;
            public string AfterMatchText { get; set; } = string.Empty;
            public string FullMatch => BeforeMatchText + MatchText + AfterMatchText;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Wordle {

    public class Program {
        private static readonly string SOL_PATH = "/Users/nicgw/Desktop/repos/wordle-algo/sol-word-list.txt";
        private static readonly string FULL_WORD_LIST_PATH = "/Users/nicgw/Desktop/repos/wordle-algo/complete-word-list.txt";
        private static readonly string OUTPUT_PATH = "/Users/nicgw/Desktop/repos/wordle-algo/data.csv";
        private static int NUM_RUNS;
        private static int RUN_MODE;

        private static readonly Regex alphaNumRegex = new Regex("[^a-zA-Z0-9]");

        private static readonly string BLUE_SQUARE = "\uD83D\uDFE6";
        private static readonly string YELLOW_SQUARE = "\uD83D\uDFE8";
        private static readonly string BLACK_SQUARE = "\u2b1b";

        public static void Main(string[] args) {

            Console.WriteLine($"Starting...\nImporting Word Data...");
            List<string> solutionList = ImportWords(SOL_PATH);

            
            while(true) {
                Console.WriteLine("Run Modes:\n" +
                                  "1: Single Run\n" +
                                  "2: Batch Run\n" +
                                  "3: Play Game\n" +
                                  "4: Solver\n" +
                                  "5: Exit");
                RUN_MODE = Convert.ToInt32(Console.ReadLine());    

                if (RUN_MODE == 1) {
                    SingleRun(solutionList);
                }
                else if (RUN_MODE == 2) {
                    BatchRun(solutionList);
                } 
                else if (RUN_MODE == 3) {
                    PlayGame(solutionList);
                }
                else if (RUN_MODE == 4) {
                    RunSolver(solutionList);
                }
                else if (RUN_MODE == 5) {
                    break;
                }
                else {
                    Console.WriteLine("Error: Not a valid run mode");   
                }
            }
        }

        private static void SingleRun(List<string> solutionList) {
           RunData run = new RunData();
           Run(solutionList, out run); 
        }
        private static void RunSolver(List<string> solutionList) {
            bool isLooking = true;
            List<char> chars = new List<char>();
            List<char> badChars = new List<char>();

            while (isLooking) {
                string goodChars = "";
                foreach (char goodChar in chars) {
                    goodChars += goodChar;
                }
                Console.WriteLine($"Current good characters:\n {goodChars}");
                Console.WriteLine("Enter new good characters:\n");
                string newChars = Console.ReadLine();

                string currBadChars = "";
                foreach (char currBadChar in badChars) {
                    currBadChars += currBadChar;
                }
                Console.WriteLine($"Current bad characters:\n {currBadChars}");

                Console.WriteLine("Enter new bad characters:\n");
                string newBadChars = Console.ReadLine();

                foreach (char newChar in newChars) {
                    chars.Add(newChar);
                }

                foreach (char badChar in newBadChars) {
                    badChars.Add(badChar);
                }

                List<string> newWords = new List<string>();
                foreach (string word in solutionList) {
                    if (ContainsChars(word, chars, badChars)) {
                        newWords.Add(word);
                    }
                }
                if (newWords.Count == 1) {
                    isLooking = false;
                    Console.WriteLine($"{newWords[0]}");
                }
                else if (newWords.Count > 300) {
                    Console.WriteLine($"{newWords.Count} matches. Enter more characters\n");
                }
                else if (newWords.Count == 0) {
                    Console.WriteLine($"No words found, sorry!");
                    isLooking = false;
                }
                else {
                    foreach (string word in newWords) {
                        Console.WriteLine($"{word}");
                    }
                }
                
            }
        }
        private static void PlayGame(List<string> solutionList) {
            Stopwatch sw = new Stopwatch();
            Random rand = new Random();
            int index = rand.Next(solutionList.Count);
            string targetWord = solutionList[index];
            bool wonEarly = false;


            List<char> chars = new List<char>();
            List<char> badChars = new List<char>();
            var attempts = new List<List<Tuple<char, int>>>();
            

            Console.WriteLine("Welcome to WordleClone! Please enter your first word...\n");
            string attempt = Console.ReadLine();
            sw.Start();
            attempts.Add(ConvertAttempt(targetWord, attempt));

            while (attempts.Count < 5) {
                PrintBoard(attempts);
                string newAttempt = Console.ReadLine();
                attempts.Add(ConvertAttempt(targetWord, newAttempt));
                if (newAttempt == targetWord) {
                    wonEarly = true;
                    break;
                }
            }
            sw.Stop();
            if (!wonEarly) {
                if (attempts[4] == ConvertAttempt(targetWord, targetWord)) {
                    Console.WriteLine("You win!");
                }
                else {
                    Console.WriteLine("You Lost!");
                } 
            } else {
               Console.WriteLine("You win!"); 
            }
            PrintBoard(attempts);
            Console.WriteLine($"Results:\nword = {targetWord} time = {sw.ElapsedMilliseconds / 1000 / 60} minutes");
            PrintResult(attempts);
        }
        private static void BatchRun(List<string> solutionList) {
            Stopwatch sw = new Stopwatch();
            Console.WriteLine($"Data imported.\nNumber of runs:");
            NUM_RUNS = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine($"Running...");
            sw.Start();
            List<RunData> dataList = GetRunData(solutionList);
            sw.Stop();
            Console.WriteLine($"\nTook {sw.ElapsedMilliseconds} ms");

            Console.WriteLine($"Done.\nCalculating CMA...");
            sw.Reset();

            sw.Start();
            List<double> cmaData = CalculateGuessCMA(dataList);
            sw.Stop();
            Console.WriteLine($"\nTook {sw.ElapsedMilliseconds} ms");

            //CreateCSV(dataList, cmaData);
            Console.WriteLine("CMA Gathered\nStarting plot...");
            CreatePlot(dataList, cmaData);
            Console.WriteLine($"Plot done.");
        }

        private static void CreatePlot(List<RunData> runs, List<double> cmaData) {
            var plt = new ScottPlot.Plot();
            double[] guesses = new double[runs.Count];
            double[] cmas = new double[cmaData.Count];
            double[] runNum = new double[cmaData.Count];

            for (int i = 0; i < runs.Count; i++) {
                guesses[i] = (double)runs[i].NumGuess;
                cmas[i] = (double)cmaData[i];
                runNum[i] = (double)(i+1);
            }

            //plt.AddScatter(runNum, guesses);
            plt.AddScatter(runNum, cmas);

            plt.Title($"CMA for {NUM_RUNS} runs");
            plt.XLabel("Run Number");
            plt.YLabel($"Number of Guesses");
            
            plt.SetAxisLimitsY(0, cmas.Max() + (cmas.Max() * 0.25));

            plt.SaveFig($"cma-for-{cmaData.Count}-runs.png");
        }

        private static void CreateCSV(List<RunData> dataList, List<double> cmaData) {
            var csv = new StringBuilder();
            var header = string.Format("Run,NumGuesses,TimeToComplete,GuessCMA");
            csv.AppendLine(header);
            for (int j = 0; j < dataList.Count; j++) {
                var newLine = string.Format($"{j},{dataList[j].NumGuess},{dataList[j].TimeToCompleteMs},{cmaData[j]}");
                csv.AppendLine(newLine);
            }
            
            File.WriteAllText(OUTPUT_PATH, csv.ToString());
        }

        private static List<RunData> GetRunData(List<string> solutionList) {
           List<RunData> dataList = new List<RunData>();
           for (int i = 0; i < NUM_RUNS; i++) {
               RunData runData = new RunData();   
               Run(solutionList, out runData);
               dataList.Add(runData);
               Console.Write($"\r {i+1}/{NUM_RUNS}");
            }
            return dataList;
        }
        private static List<double> CalculateGuessCMA(List<RunData> runs) {
            List<double> cmaData = new List<double>();
            List<int> guesses = new List<int>();

            foreach (RunData run in runs) {
                guesses.Add(run.NumGuess);
                double cma = (double)Sum(CollectionsMarshal.AsSpan(guesses)) / guesses.Count;
                cmaData.Add(cma);
                Console.Write($"\r {runs.IndexOf(run)+1}/{NUM_RUNS}");
            }

            return cmaData;
        }

        private static double Sum(ReadOnlySpan<int> source) {
            int sum = 0;
            if (Vector.IsHardwareAccelerated && 
                source.Length>Vector<int>.Count*2) 
            // use SIMD    
            {
                var vectors = MemoryMarshal.Cast<int, Vector<int>>(source);
                var vectorSum = Vector<int>.Zero;
                
                foreach (var vector in vectors)
                    vectorSum += vector;
                
                for (var index = 0; index<Vector<int>.Count; index++)
                    sum += vectorSum[index];
                
                var count = source.Length % Vector<int>.Count;
                source = source.Slice(source.Length-count, count);    
            }

            foreach (var item in source)
                sum += item;
            return sum;
        }

        private static void Run(List<string> solutionList, out RunData thisRun) {
            Stopwatch sw = new Stopwatch();
            Random rand = new Random();
            int index = rand.Next(solutionList.Count);
            string targetWord = solutionList[index];

            var attempts = new List<List<Tuple<char, int>>>();
            bool isDone = false;
            int guessIndex = rand.Next(solutionList.Count);
            string guess = solutionList[guessIndex];
            int numGuess = 1;

            if (RUN_MODE == 1) {Console.WriteLine($"Starting... list size is {solutionList.Count}");}
            List<string> currList = solutionList;

            sw.Start();
            var convertedAttempt = ConvertAttempt(targetWord, guess);

            while (!isDone) {
                if (guess == targetWord) {
                    isDone = true;
                    if (RUN_MODE == 1) {PrintAttempt(ConvertAttempt(targetWord, guess));}
                    break;
                }
                else {
                    convertedAttempt = ConvertAttempt(targetWord, guess);
                    attempts.Add(convertedAttempt);
                    List<string> newList = FilterList(convertedAttempt, currList);
                    if (RUN_MODE == 1) {
                        PrintAttempt(convertedAttempt);
                        Console.WriteLine($"New list size is {newList.Count}");
                        }

                    if (newList.Contains(guess)) {
                        newList.Remove(guess);
                    }
                    
                    guessIndex = rand.Next(newList.Count);
                    guess = newList[guessIndex];
                    numGuess++;
                    currList = newList;
                }
            }
            attempts.Add(ConvertAttempt(targetWord, guess));
            sw.Stop();
            if (RUN_MODE == 1) {
                Console.WriteLine($"Done! Word was {targetWord}. Guessed in {numGuess} guesses. Completed in {sw.ElapsedMilliseconds} ms");
                PrintResult(attempts);
            }
            thisRun = new RunData(numGuess, sw.ElapsedMilliseconds);
        }
        
        private static List<string> FilterList(List<Tuple<char, int>> attempt, List<string> listToFilter) {
            List<string> filteredList = new List<string>();
            var attemptNoLocks = new List<Tuple<char, int>>();
            var badLetters = new List<Tuple<char,int>>();

            foreach (var letter in attempt) {
                filteredList = new List<string>();
                if (letter.Item2 != -1 && letter.Item2 != -2) {
                    var query = from word in listToFilter
                                where word[letter.Item2] == letter.Item1
                                select word;

                    foreach (var newWord in query.ToList()) {
                        filteredList.Add(newWord);
                    }
                }
                else if (letter.Item2 == -1) {
                    attemptNoLocks.Add(letter);
                }
                else if (letter.Item2 == -2) {
                    badLetters.Add(letter);
                }

                if (filteredList.Count != 0) {
                    listToFilter = filteredList;
                }
            }

            foreach (var letter in attemptNoLocks) {
                filteredList = new List<string>();
                // Would be nice to have a record of all positions a floating letter has been in before
                var query = from word in listToFilter
                            where word.Contains(letter.Item1)
                            select word;

                foreach (var newWord in query.ToList()) {
                    filteredList.Add(newWord);
                }

                if (filteredList.Count != 0) {
                    listToFilter = filteredList;
                } 
            }

            foreach (var letter in badLetters) {
                filteredList = new List<string>();
                var query = from word in listToFilter
                            where !word.Contains(letter.Item1)
                            select word;

                foreach (var noBadLetterWord in query.ToList()) {
                    filteredList.Add(noBadLetterWord);
                }

                if (filteredList.Count != 0) {
                    listToFilter = filteredList;
                }
            }

            if (filteredList.Count == 0) {
                return listToFilter;
            }
            else {
                return filteredList.Distinct().ToList();
            }
        }

        private static void PrintAttempt(List<Tuple<char, int>> attempt) {
            foreach (var letter in attempt) {
                    if (letter.Item2 >= 0) {
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                    }
                    else if (letter.Item2 == -1) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write($" {letter.Item1} ");
                    Console.ResetColor();
            }
        }
        private static void PrintResult(List<List<Tuple<char, int>>> attempts) {
           Console.OutputEncoding = System.Text.Encoding.UTF8;
           foreach (var word in attempts) {
                foreach (var letter in word) {
                    if (letter.Item2 >= 0) {
                        Console.Write(BLUE_SQUARE);
                    }
                    else if (letter.Item2 == -1) {
                        Console.Write(YELLOW_SQUARE);
                    }
                    else {
                        Console.Write(BLACK_SQUARE);
                    }
                }
                Console.WriteLine();
            } 
        }
        private static void PrintBoard(List<List<Tuple<char, int>>> attempts) {
            foreach (var word in attempts) {
                foreach (var letter in word) {
                    if (letter.Item2 >= 0) {
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                    }
                    else if (letter.Item2 == -1) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write($" {letter.Item1} ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }

        private static List<Tuple<char, int>> ConvertAttempt(string targetWord, string newWord) {
            var convertedWord = new List<Tuple<char, int>>();
            for (int i = 0; i < newWord.Length; i++) {
                if (newWord[i] == targetWord[i]) {
                    Tuple<char, int> lockLetter = new Tuple<char, int>(newWord[i], i);
                    convertedWord.Add(lockLetter);
                }
                else if (targetWord.Contains(newWord[i])) {
                    Tuple<char, int> unlockLetter = new Tuple<char, int>(newWord[i], -1);
                    convertedWord.Add(unlockLetter);
                }
                else {
                    Tuple<char, int> badLetter = new Tuple<char, int>(newWord[i], -2);
                    convertedWord.Add(badLetter);
                }
            }
            return convertedWord;
        }

        private static bool ContainsChars(string word, List<char> chars, List<char> badChars) {
            foreach (char goodLetter in chars) {
                if (!word.Contains(goodLetter)) {
                    return false;
                }
            }

            foreach(char badLetter in badChars) {
                if (word.Contains(badLetter)) {
                    return false;
                }
            }

            return true;

        }
        private static List<string> ImportWords(string path) {
            List<string> parsedList = new List<string>();
            using (var reader = new StreamReader(path)) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    for (int i = 0; i < values.Count(); i++) {
                        values[i] = alphaNumRegex.Replace(values[i], String.Empty);
                    }
                    parsedList.AddRange(values);
                }
            }
            return parsedList;
        }

    }
}
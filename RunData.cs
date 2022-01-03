using System;

namespace Wordle {
    public class RunData {
        public int NumGuess { get; set; }
        public long TimeToCompleteMs  { get; set; }

        public RunData(int guesses, long completionTime) {
            NumGuess = guesses;
            TimeToCompleteMs = completionTime;
        }

        public RunData() {}
    }
}
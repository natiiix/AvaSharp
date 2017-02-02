using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NatiTools.xIO;

namespace AvaSharp
{
    class Program
    {
        private const string SAMPLES_PATH = "samples.xml";

        static int Main(string[] args)
        {
            List<string> listSamples = new List<string>();

            if (!File.Exists(SAMPLES_PATH))
                SerializerXML.Serialize(listSamples, SAMPLES_PATH);

            listSamples = SerializerXML.Deserialize<List<string>>(SAMPLES_PATH);

            for (;;)
            {
                Console.Write("Enter your sentence: ");

                string input = Console.ReadLine();
                if (input == string.Empty)
                    break;

                double matchScore = 0.0;
                Console.WriteLine("Best match: " + FindBestMatch(input, listSamples, out matchScore).ToString() + " [ " + matchScore.ToString("F3") + " ]");

                if (matchScore < 0.8)
                    listSamples.Add(input);
            }

            SerializerXML.Serialize(listSamples, SAMPLES_PATH);
            return 0;
        }

        private static int FindBestMatch(string input, List<string> samples, out double matchScore)
        {
            string[] wordsInput = BreakSentenceToWords(input);
            //string[][] wordsSamples = new string[samples.Length][];
            
            int bestMatchID = -1;
            double bestMatchScore = 0.0;

            for (int iSample = 0; iSample < samples.Count && bestMatchScore < 1.0; iSample++)
            {
                double sampleScore = CompareSentences(wordsInput, BreakSentenceToWords(samples[iSample]));

                if (sampleScore > bestMatchScore)
                {
                    bestMatchID = iSample;
                    bestMatchScore = sampleScore;
                }
            }

            matchScore = bestMatchScore;
            return bestMatchID;
        }

        private static double CompareSentences(string[] inputWords, string[] sampleWords)
        {
            double inputMaxScore = 0.0;
            double matchScore = 0.0;

            for (int iInput = 0; iInput < inputWords.Length; iInput++)
            {
                inputMaxScore += inputWords[iInput].Length;

                for (int iSample = 0; iSample < sampleWords.Length; iSample++)
                {
                    if (inputWords[iInput] == sampleWords[iSample])
                        matchScore += inputWords[iInput].Length;
                }
            }

            return matchScore / inputMaxScore;
        }

        private static string[] BreakSentenceToWords(string sentence)
        {
            return sentence.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

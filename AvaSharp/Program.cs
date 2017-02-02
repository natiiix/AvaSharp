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
        private const string FILE_QUESTIONS_PATH = "questions.xml";
        private const string FILE_ANSWER_PATH = "answers.xml";

        // Question similarity threshold
        private const double MATCH_SCORE_REQUIED = 0.75;

        static int Main(string[] args)
        {
            Random rand = new Random();

            List<Question> listQuestions = new List<Question>();
            List<Answer> listAnswers = new List<Answer>();

            // Create database files if they don't exist yet
            if (!File.Exists(FILE_QUESTIONS_PATH))
                SerializerXML.Serialize(listQuestions, FILE_QUESTIONS_PATH);

            if (!File.Exists(FILE_ANSWER_PATH))
                SerializerXML.Serialize(listAnswers, FILE_ANSWER_PATH);

            // Load the question and answer database
            listQuestions = SerializerXML.Deserialize<List<Question>>(FILE_QUESTIONS_PATH);
            listAnswers = SerializerXML.Deserialize<List<Answer>>(FILE_ANSWER_PATH);

            for (;;)
            {
                // Ask a question
                Question askedQuestion = GetMostAcuteQuestion(listQuestions, listAnswers);
                if (askedQuestion.ID >= 0)
                    Console.WriteLine("Q: " + askedQuestion.Text);

                // Receive an answer
                string input = Console.ReadLine();
                // Exit on empty input
                if (input == string.Empty)
                    break;

                // Save the answer
                if (askedQuestion.ID >= 0)
                    listAnswers.Add(new Answer(listAnswers.Count, askedQuestion.ID, input));

                // Find a similar question to the one currently being asked
                Question question = FindBestMatch(input, listQuestions);

                // If there is no such question in the database, reply with "I don't know." and save the question
                if (question.ID < 0)
                {
                    listQuestions.Add(new Question(listQuestions.Count, input));
                    Console.WriteLine("A: I don't know.");
                }
                else
                {
                    // Increment the question asked counter
                    IncrementQuestionCounter(question.ID, ref listQuestions);

                    // Find a suitable answer for this question
                    Answer answer = FindAnswer(question.ID, listAnswers, rand);

                    // If there is none, reply with "I don't know."
                    if (answer.ID < 0)
                        Console.WriteLine("A: I don't know.");
                    // If there is a suitable answer, reply with it
                    else
                        Console.WriteLine("A: " + answer.Text);
                }
            }

            // Save the question and answer database
            SerializerXML.Serialize(listQuestions, FILE_QUESTIONS_PATH);
            SerializerXML.Serialize(listAnswers, FILE_ANSWER_PATH);

            return 0;
        }

        // Find a question by its ID and increment its asked counter
        private static void IncrementQuestionCounter(int question_id, ref List<Question> questions)
        {
            for (int i = 0; i < questions.Count; i++)
            {
                if (questions[i].ID == question_id)
                {
                    questions[i].IncrementCounter();
                    return;
                }
            }
        }

        // Using a built-in formula, decide which question needs to be asked the most
        private static Question GetMostAcuteQuestion(List<Question> questions, List<Answer> answers)
        {
            // There's nothing to ask if there are no questions
            if (questions.Count <= 0)
                return new Question();

            double highestPriority = 0;
            int highestPriorityIndex = 0;

            // Go through all the questions and find the one with the highest priority
            for (int i = 0; i < questions.Count; i++)
            {
                int answerCount = GetQuestionAnswers(questions[i].ID, answers).Count;
                // Simple priority calculation formula
                double priority = questions[i].Counter / (answerCount == 0 ? 0.1 : answerCount);

                if (priority > highestPriority)
                {
                    highestPriority = priority;
                    highestPriorityIndex = i;
                }
            }

            return questions[highestPriorityIndex];
        }

        // Find a suitable answer to a question
        private static Answer FindAnswer(int question_id, List<Answer> answers, Random rand)
        {
            // Get all the suitable answers
            List<Answer> questionAnswers = GetQuestionAnswers(question_id, answers);

            // Return a random suitable answer
            if (questionAnswers.Count > 0)
                return questionAnswers[rand.Next(questionAnswers.Count)];
            // If there's no answer to this question, return an empty answer object
            else
                return new Answer();
        }

        // Extract answers to a certain question from a list of all the answers
        private static List<Answer> GetQuestionAnswers(int question_id, List<Answer> answers)
        {
            return answers.FindAll(x => x.QuestionID == question_id);
        }

        // Find a similar question to the one being currently asked
        private static Question FindBestMatch(string input, List<Question> questions)
        {
            string[] wordsInput = BreakSentenceToWords(input);
            
            int bestMatchID = -1;
            double bestMatchScore = 0.0;

            // Compare the input sentence with each question in the database
            for (int i = 0; i < questions.Count; i++)
            {
                double sampleScore = CompareSentences(wordsInput, BreakSentenceToWords(questions[i].Text));

                if (sampleScore > MATCH_SCORE_REQUIED &&
                    sampleScore > bestMatchScore)
                {
                    bestMatchID = i;
                    bestMatchScore = sampleScore;
                }
            }

            // If there is no satisfying match, return an empty question object
            if (bestMatchID < 0)
                return new Question();
            return questions[bestMatchID];
        }

        // Calculate the similarity of two strings pre-separated into words
        private static double CompareSentences(string[] inputWords, string[] sampleWords)
        {
            double inputMaxScore = 0.0;
            double matchScore = 0.0;

            // Compare each word in the input sentence with each word in the sample sentence
            for (int i = 0; i < inputWords.Length; i++)
            {
                inputMaxScore += inputWords[i].Length;

                for (int j = 0; j < sampleWords.Length; j++)
                {
                    // For each matching word, a match score equivalent to its length is added
                    if (inputWords[i] == sampleWords[j])
                        matchScore += inputWords[i].Length;
                }
            }

            return matchScore / inputMaxScore;
        }

        // Separate words in a sentence and remove garbage
        private static string[] BreakSentenceToWords(string sentence)
        {
            return sentence
                .ToLower()
                .Replace("'re", " are")
                .Replace("'ve", " have")
                .Replace("can't", "cannot")
                .Replace("n't", " not")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(",", "")
                .Replace(".", "")
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    // Class used to store questions
    public class Question
    {
        public int ID;
        public string Text;
        // How many times has the program been asked this question
        public int Counter;

        public Question()
        {
            ID = -1;
            Text = string.Empty;
            Counter = 0;
        }

        public Question(int _id, string _text)
        {
            ID = _id;
            Text = _text;
            Counter = 1;
        }

        public void IncrementCounter()
        {
            Counter++;
        }
    }

    // Class used to store answers
    public class Answer
    {
        public int ID;
        public int QuestionID;
        public string Text;

        public Answer()
        {
            ID = -1;
            QuestionID = -1;
            Text = string.Empty;
        }

        public Answer(int _id, int _question_id, string _text)
        {
            ID = _id;
            QuestionID = _question_id;
            Text = _text;
        }
    }
}

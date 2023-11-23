namespace DigitalMakerServer
{
    public interface IWordGenerator
    {
        string GetWords();
    }

    public class WordGenerator : IWordGenerator
    {
        public string GetWords()
        {
            var wordsFile = Path.Combine(Directory.GetCurrentDirectory(), "words.txt");
            var words = File.ReadAllLines(wordsFile);

            var rand = new Random();
            var wordIndex1 = rand.Next(words.Length);
            var wordIndex2 = rand.Next(words.Length);

            return $"{words[wordIndex1]} {words[wordIndex2]}";
        }
    }
}

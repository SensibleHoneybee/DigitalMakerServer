using Xunit;

namespace DigitalMakerServer.Tests
{
    public class WordGeneratorTest
    {
        [Fact]
        public void TestWordGenerator()
        {
            var wordGenerator = new WordGenerator();
            
            var words = wordGenerator.GetWords();

            var splitWords = words.Split(' ');
            Assert.Equal(2, splitWords.Length);
        }
    }
}

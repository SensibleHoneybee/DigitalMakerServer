using Xunit;

namespace DigitalMakerServer.Tests
{
    public class SecretHasherTests
    {
        [Fact]
        public void ThatCorrectPasswordMatches()
        {
            const string password = "this-is-A-password-1234";

            var secretHasher = new SecretHasher();

            var hash = secretHasher.Hash(password);

            Assert.True(secretHasher.Verify(password, hash));
        }

        [Fact]
        public void ThatDifferentCaseDoesNotMatch()
        {
            const string password = "this-is-A-password-1234";

            var secretHasher = new SecretHasher();

            var hash = secretHasher.Hash(password);

            Assert.False(secretHasher.Verify(password.ToLower(), hash));
        }

        [Fact]
        public void ThatTruncatedPasswordDoesNotMatch()
        {
            const string password = "this-is-A-password-1234";

            var secretHasher = new SecretHasher();

            var hash = secretHasher.Hash(password);

            Assert.False(secretHasher.Verify(password.Substring(0, password.Length - 1), hash));
        }
    }
}

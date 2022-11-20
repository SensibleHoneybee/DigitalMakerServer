namespace DigitalMakerServer
{
    public interface ISecretHasher
    {
        public string Hash(string password);

        public bool Verify(string enteredPassword, string hashedPassword);
    }
}

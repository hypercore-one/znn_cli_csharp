using System.Security.Cryptography;
using System.Text;

namespace ZenonCli
{
    internal static class Helper
    {
        public static byte[] ComputeSha256Hash(string message)
        {
            return ComputeSha256Hash(Encoding.UTF8.GetBytes(message));
        }

        public static byte[] ComputeSha256Hash(byte[] message)
        {
            // Create a SHA256 hash from string   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Computing Hash - returns here byte array
                return sha256Hash.ComputeHash(message);
            }
        }

        public static byte[] GeneratePreimage(int length = 32)
        {
            return RandomNumberGenerator.GetBytes(length);
        }
    }
}

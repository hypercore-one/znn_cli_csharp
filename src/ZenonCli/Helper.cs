using System.Security.Cryptography;
using System.Text;

namespace ZenonCli
{
    internal static class Helper
    {
        public static byte[] ComputeStringToSha256Hash(string plainText)
        {
            // Create a SHA256 hash from string   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Computing Hash - returns here byte array
                return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            }
        }
    }
}

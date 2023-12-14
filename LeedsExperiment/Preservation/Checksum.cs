using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Preservation
{
    public class Checksum
    {
        public static string? Sha256FromFile(FileInfo fileInfo)
        {
            using (SHA256 mySHA256 = SHA256.Create())
            {
                using (FileStream fileStream = fileInfo.Open(FileMode.Open))
                {
                    try
                    {
                        // Create a fileStream for the file.
                        // Be sure it's positioned to the beginning of the stream.
                        fileStream.Position = 0;
                        // Compute the hash of the fileStream.
                        byte[] hashValue = mySHA256.ComputeHash(fileStream);
                        // Write the name and hash value of the file to the console.
                        return FromByteArray(hashValue);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"I/O Exception: {e.Message}");
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Access Exception: {e.Message}");
                    }
                }
            }
            return null;
        }

        public static string FromByteArray(byte[] hashValue)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hashValue.Length; i++)
            {
                sb.Append(hashValue[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}

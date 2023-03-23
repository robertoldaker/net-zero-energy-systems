using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace SmartEnergyLabDataApi.Common
{
    public class Crypto
    {
        private RSACryptoServiceProvider _rsaKey;

        public Crypto(RSACryptoServiceProvider rsaKey)
        {
            _rsaKey = rsaKey;
        }

        public string EncryptToBase64(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            //Pass the data to ENCRYPT, the public key information  
            //(using RSACryptoServiceProvider.ExportParameters(false), 
            //and a boolean flag specifying no OAEP padding.
            var encryptedData = _rsaKey.Encrypt(bytes, false);
            return System.Convert.ToBase64String(encryptedData);
        }

        public string DecryptFromBase64(string input)
        {
            var bytes = System.Convert.FromBase64String(input);
            //Pass the data to ENCRYPT, the public key information  
            //(using RSACryptoServiceProvider.ExportParameters(false), 
            //and a boolean flag specifying no OAEP padding.
            var decryptedData = _rsaKey.Decrypt(bytes, false);
            return System.Text.Encoding.UTF8.GetString(decryptedData); ;
        }

        public static byte[] GetPasswordHash(string password, byte[] salt)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] passData = encoding.GetBytes(password);

            //
            byte[] data = new byte[salt.Length + passData.Length];
            salt.CopyTo(data, 0);
            passData.CopyTo(data, salt.Length);

            //
            SHA256 shaM = new SHA256Managed();
            return shaM.ComputeHash(data);
        }

        public static byte[] CreateSalt()
        {
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            byte[] salt = new byte[32];
            rand.GetBytes(salt);
            return salt;
        }

        public static bool VerifyPassword(string password, byte[] salt, byte[] passwordHash)
        {
            // Simply check if the password hashes aggree using the given salt
            byte[] passHash = GetPasswordHash(password,salt);
            //
            return passHash.SequenceEqual(passwordHash);
        }

        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static string CalculateSHA256HexDigest(byte[] key, string input)
        {
            using (var sha256 = new HMACSHA256(key))
            {
                var messageArray = Encoding.ASCII.GetBytes(input);
                var hmacArray = sha256.ComputeHash(messageArray);
                string hmac = "";
                foreach (byte b in hmacArray)
                {
                    hmac += b.ToString("x2");
                }
                // see if they are the same!
                return hmac;
            }
        }

        public static string CalculateSHA256HexDigestAsBase64String(byte[] key, string input)
        {
            using (var sha256 = new HMACSHA256(key))
            {
                var messageArray = Encoding.ASCII.GetBytes(input);
                var hmacArray = sha256.ComputeHash(messageArray);
                string hmac = System.Convert.ToBase64String(hmacArray);
                // see if they are the same!
                return hmac;
            }
        }
    }
}

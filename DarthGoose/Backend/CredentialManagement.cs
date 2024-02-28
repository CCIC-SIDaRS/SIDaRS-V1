using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using System.Text.Json.Serialization;

namespace Backend.CredentialManager
{
    class Credentials
    {
        [JsonInclude]
        private string _username { get; set; }

        [JsonInclude]
        private string _password { get; set; }

        public Credentials(string _username, string _password, bool encrypted = true)
        {
            this._username = _username;
            if (!encrypted)
            {
                this._password = SymmetricEncryption.Encrypt(_password, SymmetricEncryption.master);
            }else
            {
                this._password = _password;
            }
        }

        [JsonConstructor]
        public Credentials(string _username, string _password)
        {
            this._username = _username;
            this._password = _password;
        }

        //public Credentials(Dictionary<string,string> serializedData)
        //{
        //    // This constructor is for use with deserializers
        //    this._username = serializedData[nameof(this._username)];
        //    this._password = serializedData[nameof(this._password)];
        //}
        public string[] GetCreds()
        {
            return [_username, SymmetricEncryption.Decrypt(_password, SymmetricEncryption.master)];
        }
        public string Save()
        {
            Dictionary<string, object> properties = new();
            foreach (PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                properties.Add(prop.Name, prop.GetValue(this));
            return Regex.Replace(JsonSerializer.Serialize(properties), @"[^\u0000-\u007F]+", string.Empty);
        }
    }
    static class SymmetricEncryption
    {
        internal static byte[] master;

        public static byte[] Hash(string source, string saltStr)
        {
            byte[] salt = Encoding.UTF8.GetBytes(saltStr);
            int iterations = 100000;
            int desiredKeyLength = 32;
            HashAlgorithmName hashMethod = HashAlgorithmName.SHA512;
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(source), salt, iterations, hashMethod, desiredKeyLength);
        }
        public static string Encrypt(string sourceText, byte[] _password)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = _password;
                aes.Padding = PaddingMode.PKCS7;
                using MemoryStream output = new();
                using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
                cryptoStream.Write(Encoding.Unicode.GetBytes(sourceText));
                cryptoStream.FlushFinalBlock();
                cryptoStream.Close();
                return Convert.ToBase64String(output.ToArray()) + "'''" + Convert.ToBase64String(aes.IV); ;
            }
            catch (Exception ex)
            {
                throw new Exception("Error during encrpytion " + ex);
            }
        }
        public static string Decrypt(string encryptedText, byte[] _password)
        {
            try
            {
                byte[] encrpytedBytes = Convert.FromBase64String(encryptedText.Split("'''")[0]);
                using Aes aes = Aes.Create();
                aes.Key = _password;
                aes.IV = Convert.FromBase64String(encryptedText.Split("'''")[1]);
                aes.Padding = PaddingMode.PKCS7;
                using MemoryStream input = new(encrpytedBytes);
                using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using MemoryStream output = new();
                cryptoStream.CopyTo(output);
                return Encoding.Unicode.GetString(output.ToArray());
            }
            catch (CryptographicException ex)
            {
                throw new Exception("Decryption error - It is likely the correct password was not entered " + ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown decryption error " + ex);
            }
        }
        public static void SetMaster(string master)
        {
            SymmetricEncryption.master = Hash(master, "PHATWALRUS!!!");
        }
    }
}

using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CredentialManager
{
    class Credentials
    {
        
        private string username { get; set; }
        private string password { get; set; }
        public Credentials(string username, string password, bool encrypted = true)
        {
            this.username = username;
            if (!encrypted)
            {
                this.password = SymmetricEncryption.Encrypt(password, SymmetricEncryption.master);
            }else
            {
                this.password = password;
            }
        }
        public Credentials(Dictionary<string,string> serializedData)
        {
            // This constructor is for use with deserializers
            this.username = serializedData[nameof(this.username)];
            this.password = serializedData[nameof(this.password)];
        }
        public string[] GetCreds()
        {
            return [username, SymmetricEncryption.Decrypt(password, SymmetricEncryption.master)];
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
        public static string Encrypt(string sourceText, byte[] password)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = password;
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
        public static string Decrypt(string encryptedText, byte[] password)
        {
            try
            {
                byte[] encrpytedBytes = Convert.FromBase64String(encryptedText.Split("'''")[0]);
                using Aes aes = Aes.Create();
                aes.Key = password;
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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;

namespace Backend.CredentialManager
{
    /// <summary>
    /// Checks if a password is hashed or not; if hashed, assigned as the password
    /// if not, hash the password and assign it as the password. Retrieve username and password 
    /// </summary>
    class Users
    {
        [JsonInclude]
        private string username { get; set; }

        [JsonInclude]
        private string password { get; set; }

        [JsonInclude]
        private bool hashed {  get; set; }

        [JsonConstructor]
        public Users(string username, string password, bool hashed = true) 
        {
            this.username = username;
            if (!hashed)
            {
                this.password = Encoding.Unicode.GetString(SymmetricEncryption.Hash(password, "DARTHGOOSE!!!!"));
            }else
            {
                this.password = password;
            }
            this.hashed = true;
        }

        public string GetPassword()
        {
            return password;
        }

        public string GetUsername()
        {
            return username;
        }
    }
        /// <summary>
        /// Encrypts the password if its not already, uses encyrpted password, returns encrypted username and password. 
        /// </summary>
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

        public string[] GetEncrypted()
        {
            return [_username, _password];
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
        public static string? Decrypt(string encryptedText, byte[] _password)
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
                MessageBox.Show("You have selected a save file that was not created by the user you are currently logged in as");
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown decryption error " + ex);
            }
        }
        public static void SetMaster(string master, string username)
        {
            SymmetricEncryption.master = Hash(username + master, "PHATWALRUS!!!");
        }
    }
}

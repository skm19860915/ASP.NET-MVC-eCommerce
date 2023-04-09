using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Configuration;
using System.Text;
using System.IO;

namespace Platini.Models
{
    public class Cryptography
    {
        public static readonly string ENCRYPTION_KEY = ConfigurationManager.AppSettings["ENCRYPTION_KEY"];

        public static string Encrypt(object data)
        {
            return Encrypt(data, ENCRYPTION_KEY);
        }
        public static string Encrypt(object data, string privatekey)
        {
            return Encrypt(data, privatekey, "");
        }
        public static string Encrypt(object data, string privatekey, string salt)
        {
            string strData = data.ToString();
            _Cryptography c = new _Cryptography();
            c.Key = privatekey;
            c.Salt = salt;
            return c.Encrypt(strData);
        }

        public static string Decrypt(object data)
        {
            string result = "";
            if (!Decrypt(data, out result))
            {
                result = "";
            }
            return result;
        }
        public static bool Decrypt(object data, out string result)
        {
            result = "";
            return Decrypt(data, ENCRYPTION_KEY, out result);
        }
        public static bool Decrypt(object data, string privatekey, out string result)
        {
            result = "";
            return Decrypt(data, privatekey, "", out result);
        }
        public static bool Decrypt(object data, string privatekey, string salt, out string result)
        {
            if (string.IsNullOrEmpty(data.ToString()))
            {
                result = "";
                return false;
            }
            _Cryptography c = new _Cryptography();
            c.Key = privatekey;
            c.Salt = salt;
            result = c.Decrypt(data.ToString());
            if (string.IsNullOrEmpty(result))
            {
                result = "";
                return false;
            }
            return true;
        }
        public static string ToBase64String(byte[] input)
        {
            return Convert.ToBase64String(input).Replace('/', '_').Replace('+', '-').TrimEnd('=');
        }
        public static byte[] FromBase64String(string input)
        {
            input = input.Replace('_', '/').Replace('-', '+');
            while (input.Length % 4 > 0)
            {
                input += "=";
            }
            byte[] arrByte = null;
            try
            {
                arrByte = Convert.FromBase64String(input);
            }
            catch { };
            return arrByte;
        }

        /// <summary>
        /// Generated a hex encoded MD5 hash
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MD5(string plainText)
        {
            _Hash h = new _Hash(HashProvider.MD5);
            return h.Encrypt(plainText);
        }
        public enum EncryptionProvider : int
        {
            Rijndael,
            RC2,
            DES,
            TripleDES
        }
        public enum HashProvider : int
        {
            SHA1,
            SHA256,
            SHA384,
            SHA512,
            MD5
        }
        private class _Cryptography
        {

            private string _key = string.Empty;
            public string Key
            {
                get { return _key; }
                set { _key = value; }
            }

            private string _salt = string.Empty;
            public string Salt
            {
                get { return _salt; }
                set { _salt = value; }
            }
            private EncryptionProvider algorithm;
            private SymmetricAlgorithm cryptoService;


            public _Cryptography()
            {
                algorithm = EncryptionProvider.Rijndael;
                Construct();
            }
            public _Cryptography(EncryptionProvider serviceProvider)
            {
                algorithm = serviceProvider;
                Construct();
            }
            //public _Cryptography(string serviceProviderName)
            //{
            //    if (Enum.IsDefined(typeof(EncryptionProvider), serviceProviderName))
            //    {
            //        algorithm = serviceProviderName.ToEnum<EncryptionProvider>();
            //        Construct();
            //    }
            //    else
            //    {
            //        throw new Exception("Unknown SymmetricAlgorithm Cryptograpy Service provider :" + serviceProviderName);
            //    }
            //}

            public virtual string Encrypt(string plainText)
            {
                Random r = new Random();
                plainText = (char)r.Next(128) + plainText;

                byte[] plainByte = ASCIIEncoding.ASCII.GetBytes(plainText);
                byte[] keyByte = GetLegalKey();

                // Set private key
                cryptoService.Key = keyByte;
                SetLegalIV();

                // Encryptor object
                ICryptoTransform cryptoTransform = cryptoService.CreateEncryptor();

                // Memory stream object
                MemoryStream ms = new MemoryStream();

                // Crpto stream object
                CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);

                // Write encrypted byte to memory stream
                cs.Write(plainByte, 0, plainByte.Length);
                cs.FlushFinalBlock();

                // Get the encrypted byte length
                // Convert into base 64 to enable result to be used in Xml
                return ToBase64String(ms.ToArray());
            }
            public virtual string Decrypt(string cryptoText)
            {
                // Convert from base 64 string to bytes
                byte[] cryptoByte = FromBase64String(cryptoText);
                byte[] keyByte = GetLegalKey();

                // Set private key
                cryptoService.Key = keyByte;
                SetLegalIV();

                // Decryptor object
                ICryptoTransform cryptoTransform = cryptoService.CreateDecryptor();
                try
                {
                    // Memory stream object
                    MemoryStream ms = new MemoryStream(cryptoByte, 0, cryptoByte.Length);

                    // Crpto stream object
                    CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Read);

                    // Get the result from the Crypto stream
                    StreamReader sr = new StreamReader(cs);
                    return sr.ReadToEnd().Substring(1);
                }
                catch
                {
                    return null;
                }
            }
            private void Construct()
            {
                cryptoService = (SymmetricAlgorithm)CryptoConfig.CreateFromName(algorithm.ToString());
                cryptoService.Mode = CipherMode.CBC;
            }
            private byte[] GetLegalKey()
            {
                // Adjust key if necessary, and return a valid key
                if (cryptoService.LegalKeySizes.Length > 0)
                {
                    // Key sizes in bits
                    int keySize = _key.Length * 8;
                    int minSize = cryptoService.LegalKeySizes[0].MinSize;
                    int maxSize = cryptoService.LegalKeySizes[0].MaxSize;
                    int skipSize = cryptoService.LegalKeySizes[0].SkipSize;

                    if (keySize > maxSize)
                    {
                        // Extract maximum size allowed
                        _key = _key.Substring(0, maxSize / 8);
                    }
                    else if (keySize < maxSize)
                    {
                        // Set valid size
                        int validSize = (keySize <= minSize) ? minSize : (keySize - keySize % skipSize) + skipSize;
                        if (keySize < validSize)
                        {
                            // Pad the key with asterisk to make up the size
                            _key = _key.PadRight(validSize / 8, '*');
                        }
                    }
                }
                PasswordDeriveBytes key = new PasswordDeriveBytes(_key, ASCIIEncoding.ASCII.GetBytes(_salt));
                return key.GetBytes(_key.Length);
            }
            private void SetLegalIV()
            {
                switch (algorithm)
                {
                    case EncryptionProvider.Rijndael:
                        cryptoService.IV = new byte[] { 0xf, 0x6f, 0x13, 0x2e, 0x35, 0xc2, 0xcd, 0xf9, 0x5, 0x46, 0x9c, 0xea, 0xa8, 0x4b, 0x73, 0xcc };
                        break;
                    default:
                        cryptoService.IV = new byte[] { 0xf, 0x6f, 0x13, 0x2e, 0x35, 0xc2, 0xcd, 0xf9 };
                        break;
                }
            }
        }
        private class _Hash
        {
            private HashAlgorithm cryptoService;

            private string _salt;
            public string Salt
            {
                get { return _salt; }
                set { _salt = value; }
            }
            public _Hash() : this(HashProvider.MD5) { }
            public _Hash(HashProvider serviceProvider) : this(serviceProvider.ToString()) { }
            public _Hash(string serviceProviderName)
            {
                cryptoService = (HashAlgorithm)CryptoConfig.CreateFromName(serviceProviderName.ToUpper());
            }
            public virtual string Encrypt(string plainText)
            {
                return Encrypt(plainText, false);
            }
            public virtual string Encrypt(string plainText, bool returnBase64)
            {
                byte[] cryptoByte = cryptoService.ComputeHash(ASCIIEncoding.ASCII.GetBytes(plainText + _salt));

                // Convert into base 64 to enable result to be used in Xml

                return returnBase64 ? Cryptography.ToBase64String(cryptoByte) : BitConverter.ToString(cryptoByte).Replace("-", "").ToLower();
            }
        }
    
    }
}
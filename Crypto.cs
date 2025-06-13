using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AngelDBTools
{
    public static class Crypto
    {
        public static byte[] Encrypt(byte[] input, string code)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes("2qtowjmowzimgi0ndrm", new byte[] { 0x42, 0x84, 0x23, 0x70 });
            MemoryStream ms = new MemoryStream();
            Aes aes = Aes.Create();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.Close();
            return ms.ToArray();
        }
        public static byte[] Decrypt(byte[] input)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes("2qtowjmowzimgi0ndrm", new byte[] { 0x42, 0x84, 0x23, 0x70 });
            MemoryStream ms = new MemoryStream();
            Aes aes = Aes.Create();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.Close();
            return ms.ToArray();
        }


        public static string EncryptString(string text, string key)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            //byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] keyBytes = new byte[128/8];

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;

                byte[] iv = aes.IV;

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(textBytes, 0, textBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    byte[] encryptedBytes = ms.ToArray();

                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }


        public static void LegalSizeKeys() 
        {
            var key = Encoding.UTF8.GetBytes("mysmallkey");
            //myAes.Key = Key; //ERROR

            using (Aes aes = Aes.Create()) 
            {
                KeySizes[] ks = aes.LegalKeySizes;
                foreach (KeySizes item in ks)
                {
                    Console.WriteLine("Legal min key size = " + item.MinSize);
                    Console.WriteLine("Legal max key size = " + item.MaxSize);
                    //Output
                    // Legal min key size = 128
                    // Legal max key size = 256
                }
            }

        }


        public static string DecryptString(string text, string key)
        {
            byte[] encryptedBytes = Convert.FromBase64String(text);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;

                int ivLength = aes.BlockSize / 8;
                byte[] iv = new byte[ivLength];

                Array.Copy(encryptedBytes, 0, iv, 0, ivLength);
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedBytes, ivLength, encryptedBytes.Length - ivLength);
                        cs.FlushFinalBlock();
                    }

                    byte[] decryptedBytes = ms.ToArray();

                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }



        public static void GenerateCertificate(string password, string userName, string userAddress, string certificatePath, string keyPath)
        {
            // Generar una clave RSA
            using (RSA rsa = RSA.Create(2048))
            {
                // Crear el Distinguished Name con nombre y dirección
                var distinguishedName = new X500DistinguishedName($"CN={userName}, O={userAddress}");

                // Crear la solicitud de certificado con los datos adicionales
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Generar un certificado autofirmado
                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

                // Exportar la clave privada cifrada
                byte[] encryptedPrivateKey = certificate.Export(X509ContentType.Pfx, password);

                // Exportar el certificado en formato DER
                byte[] certData = certificate.Export(X509ContentType.Cert);

                // Guardar el certificado y la clave privada en archivos
                File.WriteAllBytes(certificatePath, certData);
                File.WriteAllBytes(keyPath, encryptedPrivateKey);
            }
        }


    }


    public static class CryptoString
    {
        public static string Encrypt(string textToEncrypt, string publickey, string secretkey)
        {
            try
            {
                string ToReturn = "";
                byte[] secretkeyByte = { };
                secretkeyByte = System.Text.Encoding.UTF8.GetBytes(secretkey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = System.Text.Encoding.UTF8.GetBytes(textToEncrypt);
                using (DES des = DES.Create())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateEncryptor(publickeybyte, secretkeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    ToReturn = Convert.ToBase64String(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        public static string Decrypt(string textToDecrypt, string publickey, string privatekey)
        {
            try
            {
                string ToReturn = "";
                byte[] privatekeyByte = { };
                privatekeyByte = System.Text.Encoding.UTF8.GetBytes(privatekey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = new byte[textToDecrypt.Replace(" ", "+").Length];
                inputbyteArray = Convert.FromBase64String(textToDecrypt.Replace(" ", "+"));
                using (DES des = DES.Create())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateDecryptor(publickeybyte, privatekeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    ToReturn = encoding.GetString(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ae)
            {
                throw new Exception(ae.Message, ae.InnerException);
            }
        }


        public static string sha256_hash(string value)
        {
            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }
    }
}

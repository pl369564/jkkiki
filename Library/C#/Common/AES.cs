using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Modules.Communication
{
    public class AES
    {

        public static byte[] key = { 0x02, 0x05, 0x00, 0x08, 0x01, 0x07, 0x00, 0x01, 0x01, 0x09, 0x09, 0x01, 0x00, 0x07, 0x02, 0x04 };
       
        /// <summary>
        /// 加密
        /// </summary>
        public static byte[] Encrypt(byte[] bytes, byte[] key)
        {
            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
            {
                Key = key,
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7
            };

            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
            int len = bytes.Length / 16 * 16;
            byte[] encryptBuffer = new byte[len];
            Array.Copy(bytes, encryptBuffer, len);
            byte[] result = cTransform.TransformFinalBlock(encryptBuffer, 0, len);
            Array.Copy(result, bytes, len);

            return bytes;
        }

        /// <summary>
        /// 解密
        /// </summary>
        public static byte[] Decryptor(byte[] bytes, byte[] key)
        {

            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
            {
                Key = key,
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = PaddingMode.Zeros

            };

            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateDecryptor();
            int len = bytes.Length / 16 * 16;

            byte[] decryptBuffer = new byte[len];
            Array.Copy(bytes, decryptBuffer, len);
            byte[] result = cTransform.TransformFinalBlock(decryptBuffer, 0, len);
            Array.Copy(result, bytes, len);

            return bytes;
        }

    }
}

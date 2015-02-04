using Duncan.FileCanDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Duncan.FileCanDB
{
    

    public static class SerializeToFile
    {
        private const string EncryptedDetailsFileExtension = ".details";

        public static void SerializeToFileEncryptedBson<T>(T ObjectData, string FilePath, string Password)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JsonSerializer serializer = new JsonSerializer();
                // serialize product to BSON
                using (BsonWriter writer = new BsonWriter(ms))
                {
                    serializer.Serialize(writer, ObjectData);
                    using (var fileStream = new FileStream(FilePath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        ms.Position = 0;
                        byte[] Salt = Encoding.UTF8.GetBytes(Encryption.GetSalt(50));
                        byte[] PasswordByte = Encoding.UTF8.GetBytes(Encryption.GetHash(Password, Salt));
                        byte[] EncryptedData = Encryption.AES_Encrypt(ms.ToArray(), PasswordByte, Salt);

                        EncryptedDetails MyEncryptionDetails = new EncryptedDetails();
                        MyEncryptionDetails.salt = Salt;

                        //Store encryption details in a new file with same name with .s as extension
                        SerializeToFileBson(MyEncryptionDetails, FilePath + EncryptedDetailsFileExtension);

                        using (MemoryStream ems = new MemoryStream(EncryptedData))
                        {
                            ems.WriteTo(fileStream);
                        }
                    }
                }
            }
        }

        public static void SerializeToFileBson<T>(T ObjectData, string FilePath)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JsonSerializer serializer = new JsonSerializer();
                // serialize product to BSON
                using (BsonWriter writer = new BsonWriter(ms))
                {
                    serializer.Serialize(writer, ObjectData);
                    using (var fileStream = new FileStream(FilePath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        ms.Position = 0;
                        ms.WriteTo(fileStream); // fileStream is not populated
                    }
                }
            }
        }

        public static void SerializeToFileJson<T>(T ObjectData, string FilePath)
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(FilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, ObjectData);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

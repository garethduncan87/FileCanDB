using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duncan.FileCanDB
{
    public static class DeserializeFromFile
    {
        private const string EncryptedDetailsFileExtension = ".details";
        public static T DeserializeFromFileJson<T>(string FilePath)
        {
            using (StreamReader sr = new StreamReader(FilePath))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var result = serializer.Deserialize<T>(reader);
                    return result;
                }
            }
        }

        public static T DeserializeFromFileBson<T>(string FilePath)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream input = File.OpenRead(FilePath))
                {
                    input.CopyTo(memoryStream);
                }
                memoryStream.Position = 0;

                BsonReader reader = new BsonReader(memoryStream);
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public static T DeserializeFromFileBsonEncrypted<T>(string FilePath, string Password)
        {
            byte[] unencrypted;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream input = File.OpenRead(FilePath))
                {
                    input.CopyTo(memoryStream);
                }
                memoryStream.Position = 0;

                EncryptedDetails MyEncryptedDetails = new EncryptedDetails();
                MyEncryptedDetails = DeserializeFromFileBson<EncryptedDetails>(FilePath + EncryptedDetailsFileExtension);

                unencrypted = Encryption.AES_Decrypt(memoryStream.ToArray(), Encoding.UTF8.GetBytes(Encryption.GetHash(Password, MyEncryptedDetails.salt)), MyEncryptedDetails.salt);
            }

            if (unencrypted != null || unencrypted.Length != 0)
            {
                using (MemoryStream ms = new MemoryStream(unencrypted))
                {
                    BsonReader reader = new BsonReader(ms);
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(reader);
                }
            }
            return default(T);
        }
    }
}

using Duncan.FileCanDB.Models;
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
    public static class FileReader
    {
        private const string EncryptedDetailsFileExtension = ".details";
        public static PacketModel<T> ReadJson<T>(string FilePath)
        {
            using (StreamReader sr = new StreamReader(FilePath))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var result = serializer.Deserialize<PacketModel<T>>(reader);
                    return result;
                }
            }
        }

        public static PacketModel<T> ReadBson<T>(string FilePath)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream input = File.OpenRead(FilePath))
                    input.CopyTo(memoryStream);

                memoryStream.Position = 0;

                BsonReader reader = new BsonReader(memoryStream);
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<PacketModel<T>>(reader);
            }
        }

        public static PacketModel<T> ReadEncryptedBson<T>(string FilePath, string Password)
        {
            byte[] unencrypted;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream input = File.OpenRead(FilePath))
                    input.CopyTo(memoryStream);

                memoryStream.Position = 0;

                PacketModel<EncryptedDetails> EncryptedDetailsPacketModel = new PacketModel<EncryptedDetails>();
                EncryptedDetails MyEncryptedDetails = new EncryptedDetails();
                EncryptedDetailsPacketModel = ReadBson<EncryptedDetails>(FilePath + EncryptedDetailsFileExtension);
                MyEncryptedDetails = EncryptedDetailsPacketModel.Data;
                unencrypted = Encryption.DecryptBytes(memoryStream.ToArray(), Encoding.UTF8.GetBytes(Encryption.GetHash(Password, MyEncryptedDetails.salt)), MyEncryptedDetails.salt);
            }

            if (unencrypted != null || unencrypted.Length != 0)
            {
                using (MemoryStream ms = new MemoryStream(unencrypted))
                {
                    BsonReader reader = new BsonReader(ms);
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<PacketModel<T>>(reader);
                }
            }
            return default(PacketModel<T>);
        }
    }
}

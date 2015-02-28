using Duncan.FileCanDB;
using Duncan.FileCanDB.Models;
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

        public static bool SerializeToFileEncryptedBson<T>(PacketModel<T> PacketData, string FilePath, string Password)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JsonSerializer serializer = new JsonSerializer();
                // serialize product to BSON
                using (BsonWriter writer = new BsonWriter(ms))
                {
                    serializer.Serialize(writer, PacketData);

                    //If object exists move to delete as can't overwrite
                    bool RequiredToDelete = false;
                    if (File.Exists(FilePath))
                    {
                        File.Move(FilePath, FilePath + ".delete");
                        RequiredToDelete = true;
                    }

                    using (var fileStream = new FileStream(FilePath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        ms.Position = 0;
                        byte[] Salt = Encoding.UTF8.GetBytes(Encryption.GetSalt(50));
                        byte[] PasswordByte = Encoding.UTF8.GetBytes(Encryption.GetHash(Password, Salt));
                        byte[] EncryptedData = Encryption.AES_Encrypt(ms.ToArray(), PasswordByte, Salt);

                        EncryptedDetails MyEncryptionDetails = new EncryptedDetails();
                        MyEncryptionDetails.salt = Salt;

                        PacketModel<EncryptedDetails> MyPacket = new PacketModel<EncryptedDetails>();
                        MyPacket.Data = MyEncryptionDetails;
                        

                        //Store encryption details in a new file with same name with .s as extension
                        SerializeToFileBson<EncryptedDetails>(MyPacket, FilePath + EncryptedDetailsFileExtension);

                        using (MemoryStream ems = new MemoryStream(EncryptedData))
                        {
                            ems.WriteTo(fileStream);
                        }
                    }

                    //Finally delete old file
                    if (RequiredToDelete)
                    {
                        File.Delete(FilePath + ".delete");
                    }

                    return true;
                }
            }
        }

        public static bool SerializeToFileBson<T>(PacketModel<T> PacketData, string FilePath)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JsonSerializer serializer = new JsonSerializer();
                // serialize product to BSON
                using (BsonWriter writer = new BsonWriter(ms))
                {
                    serializer.Serialize(writer, PacketData);
                    
                    //If object exists move to delete as can't overwrite
                    bool RequiredToDelete = false;
                    if(File.Exists(FilePath))
                    {
                        File.Move(FilePath, FilePath + ".delete");
                        RequiredToDelete = true;
                    }
                   
                    using (var fileStream = new FileStream(FilePath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {

                        ms.Position = 0;
                        ms.WriteTo(fileStream); // fileStream is not populated
                    }

                    //Finally delete old file
                    if (RequiredToDelete)
                    {
                        File.Delete(FilePath + ".delete");
                    }
                    return true;
                }
            }
        }

        public static bool SerializeToFileJson<T>(PacketModel<T> PacketData, string FilePath)
        {
            try
            {
                //Move file to delete as can't overwrite
                bool RequiredToDelete = false;
                if (File.Exists(FilePath))
                {
                    File.Move(FilePath, FilePath + ".delete");
                    RequiredToDelete = true;
                }


                using (StreamWriter file = File.CreateText(FilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, PacketData);
                }

                //Finally delete the old file
                if (RequiredToDelete)
                {
                    File.Delete(FilePath + ".delete");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

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
    public enum StorageMethod
    {
        json,
        bson,
        encrypted
    };

    public class FileCanDB
    {
        private const string EncryptedDetailsFileExtension = ".details";
        private string DatabaseLocation;
        private bool EnableIndexing;
        private StorageMethod ChosenStorageMethod;

        public FileCanDB(string DatabaseLocation, StorageMethod StorageMethodValue, bool EnableIndexing)
        {
            this.DatabaseLocation = DatabaseLocation;
            this.ChosenStorageMethod = StorageMethodValue;
            this.EnableIndexing = EnableIndexing;
        }

        public string GenerateId()
        {
            return DateTime.Now.Ticks.ToString() + "-" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// All packets are inserted and made with an ID
        /// </summary>
        /// <param name="PacketId"></param>
        /// <param name="PacketName"></param>
        /// <returns>Bool: Object has been named</returns>
        public void NamePacket(string PacketId, string Collection, string Area, string PacketName)
        {
            string FileDirectory = DatabaseLocation + "\\" + Area + "\\" + Collection + "\\PacketNames";
            if(!Directory.Exists(FileDirectory))
            {
                Directory.CreateDirectory(FileDirectory);
            }

            string FilePath = FileDirectory + "\\"  + PacketId + "_" + PacketName + ".json";
            //Check Name doesn't already exist.
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            PacketNameModel MyPacketName = new PacketNameModel();
            MyPacketName.Id = PacketId;
            MyPacketName.Name = PacketName;

            PacketModel<PacketNameModel> MyPacketModel = new PacketModel<PacketNameModel>();
            MyPacketModel.Data = MyPacketName;
            SerializeToFile.SerializeToFileJson<PacketNameModel>(MyPacketModel, FilePath);
        }

        /// <summary>
        /// Delete packet name link
        /// </summary>
        /// <param name="PacketId"></param>
        /// <param name="Collection"></param>
        /// <param name="Area"></param>
        private void DeletePacketName(string PacketId, string Collection, string Area)
        {
            string FilePath = DatabaseLocation + "\\" + Area + "\\" + Collection + "\\PacketNames";
            if(Directory.Exists(FilePath))
            {
                var PacketNames = Directory.EnumerateFiles(FilePath).Where(m => m.StartsWith(PacketId));
                foreach (string file in PacketNames)
                {
                    if (file.StartsWith(PacketId))
                    {
                        //File found. Delete it
                        File.Delete(file);
                    }
                }
            }
            
        }

        /// <summary>
        /// Inserts object into collection. If password is provided the enum StorageMethod value will be changed to "encrypted".
        /// </summary>
        /// <typeparam name="T">Type of object to store in the database</typeparam>
        /// <param name="PacketData">The object to store in the database</param>
        /// <returns>Returns an ID of the newly inserted object into the database</returns>
        public string InsertPacket<T>(T PacketData, string Area, string Collection, string PacketName = "", string Password = "", List<string> KeyWords = null)
        {
            //Create file name. Use datetime tick so easier to sort files by time created, then append guid to prevent any duplicates
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }
            string FilePath;
            string PacketId = GenerateId();
            //Check if file already exists 
            FilePath = DirectoryPath + "\\" + PacketId + "." + ChosenStorageMethod;
            if (File.Exists(FilePath))
            {
                throw new Exception("Packet already exists. Either delete first or use UpdatePacket");
            }

            //PacketWrapper
            PacketModel<T> MyPacket = new PacketModel<T>();
            MyPacket.Created = DateTime.Now;
            MyPacket.Data = PacketData;
            MyPacket.Modified = DateTime.Now;
            MyPacket.Id = PacketId;

            //Serialise object using Json.net
            switch (ChosenStorageMethod)
            {
                case StorageMethod.encrypted:
                    {
                        SerializeToFile.SerializeToFileEncryptedBson<T>(MyPacket, FilePath, Password);
                        break;
                    }
                case StorageMethod.bson:
                    {
                        SerializeToFile.SerializeToFileBson<T>(MyPacket, FilePath);
                        break;
                    }
                default:
                    {
                        SerializeToFile.SerializeToFileJson<T>(MyPacket, FilePath);
                        break;
                    }
            }

            //Index object
            if (KeyWords != null && EnableIndexing)
            {
                Indexing.IndexObject(DatabaseLocation, PacketId, Area, Collection, KeyWords);
            }

            //Name the packet if it has been provided a name
            if (!string.IsNullOrEmpty(PacketName))
            {
                //Give packet a name
                NamePacket(PacketId, Collection, Area, PacketName);
            }

            return PacketId;
        }

        public bool UpdatePacket<T>(string PacketId, T PacketData, string Area, string Collection, string Password = "", List<string> KeyWords = null)
        {
            // deserialize product from BSON
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            if (Directory.Exists(DirectoryPath))
            {
                //Possible file path
                string FilePath = DirectoryPath + "\\" + PacketId + "." + ChosenStorageMethod;

                //If File path exists
                if (File.Exists(FilePath))
                {
                    //PacketWrapper
                    PacketModel<T> MyPacket = new PacketModel<T>();
                    MyPacket.Created = DateTime.Now;
                    MyPacket.Data = PacketData;
                    MyPacket.Modified = DateTime.Now;
                    MyPacket.Id = PacketId;

                    if (ChosenStorageMethod == StorageMethod.encrypted)
                    {
                        //bson encrypted method
                        SerializeToFile.SerializeToFileEncryptedBson<T>(MyPacket, FilePath, Password);
                    }
                    else if (ChosenStorageMethod == StorageMethod.bson)
                    {
                        //return bson
                        SerializeToFile.SerializeToFileBson<T>(MyPacket, FilePath);
                    }
                    else
                    {
                        //return json
                        SerializeToFile.SerializeToFileJson<T>(MyPacket, FilePath);
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// Deletes an object from the database
        /// </summary>
        /// <param name="PacketId">Object ID</param>
        /// <param name="Area">Database ID</param>
        /// <param name="Collection">Collection ID</param>
        /// <returns>Returns true if file has been deleted</returns>
        public bool DeletePacket(string PacketId, string Area, string Collection)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            bool FileDeleted = false;
            if (Directory.Exists(DirectoryPath))
            {
                //Flag. Parallel loop can't return bool but can break, so set flag to indicate file has been deleted.

                //Possible file path
                string FilePath = DirectoryPath + "\\" + PacketId + "." + ChosenStorageMethod;

                if (File.Exists(FilePath))
                {

                    File.Delete(FilePath);
                    if (!File.Exists(FilePath))
                    {
                        //Check if encrypted details file exist with it. If so, delete it
                        if (File.Exists(FilePath + EncryptedDetailsFileExtension))
                        {
                            File.Delete(FilePath + EncryptedDetailsFileExtension);
                        }
                        FileDeleted = true;
                    }

                    //If packet has been named, then delete the naming file
                    DeletePacketName(PacketId, Collection, Area);

                    //Delete object from index file
                    if (EnableIndexing)
                    {
                        Indexing.DeleteObjectIndexRecord(DatabaseLocation, PacketId, Area, Collection);
                    }

                    //Check how many files left in directory. if 0, delete directory.
                    int FileCount = Directory.EnumerateFiles(DirectoryPath).Count();
                    if (FileCount == 0)
                    {
                        Directory.Delete(DirectoryPath);
                    }
                }
            }
            return FileDeleted;
        }

        public IList<string> FindObjects<T>(string query, string Area, string Collection, int skip, int take)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            string IndexPath = DatabaseLocation + "\\" + Area + "\\" + Collection + "\\index.txt";

            List<string> searchwords = query.Split(' ').ToList();
            List<string> ObjectIdsFound = new List<string>();
            return ObjectIdsFound;
        }

        public IList<string> FindObjectsUsingKeywords(string query, string Area, string Collection, int skip, int take)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            string IndexPath = DatabaseLocation + "\\" + Area + "\\" + Collection + "\\index.txt";

            List<string> searchwords = query.Split(' ').ToList();

            List<string> ObjectIdsFound = new List<string>();

            string line;
            if (File.Exists(IndexPath))
            {
                using (var sr = new StreamReader(IndexPath))
                {
                    while ((line = sr.ReadLine()) != null)
                    {

                        foreach (string word in searchwords)
                        {
                            //keyword in index file
                            string keyword = line.Split(' ')[0];
                            if (keyword.ToLower().Contains(word.ToLower()))
                            {
                                //return list of object ids
                                string objectIds = line.Split(' ')[1];
                                List<string> objectIdsList = objectIds.Split(',').ToList();
                                ObjectIdsFound.AddRange(objectIdsList);
                                ObjectIdsFound = ObjectIdsFound.Distinct().ToList();
                            }
                        }
                    }
                }
            }
                
            return ObjectIdsFound;
        }

        /// <summary>
        /// List all object ids in a collection
        /// </summary>
        /// <param name="Area"></param>
        /// <param name="Collection"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public IEnumerable<string> ListPackets(string Area, string Collection, int skip = 0, int take = 0)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            if (Directory.Exists(DirectoryPath))
            {
                if (take != 0)
                {
                    return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Skip(skip).Take(take).Select(x => Path.GetFileNameWithoutExtension(x));
                }
                return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Skip(skip).Select(x => Path.GetFileNameWithoutExtension(x));
            }
            return null;
        }

        public int CollectionPacketCount(string Area, string Collection)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Count();
            }
            return 0;
        }

        public int AreaCollectionsCount(string Area)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetDirectories(DirectoryPath).Count();
            }
            return 0;
        }

        /// <summary>
        /// Gets all objects in a databases collection. Optionl password parameter. Parallel method.
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Area"></param>
        /// <param name="Collection"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public IList<PacketModel<T>> GetPackets<T>(string Area, string Collection, int skip, int take, string Password = "")
        {
            IEnumerable<string> ObjectIds = ListPackets(Area, Collection, skip, take);
            IList<PacketModel<T>> Objects = new List<PacketModel<T>>();
            Parallel.ForEach(ObjectIds, ObjectId =>
            {
                var Object = GetPacket<T>(ObjectId, Area, Collection, Password);
                Objects.Add(Object);
            });
            return Objects;
        }

        /// <summary>
        /// Get packet by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketName"></param>
        /// <param name="Area"></param>
        /// <param name="Collection"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public PacketModel<T> GetPacketByName<T>(string PacketName, string Area, string Collection, string Password = "")
        {
            //Get packetid from name
            string PacketId = GetPacketId(PacketName, Area, Collection);
            return GetPacket<T>(PacketId, Area, Collection, Password);
        }

        /// <summary>
        /// Get Id of Packet
        /// </summary>
        /// <param name="PacketName"></param>
        /// <param name="Area"></param>
        /// <param name="Collection"></param>
        /// <returns></returns>
        private string GetPacketId(string PacketName, string Area, string Collection)
        {
            PacketNameModel MyPacketName = new PacketNameModel();
            string FileDirectory = DatabaseLocation + "\\" + Area + "\\" + Collection + "\\PacketNames\\";

            if(Directory.Exists(FileDirectory))
            {
                var filepath = Directory.EnumerateFiles(FileDirectory).Where(m => m.Contains(PacketName)).FirstOrDefault();
                PacketModel<PacketNameModel> MyPackerModel = new PacketModel<PacketNameModel>();


                PacketModel<PacketNameModel> result = DeserializeFromFile.DeserializeFromFileJson<PacketNameModel>(filepath);
                return result.Data.Id;
            }
            return null;          
        }

        /// <summary>
        /// Get object by packet file path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="FilePath">File path of packet</param>
        /// <param name="Password">Password if file is encrypted</param>
        /// <returns>Returns object</returns>
        public PacketModel<T> GetPacket<T>(string FilePath, string Password = "")
        {
            return GetPacketData<T>(FilePath, Password);
        }

        /// <summary>
        /// Get an object from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ObjectId">Object ID</param>
        /// <param name="Area">Database ID</param>
        /// <param name="Collection">Collection ID</param>
        /// <param name="Password">Optional password parametert to enable encryption</param>
        /// <returns>Object stored in database</returns>
        public PacketModel<T> GetPacket<T>(string PacketId, string Area, string Collection, string Password = "")
        {
            // deserialize product from BSON
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;
            if (Directory.Exists(DirectoryPath))
            {
                string FilePath = DirectoryPath + "\\" + PacketId + "." + ChosenStorageMethod;
                //If File path exists
                return GetPacketData<T>(FilePath, Password);           
            }
            return default(PacketModel<T>);
        }

        private PacketModel<T> GetPacketData<T>(string FilePath, string Password = "")
        {
            //If File path exists
            if (File.Exists(FilePath))
            {
                if (ChosenStorageMethod == StorageMethod.encrypted)
                {
                    //bson encrypted method
                    return DeserializeFromFile.DeserializeFromFileBsonEncrypted<T>(FilePath, Password);
                }
                else if (ChosenStorageMethod == StorageMethod.bson)
                {
                    //return bson
                    return DeserializeFromFile.DeserializeFromFileBson<T>(FilePath);
                }
                else
                {
                    //return json
                    return DeserializeFromFile.DeserializeFromFileJson<T>(FilePath);
                }
            }
            return default(PacketModel<T>);
        }

        /// <summary>
        /// Returns a list of Collection names found in a database
        /// </summary>
        /// <param name="Area"></param>
        /// <returns>An IList of string</returns>
        public IList<string> GetCollections(string Area)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetDirectories(DirectoryPath).Select(m => m.Substring(m.LastIndexOf('\\') + 1)).ToList();
            }
            return null;
            
        }

        /// <summary>
        /// Delete a database collection. Warning! Will delete all sub files and folders
        /// </summary>
        /// <param name="Area">Database Id</param>
        /// <param name="Collection">Collection Id</param>
        /// <returns>Returns true if the collection was deleted</returns>
        public bool DeleteCollection(string Area, string Collection)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area + "\\" + Collection;

            if(Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, true);
            }
            if(Directory.Exists(DirectoryPath))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Delete a database. Warning! Will remove the database folder including all sub folders and files. 
        /// </summary>
        /// <param name="Area"></param>
        /// <returns>Returns true if the datbase was deleted</returns>
        public bool DeleteDatabase(string Area)
        {
            string DirectoryPath = DatabaseLocation + "\\" + Area;
            Directory.Delete(DirectoryPath, true);
            if (Directory.Exists(DirectoryPath))
            {
                return false;
            }
            return true;
        }
    }
}
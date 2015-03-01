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
    public enum StorageType
    {
        json,
        bson,
        encrypted
    };

    public class FileCanDB<T> : IFileCanDB<T>
    {
        private const string _encryptedDetailsFileExtension = ".details";
        private const string _collecitonIndexFilename = "index.txt";
        private const string _packetNamesFolderName = "PacketNames";
        private string _databaseLocation;
        private bool _enableIndexing;
        private StorageType _storageType;
        private string _area;
        private string _areaPath;
        private string _collection;
        private string _collectionPath;
        private string _collectionPacketNamesPath;
        private string _collectionIndexPath;

        public FileCanDB(string DatabaseLocation, string Area, string Collection, StorageType StorageType, bool EnableIndexing)
        {
            this._databaseLocation = DatabaseLocation;
            this._storageType = StorageType;
            this._enableIndexing = EnableIndexing;
            this._area = Area;
            this._areaPath = Path.Combine(DatabaseLocation, Area);
            this._collection = Collection;
            this._collectionPath = Path.Combine(_databaseLocation, _area, _collection);
            this._collectionPacketNamesPath = Path.Combine(_databaseLocation, _area, _collection, _packetNamesFolderName);
            this._collectionIndexPath = Path.Combine(_collectionPath, _collecitonIndexFilename);
        }


        /// <summary>
        /// Generate an Id that uses DateTime Ticks and a Guid number
        /// </summary>
        /// <returns></returns>
        private string generateId()
        {
            return DateTime.Now.Ticks.ToString() + "-" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Name a Packet already in the database
        /// </summary>
        /// <param name="PacketId">Packet id</param>
        /// <param name="PacketName">Packet Name</param>
        /// <returns>Bool: Packet has been named</returns>
        public void NamePacket(string PacketId, string PacketName)
        {

            
            if (!Directory.Exists(_collectionPacketNamesPath))
            {
                Directory.CreateDirectory(_collectionPacketNamesPath);
            }

            string FilePath = Path.Combine(_collectionPacketNamesPath, PacketId + "_" + PacketName + ".json");
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
        private void DeletePacketName(string PacketId)
        {
            if (Directory.Exists(_collectionPacketNamesPath))
            {
                IEnumerable<string> PacketNames = Directory.EnumerateFiles(_collectionPacketNamesPath).Where(m => m.StartsWith(PacketId));
                Parallel.ForEach(PacketNames, packetname =>
                {
                    if (packetname.StartsWith(PacketId))
                    {
                        //File found. Delete it
                        File.Delete(packetname);
                    }
                });
            }  
        }

        /// <summary>
        /// Check that a Packet exists.
        /// </summary>
        /// <param name="PacketId">Packet Id</param>
        /// <returns>bool: Returns true if packet exists</returns>
        public bool CheckPacketExits(string PacketId)
        {
            if (!Directory.Exists(_collectionPath))
            {
                string PacketPath = Path.Combine(_collectionPath, PacketId + "." + _storageType);
                if(File.Exists(PacketPath))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Create a packet package
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId"></param>
        /// <param name="Data"></param>
        /// <returns>PacketModel: Returns a package containing the packet data provided</returns>
        private PacketModel<T> CreatePackage(string PacketId, T Data)
        {
             //PacketWrapper
            PacketModel<T> MyPacket = new PacketModel<T>();
            MyPacket.Created = DateTime.Now;
            MyPacket.Data = Data;
            MyPacket.Modified = DateTime.Now;
            MyPacket.Id = PacketId;
            return MyPacket;
        }

        private string createPackagePath(string PacketId)
        {
            //Create file name. Use datetime tick so easier to sort files by time created, then append guid to prevent any duplicates
            if (!Directory.Exists(_collectionPath))
            {
                Directory.CreateDirectory(_collectionPath);
            }
            string PacketPath;
            //Check if file already exists 
            PacketPath = Path.Combine(_collectionPath, PacketId + "." + _storageType);
            if (File.Exists(PacketPath))
            {
                throw new Exception("Packet already exists. Either delete first or use UpdatePacket");
            }
            return PacketPath;
        }



        /// <summary>
        /// Inserts Packet into collection.
        /// </summary>
        /// <typeparam name="T">Type of Packet to store in the database</typeparam>
        /// <param name="PacketData">The Packet to store in the database</param>
        /// <returns>string: Returns automaticlally generated Id of inputed packet</returns>
        public string InsertPacket(T PacketData)
        {
            return insertPacket(PacketData);
        }

        /// <summary>
        /// Inserts Packet in an encrypted form into collection.
        /// </summary>
        /// <typeparam name="T">Type of Packet to store in the database</typeparam>
        /// <param name="PacketData">The Packet to store in the database</param>
        /// <returns>string: Returns automaticlally generated Id of inputed packet</returns>
        public string InsertPacket(T PacketData, string Password)
        {
            return insertPacket(PacketData, Password);
        }

        /// <summary>
        /// Inserts Packet into collection. If password is provided the enum StorageMethod value will be changed to "encrypted".
        /// </summary>
        /// <typeparam name="T">Type of Packet to store in the database</typeparam>
        /// <param name="PacketData">The Packet to store in the database</param>
        /// <returns>string: Returns automaticlally generated Id of inputed packet</returns>
        private string insertPacket(T PacketData, string Password)
        {
            if(_storageType != StorageType.encrypted)
            {
                throw new Exception("StorageType is not set to encrypted.");
            }

            string PacketId = generateId();
            string PacketPath = createPackagePath(PacketId);

            PacketModel<T> PackagedData = CreatePackage(PacketId, PacketData);

            //Serialise Packet using Json.net
            SerializeToFile.SerializeToFileEncryptedBson<T>(PackagedData, PacketPath, Password);

            return PacketId;
        }

        private string insertPacket(T PacketData)
        {
            //Check if the database has been configured to encrypted
            if (_storageType == StorageType.encrypted)
            {
                throw new Exception("Password required as StorageType is set to encrypted");
            }

            string PacketId = generateId();
            string PacketPath = createPackagePath(PacketId);

            PacketModel<T> PackagedData = CreatePackage(PacketId, PacketData);

            //Serialise Packet using Json.net
            switch (_storageType)
            {
                case StorageType.bson:
                    {
                        SerializeToFile.SerializeToFileBson<T>(PackagedData, PacketPath);
                        break;
                    }
                default:
                    {
                        SerializeToFile.SerializeToFileJson<T>(PackagedData, PacketPath);
                        break;
                    }
            }

            return PacketId;
        }

        /// <summary>
        /// Update a packet
        /// </summary>
        /// <typeparam name="T">Packet data type</typeparam>
        /// <param name="PacketId">Packet Id</param>
        /// <param name="PacketData">Packet Data</param>
        /// <param name="KeyWords">Packet keywords</param>
        /// <returns></returns>
        public bool UpdatePacket(string PacketId, T PacketData)
        {
            return updatePacket(PacketId, PacketData);
        }

        /// <summary>
        /// Update an encrypted packet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId"></param>
        /// <param name="PacketData"></param>
        /// <param name="Password"></param>
        /// <param name="KeyWords"></param>
        /// <returns></returns>
        public bool UpdatePacket(string PacketId, T PacketData, string Password)
        {
            return updatePacket(PacketId, PacketData, Password);
        }

        private bool updatePacket(string PacketId, T PacketData, string Password = "")
        {
            // deserialize product from BSON
            if (Directory.Exists(_collectionPath))
            {
                //Possible file path
                string PacketPath = Path.Combine(_collectionPath, PacketId + "." + _storageType);

                //If File path exists
                if (File.Exists(PacketPath))
                {
                    //PacketWrapper
                    PacketModel<T> MyPacket = new PacketModel<T>();
                    MyPacket.Created = DateTime.Now;
                    MyPacket.Data = PacketData;
                    MyPacket.Modified = DateTime.Now;
                    MyPacket.Id = PacketId;

                    if (_storageType == StorageType.encrypted)
                    {
                        //bson encrypted method
                        SerializeToFile.SerializeToFileEncryptedBson<T>(MyPacket, PacketPath, Password);
                    }
                    else if (_storageType == StorageType.bson)
                    {
                        //return bson
                        SerializeToFile.SerializeToFileBson<T>(MyPacket, PacketPath);
                    }
                    else
                    {
                        //return json
                        SerializeToFile.SerializeToFileJson<T>(MyPacket, PacketPath);
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// Deletes an Packet from the database
        /// </summary>
        /// <param name="PacketId">Packet ID</param>
        /// <param name="Area">Database ID</param>
        /// <param name="Collection">Collection ID</param>
        /// <returns>Returns true if file has been deleted</returns>
        public bool DeletePacket(string PacketId)
        {
            bool FileDeleted = false;
            if (Directory.Exists(_collectionPath))
            {
                //Flag. Parallel loop can't return bool but can break, so set flag to indicate file has been deleted.

                //Possible file path
                string PacketPath = Path.Combine(_collectionPath,  PacketId + "." + _storageType);

                if (File.Exists(PacketPath))
                {

                    File.Delete(PacketPath);
                    if (!File.Exists(PacketPath))
                    {
                        //Check if encrypted details file exist with it. If so, delete it
                        if (File.Exists(PacketPath + _encryptedDetailsFileExtension))
                        {
                            File.Delete(PacketPath + _encryptedDetailsFileExtension);
                        }
                        FileDeleted = true;
                    }

                    //If packet has been named, then delete the naming file
                    DeletePacketName(PacketId);

                    //Delete packet from index file
                    if (_enableIndexing)
                    {
                        DeletePacketIndex(PacketId);
                    }

                    //Check how many files left in directory. if 0, delete directory.
                    int FileCount = Directory.EnumerateFiles(_collectionPath).Count();
                    if (FileCount == 0)
                    {
                        Directory.Delete(_collectionPath);
                    }
                }
            }
            return FileDeleted;
        }

        public IEnumerable<string> FindPacketsUsingIndex(string query, int skip, int take)
        {
            IList<string> searchwords = query.Split(' ');
            List<string> PacketIdsFound = new List<string>();

            if (!File.Exists(_collectionIndexPath))
            {
                return PacketIdsFound;
            }

            Parallel.ForEach(File.ReadLines(_collectionIndexPath), line =>
            {
                foreach (string word in searchwords)
                {
                    if (line.Split(' ')[0].ToLower().Contains(word.ToLower()))
                    {
                        //return list of packet ids
                        string packetIds = line.Split(' ')[1];
                        IEnumerable<string> packetIdsList = packetIds.Split(',');
                        PacketIdsFound.AddRange(packetIdsList);
                        PacketIdsFound = PacketIdsFound.Distinct().ToList();
                    }
                }
            });

            return PacketIdsFound.OrderBy(m => m).Skip(skip).Take(take);
        }

        /// <summary>
        /// List all packet ids in a collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ListPackets()
        {
            return listPackets();
        }

        /// <summary>
        /// List all packet ids in a collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ListPackets(int skip)
        {
            return listPackets(skip);
        }

        /// <summary>
        /// List all packet ids in a collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ListPackets(int skip, int take)
        {
            return listPackets(skip, take);
        }

        private IEnumerable<string> listPackets(int skip = 0, int take = 0)
        {
            if (Directory.Exists(_collectionPath))
            {
                if (take != 0)
                {
                    return Directory.GetFiles(_collectionPath, "*." + _storageType, SearchOption.AllDirectories).Skip(skip).Take(take).Select(x => Path.GetFileNameWithoutExtension(x));
                }
                return Directory.GetFiles(_collectionPath, "*." + _storageType, SearchOption.AllDirectories).Skip(skip).Select(x => Path.GetFileNameWithoutExtension(x));
            }
            return null;
        }

        public int CollectionPacketCount()
        {
            if (Directory.Exists(_collectionPath))
            {
                return Directory.GetFiles(_collectionPath, "*." + _storageType, SearchOption.AllDirectories).Count();
            }
            return 0;
        }

        public int AreaCollectionsCount()
        {
            if (Directory.Exists(_areaPath))
            {
                return Directory.GetDirectories(_areaPath).Count();
            }
            return 0;
        }

        /// <summary>
        /// Gets all packets in a databases collection based on a query provided
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Password"></param>
        /// <returns>List: Returns a list of packet</returns>
        public IList<PacketModel<T>> GetPackets(string query, int skip, int take)
        {
            return getPackets(null, skip, take);
        }

        /// <summary>
        /// Gets all packets in a databases collection based on a query provided
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Password"></param>
        /// <returns>List: Returns a list of packet</returns>
        public IList<PacketModel<T>> GetPackets(string query, int skip, int take, string Password)
        {
            return getPackets(null, skip, take, Password);
        }

        /// <summary>
        /// Gets all packets in a databases collection. Optionl password parameter. Parallel method.
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Password"></param>
        /// <returns></returns>
        public IList<PacketModel<T>> GetPackets(int skip, int take)
        {
            return getPackets(null, skip, take);
        }

        /// <summary>
        /// Gets all encrypted packets in a databases collection. Optionl password parameter. Parallel method.
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Password"></param>
        /// <returns></returns>
        public IList<PacketModel<T>> GetPackets(int skip, int take, string Password)
        {
            return getPackets(null, skip, take, Password);
        }

        private IList<PacketModel<T>> getPackets(string query, int skip, int take, string Password = "")
        {
            IEnumerable<string> PacketIds;
            if(string.IsNullOrEmpty(query))
            {
                PacketIds = ListPackets(skip, take);
            }
            else
            {
                PacketIds = FindPacketsUsingIndex(query, skip, take);
            }

            IList<PacketModel<T>> Packets = new List<PacketModel<T>>();
            Parallel.ForEach(PacketIds, PacketId =>
            {
                var Packet = GetPacket(PacketId, Password);
                Packets.Add(Packet);
            });
            return Packets;
        }

        /// <summary>
        /// Get packet by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketName"></param>
        /// <param name="Area"></param>
        /// <param name="Collection"></param>
        /// <returns></returns>
        public PacketModel<T> GetPacketByName(string PacketName)
        {
            //Get packetid from name
            string PacketId = GetPacketId(PacketName);
            return GetPacket(PacketId);
        }

        /// <summary>
        /// Get encrypted packet by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketName"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public PacketModel<T> GetPacketByName(string PacketName, string Password)
        {
            //Get packetid from name
            string PacketId = GetPacketId(PacketName);
            return GetPacket(PacketId, Password);
        }

        /// <summary>
        /// Get Id of Packet by its name
        /// </summary>
        /// <param name="PacketName"></param>
        /// <returns></returns>
        public string GetPacketId(string PacketName)
        {
            PacketNameModel MyPacketName = new PacketNameModel();
            if(Directory.Exists(_collectionPacketNamesPath))
            {
                var filepath = Directory.EnumerateFiles(_collectionPacketNamesPath).Where(m => m.Contains(PacketName)).FirstOrDefault();
                PacketModel<PacketNameModel> MyPackerModel = new PacketModel<PacketNameModel>();


                PacketModel<PacketNameModel> result = DeserializeFromFile.DeserializeFromFileJson<PacketNameModel>(filepath);
                return result.Data.Id;
            }
            return null;          
        }

        /// <summary>
        /// Get an packet from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId">Packet ID</param>
        /// <returns>Packet stored in database</returns>
        public PacketModel<T> GetPacket(string PacketId)
        {
            return _getPacket(PacketId);
        }

        /// <summary>
        /// Get an packet from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId">Packet ID</param>
        /// <param name="Password">Optional password parametert to enable encryption</param>
        /// <returns>Packet stored in database</returns>
        public PacketModel<T> GetPacket(string PacketId, string Password)
        {
            return _getPacket(PacketId, Password);
        }

        private PacketModel<T> _getPacket(string PacketId, string Password = "")
        {
            string PacketFilePath = Path.Combine(_collectionPath, PacketId + "." + _storageType);

            //If File path exists
            if (File.Exists(PacketFilePath))
            {
                if (_storageType == StorageType.encrypted)
                {
                    //bson encrypted method
                    return DeserializeFromFile.DeserializeFromFileBsonEncrypted<T>(PacketFilePath, Password);
                }
                else if (_storageType == StorageType.bson)
                {
                    //return bson
                    return DeserializeFromFile.DeserializeFromFileBson<T>(PacketFilePath);
                }
                else
                {
                    //return json
                    return DeserializeFromFile.DeserializeFromFileJson<T>(PacketFilePath);
                }
            }
            return default(PacketModel<T>);
        }



        /// <summary>
        /// Returns a list of Collection names found in a database
        /// </summary>
        /// <param name="Area"></param>
        /// <returns>An IList of string</returns>
        public IList<string> GetCollections()
        {
            if (Directory.Exists(_areaPath))
            {
                return Directory.GetDirectories(_areaPath).Select(m => m.Substring(m.LastIndexOf('\\') + 1)).ToList();
            }
            return null;
            
        }

        /// <summary>
        /// Delete a database collection. Warning! Will delete all sub files and folders
        /// </summary>
        /// <param name="Area">Database Id</param>
        /// <param name="Collection">Collection Id</param>
        /// <returns>Returns true if the collection was deleted</returns>
        public bool DeleteCollection()
        {
            if(Directory.Exists(_collectionPath))
            {
                Directory.Delete(_collectionPath, true);
            }
            if (Directory.Exists(_collectionPath))
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
        public bool DeleteDatabase()
        {
            Directory.Delete(_areaPath, true);
            if (Directory.Exists(_areaPath))
            {
                return false;
            }
            return true;
        }

        public void IndexPacket(string PacketId, List<string> KeyWords)
        {
            if (!File.Exists(_collectionIndexPath))
            {
                //create file
                FileStream fs1 = new FileStream(_collectionIndexPath, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(fs1);
                writer.Close();
            }

            string tempFile = Path.GetTempFileName();
            using (var sr = new StreamReader(_collectionIndexPath))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;
                bool indexadded = false;
                while ((line = sr.ReadLine()) != null)
                {

                    string keyword = line.Split(' ')[0];
                    if (KeyWords.Contains(keyword))
                    {
                        //line found
                        string PacketIds = line.Split(' ')[1];
                        List<string> PacketIdsList = PacketIds.Split(',').ToList();
                        if (PacketIdsList.Contains(PacketId))
                        {
                            //Write line
                            sw.WriteLine(line);
                        }
                        else
                        {
                            //Add to list
                            PacketIdsList.Add(PacketId);
                            sw.WriteLine(keyword + " " + string.Join(",", new List<string>(PacketIdsList).ToArray()));
                        }
                        indexadded = true;
                    }
                    else
                    {
                        sw.WriteLine(line);
                    }
                }

                if (sr.ReadLine() != null || indexadded == false)
                {
                    //file is empty so add to index
                    //for each keyword add record
                    foreach (string keyword in KeyWords)
                    {
                        sw.WriteLine(keyword + " " + PacketId);
                    }

                }
            }

            File.Delete(_collectionIndexPath);
            File.Move(tempFile, _collectionIndexPath);
        }

        public void DeletePacketIndex(string PacketId)
        {
            if (File.Exists(_collectionIndexPath))
            {
                string tempFile = Path.GetTempFileName();
                using (var sr = new StreamReader(_collectionIndexPath))
                using (var sw = new StreamWriter(tempFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains(PacketId))
                        {
                            //Remove Packetid from list and re create the line
                            string keyword = line.Split(' ')[0];
                            string PacketIds = line.Split(' ')[1];
                            List<string> PacketIdsList = PacketIds.Split(',').ToList();
                            PacketIdsList.Remove(PacketId);

                            //If there are Packets in the list, then right the line to the index file
                            if (PacketIdsList.Count > 0)
                            {
                                sw.WriteLine(keyword + " " + string.Join(",", new List<string>(PacketIdsList).ToArray()));
                            }
                        }
                        else
                        {
                            //If the line does not contain the Packet id, then write the line back to the index file
                            sw.WriteLine(line);
                        }
                    }
                }

                //Delete the original index file
                File.Delete(_collectionIndexPath);

                //recreate the new index file
                if (File.ReadLines(tempFile).Count() > 0)
                {
                    File.Move(tempFile, _collectionIndexPath);
                }
            }

        }
    }
}
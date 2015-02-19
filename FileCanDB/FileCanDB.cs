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

    public class FileCanDB : IFileCanDB
    {
        private const string EncryptedDetailsFileExtension = ".details";
        private string DbPath;
        private bool EnableIndexing;
        private StorageMethod ChosenStorageMethod;

        public FileCanDB(string DatabaseStorePath, StorageMethod StorageMethodValue, bool EnableIndexing)
        {
            this.DbPath = DatabaseStorePath;
            this.ChosenStorageMethod = StorageMethodValue;
            this.EnableIndexing = EnableIndexing;
        }

        /// <summary>
        /// Inserts object into collection. If password is provided the enum StorageMethod value will be changed to "encrypted".
        /// </summary>
        /// <typeparam name="T">Type of object to store in the database</typeparam>
        /// <param name="ObjectData">The object to store in the database</param>
        /// <returns>Returns an ID of the newly inserted object into the database</returns>
        public string InsertObject<T>(T ObjectData, string DatabaseId, string CollectionId, string Password = "", List<string> KeyWords = null)
        {
            //Create file name. Use datetime tick so easier to sort files by time created, then append guid to prevent any duplicates
            
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            string DirectoryBlockPath;
            string FilePath;

            //Does Directory for collection exist?
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath + "\\1");
            }

            //Count number of blocks in a collection. If 0, then create a directory named '1'
            int DirectoryBlockCount = Directory.EnumerateDirectories(DirectoryPath).Count();
            if (DirectoryBlockCount == 0)
            {
                Directory.CreateDirectory(DirectoryPath + "\\1");
            }
            
            //Get latest number value in folder
            //Get all directory paths, split path name after last \\
            //Then convert list to an int
            //Finally select the maximum value
            int LatestBlockNumber = Directory.EnumerateDirectories(DirectoryPath).Select(m => m.Split('\\').Last()).Select(int.Parse).Max();
            string FileName = LatestBlockNumber + "_" + DateTime.Now.Ticks.ToString() + "-" + Guid.NewGuid().ToString("N");
            //Check count in this directory - if less than 10 thousand then create new block
            DirectoryBlockPath = DirectoryPath + "\\" + LatestBlockNumber.ToString();
            int NumberOfFilesInBlock = Directory.EnumerateFiles(DirectoryBlockPath).Count();
 
            if (NumberOfFilesInBlock >= 1000)
            {
                LatestBlockNumber = LatestBlockNumber + 1;
                DirectoryBlockPath = DirectoryPath + "\\" + (LatestBlockNumber);
                Directory.CreateDirectory(DirectoryBlockPath);
            }

            //Create file path. The first part of an objectid is the Block number the file is found in
            FilePath = DirectoryBlockPath + "\\" + FileName + "." + ChosenStorageMethod;

            //Serialise object using Json.net
            if (ChosenStorageMethod == StorageMethod.encrypted)
            {
                SerializeToFile.SerializeToFileEncryptedBson<T>(ObjectData, FilePath, Password);
            }
            else if (ChosenStorageMethod == StorageMethod.json)
            {
                SerializeToFile.SerializeToFileJson<T>(ObjectData, FilePath);
            }
            else
            {
                SerializeToFile.SerializeToFileBson<T>(ObjectData, FilePath);
            }

            //Index object
            if (KeyWords != null && EnableIndexing)
            {
                Indexing.IndexObject(DbPath, FileName, DatabaseId, CollectionId, KeyWords);
            }

            return FileName;
        }

        public bool UpdateObject<T>(string ObjectId, T ObjectData, string DatabaseId, string CollectionId, string Password = "", List<string> KeyWords = null)
        {
            // deserialize product from BSON
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                //First part of any object id is the block path
                //This should speed up the process if getting an object
                string BlockNumber = string.Empty;
                int l = ObjectId.IndexOf("_");
                if (l > 0)
                {
                    BlockNumber = ObjectId.Substring(0, l);
                }

                //Possible file path
                string FilePath = DirectoryPath + "\\" + BlockNumber + "\\" + ObjectId + "." + ChosenStorageMethod;

                //If File path exists
                if (File.Exists(FilePath))
                {
                    if (ChosenStorageMethod == StorageMethod.encrypted)
                    {
                        //bson encrypted method
                        SerializeToFile.SerializeToFileEncryptedBson<T>(ObjectData, FilePath, Password);
                    }
                    else if (ChosenStorageMethod == StorageMethod.bson)
                    {
                        //return bson
                        SerializeToFile.SerializeToFileBson<T>(ObjectData,FilePath);
                    }
                    else
                    {
                        //return json
                        SerializeToFile.SerializeToFileJson<T>(ObjectData,FilePath);
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// Deletes an object from the database
        /// </summary>
        /// <param name="ObjectId">Object ID</param>
        /// <param name="DatabaseId">Database ID</param>
        /// <param name="CollectionId">Collection ID</param>
        /// <returns>Returns true if file has been deleted</returns>
        public bool DeleteObject(string ObjectId, string DatabaseId, string CollectionId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            bool FileDeleted = false;
            if (Directory.Exists(DirectoryPath))
            {
                //Flag. Parallel loop can't return bool but can break, so set flag to indicate file has been deleted.

                //First part of any object id is the block path
                //This should speed up the process if getting an object
                string BlockNumber = string.Empty;
                int l = ObjectId.IndexOf("_");
                if (l > 0)
                {
                    BlockNumber = ObjectId.Substring(0, l);
                }

                //Possible file path
                string FilePath = DirectoryPath + "\\" + BlockNumber + "\\" + ObjectId + "." + ChosenStorageMethod;

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

                    //Delete object from index file
                    if (EnableIndexing)
                    {
                        Indexing.DeleteObjectIndexRecord(DbPath, ObjectId, DatabaseId, CollectionId);
                    }

                    //Check how many files left in directory. if 0, delete directory.
                    int FileCount = Directory.EnumerateFiles(DirectoryPath + "\\" + BlockNumber).Count();
                    if (FileCount == 0)
                    {
                        Directory.Delete(DirectoryPath + "\\" + BlockNumber);
                    }
                }
            }
            return FileDeleted;
        }



        public IList<string> FindObjects(string query, string DatabaseId, string CollectionId, int skip, int take)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            string IndexPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId + "\\index.txt";

            List<string> searchwords = query.Split(' ').ToList();

            List<string> ObjectIdsFound = new List<string>();

            string line;

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
                
            return ObjectIdsFound;
        }

        /// <summary>
        /// List all object ids in a collection
        /// </summary>
        /// <param name="DatabaseId"></param>
        /// <param name="CollectionId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public IEnumerable<string> ListObjects(string DatabaseId, string CollectionId, int skip, int take)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Skip(skip).Take(take).Select(x => Path.GetFileNameWithoutExtension(x));
            }
            return null;
        }

        public int CollectionObjectsCount(string DatabaseId, string CollectionId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Count();
            }
            return 0;
        }

        public int DatabaseCollectionsCount(string DatabaseId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId;
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
        /// <param name="DatabaseId"></param>
        /// <param name="CollectionId"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public IList<T> GetObjects<T>(string DatabaseId, string CollectionId, int skip, int take, string Password = "")
        {
            IEnumerable<string> ObjectIds = ListObjects(DatabaseId, CollectionId, skip, take);
            IList<T> Objects = new List<T>();
            Parallel.ForEach(ObjectIds, ObjectId =>
            {
                var Object = GetObject<T>(ObjectId, DatabaseId, CollectionId, Password);
                Objects.Add(Object);
            });
            return Objects;
        }

        /// <summary>
        /// Get an object from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ObjectId">Object ID</param>
        /// <param name="DatabaseId">Database ID</param>
        /// <param name="CollectionId">Collection ID</param>
        /// <param name="Password">Optional password parametert to enable encryption</param>
        /// <returns>Object stored in database</returns>
        public T GetObject<T>(string ObjectId, string DatabaseId, string CollectionId, string Password = "")
        {
            // deserialize product from BSON
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                //First part of any object id is the block path
                //This should speed up the process if getting an object
                string BlockNumber = string.Empty;
                int l = ObjectId.IndexOf("_");
                if (l > 0)
                {
                    BlockNumber = ObjectId.Substring(0, l);
                }

                //Possible file path
                string FilePath = DirectoryPath + "\\" + BlockNumber + "\\" + ObjectId + "." + ChosenStorageMethod;

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
                
            }
            return default(T);
        }



        /// <summary>
        /// Returns a list of Collection names found in a database
        /// </summary>
        /// <param name="DatabaseId"></param>
        /// <returns>An IList of string</returns>
        public IList<string> GetCollections(string DatabaseId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId;
            return Directory.GetDirectories(DirectoryPath).ToList();
        }

        /// <summary>
        /// Delete a database collection. Warning! Will delete all sub folders used to split up the collection into smaller blocks.
        /// </summary>
        /// <param name="DatabaseId">Database Id</param>
        /// <param name="CollectionId">Collection Id</param>
        /// <returns>Returns true if the collection was deleted</returns>
        public bool DeleteCollection(string DatabaseId, string CollectionId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            Directory.Delete(DirectoryPath, true);
            if(Directory.Exists(DirectoryPath))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Delete a database. Warning! Will remove the database folder including all sub folders and files. 
        /// </summary>
        /// <param name="DatabaseId"></param>
        /// <returns>Returns true if the datbase was deleted</returns>
        public bool DeleteDatabase(string DatabaseId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId;
            Directory.Delete(DirectoryPath, true);
            if (Directory.Exists(DirectoryPath))
            {
                return false;
            }
            return true;
        }
    }
}
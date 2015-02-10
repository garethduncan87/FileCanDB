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
        private StorageMethod ChosenStorageMethod;

        public FileCanDB(string DatabaseStorePath, StorageMethod StorageMethodValue)
        {
            this.DbPath = DatabaseStorePath;
            this.ChosenStorageMethod = StorageMethodValue;
        }

        /// <summary>
        /// Inserts object into collection. If password is provided the enum StorageMethod value will be changed to "encrypted".
        /// </summary>
        /// <typeparam name="T">Type of object to store in the database</typeparam>
        /// <param name="ObjectData">The object to store in the database</param>
        /// <returns>Returns an ID of the newly inserted object into the database</returns>
        public string InsertObject<T>(T ObjectData, string DatabaseId, string CollectionId, string Password = "", List<string> KeyWords = null)
        {
            //Check if password was provided.
            if (!string.IsNullOrEmpty(Password))
            {
                ChosenStorageMethod = StorageMethod.encrypted;
            }

            //Create file name. Use datetime tick so easier to sort files by time created, then append guid to prevent any duplicates
            string FileName = DateTime.Now.Ticks.ToString() + "-" + Guid.NewGuid().ToString("N");
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
            var LatestBlockNumber = Directory.EnumerateDirectories(DirectoryPath).Select(m => m.Split('\\').Last()).Select(int.Parse).Max();

            //Check count in this directory - if less than 10 thousand then create new block
            DirectoryBlockPath = DirectoryPath + "\\" + LatestBlockNumber.ToString();
            int NumberOfFilesInBlock = Directory.EnumerateFiles(DirectoryBlockPath).Count();
 
            if (NumberOfFilesInBlock >= 1000)
            {
                LatestBlockNumber = LatestBlockNumber + 1;
                DirectoryBlockPath = DirectoryPath + "\\" + (LatestBlockNumber);
                Directory.CreateDirectory(DirectoryBlockPath);
            }


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
            if (KeyWords != null)
            {
                IndexObject(FileName, DatabaseId, CollectionId, KeyWords);
            }

            return FileName;
        }

        private void IndexObject(string ObjectId, string DatabaseId, string CollectionId, List<string> KeyWords)
        {
            string IndexPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId + "\\index.txt";

            if(!File.Exists(IndexPath))
            {
                //create file
                FileStream fs1 = new FileStream(IndexPath, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(fs1);
                writer.Close();
            }

            string tempFile = Path.GetTempFileName();
            using (var sr = new StreamReader(IndexPath))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;
                bool indexadded = false;
                while ((line = sr.ReadLine()) != null)
                {

                    string keyword = line.Split (' ')[0];
                    if(KeyWords.Contains(keyword))
                    {
                        //line found
                        string objectIds = line.Split(' ')[1];
                        List<string> objectIdsList = objectIds.Split(',').ToList();
                        if (objectIdsList.Contains(ObjectId))
                        {
                            //Write line
                            sw.WriteLine(line);
                        }
                        else
                        {
                            //Add to list
                            objectIdsList.Add(ObjectId);
                            sw.WriteLine(keyword + " " + string.Join(",", new List<string>(objectIdsList).ToArray()));
                        }
                        indexadded = true;
                    }
                    else
                    {
                        sw.WriteLine(line);
                    }
                }


                if(sr.ReadLine() != null || indexadded == false)
                {
                    //file is empty so add to index
                    //for each keyword add record
                    foreach (string keyword in KeyWords)
                    {
                        sw.WriteLine(keyword + " " + ObjectId);
                    }
                    
                }


            }

            File.Delete(IndexPath);
            File.Move(tempFile, IndexPath);
        }

        private void DeleteObjectIndexRecord(string ObjectId, string DatabaseId, string CollectionId)
        {
            string IndexPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId + "\\index.txt";
            string tempFile = Path.GetTempFileName();
            using (var sr = new StreamReader(IndexPath))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains(ObjectId))
                    {
                        //Remove objectid from list and re create the line
                        string keyword = line.Split(' ')[0];
                        string objectIds = line.Split(' ')[1];
                        List<string> objectIdsList = objectIds.Split(',').ToList();
                        objectIdsList.Remove(ObjectId);

                        if (objectIdsList.Count > 0)
                        {
                            sw.WriteLine(keyword + " " + string.Join(",", new List<string>(objectIdsList).ToArray()));
                        } 
                    }
                    else
                    {
                        sw.WriteLine(line);
                    }
                }
            }

            File.Delete(IndexPath);
            File.Move(tempFile, IndexPath);
        }


        public IEnumerable<string> ListObjects(string DatabaseId, string CollectionId, int skip, int take, string Password = "")
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Skip(skip).Take(take);
            }
            return null;
        }

        public long CollectionObjectsCount(string DatabaseId, string CollectionId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                return Directory.GetFiles(DirectoryPath, "*." + ChosenStorageMethod, SearchOption.AllDirectories).Count();
            }
            return 0;
        }

        public long DatabaseCollectionsCount(string DatabaseId)
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
            IEnumerable<string> ObjectIds = ListObjects(DatabaseId, CollectionId, skip, take, Password);
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
            //Check if password was provided.
            if (!string.IsNullOrEmpty(Password))
            {
                ChosenStorageMethod = StorageMethod.encrypted;
            }

            // deserialize product from BSON
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                IEnumerable<string> BlockPaths = Directory.EnumerateDirectories(DirectoryPath);
                foreach (string BlockPath in BlockPaths)
                {
                    //Possible file path as we are not sure what block the file is in
                    string PossibleFilePath = BlockPath + "\\" + ObjectId + "." + ChosenStorageMethod;

                    //If File path exists
                    if (File.Exists(PossibleFilePath))
                    {
                        if (ChosenStorageMethod == StorageMethod.encrypted)
                        {
                            //bson encrypted method
                            return DeserializeFromFile.DeserializeFromFileBsonEncrypted<T>(PossibleFilePath, Password);
                        }
                        else if (ChosenStorageMethod == StorageMethod.bson)
                        {
                            //return bson
                            return DeserializeFromFile.DeserializeFromFileBson<T>(PossibleFilePath);
                        }
                        else
                        {
                            //return json
                            return DeserializeFromFile.DeserializeFromFileJson<T>(PossibleFilePath);
                        }
                    }
                }
            }
            return default(T);
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
                IEnumerable<string> BlockPaths = Directory.EnumerateDirectories(DirectoryPath);
                 //Flag. Parallel loop can't return bool but can break, so set flag to indicate file has been deleted.
                Parallel.ForEach(BlockPaths, (BlockPath, state) =>
                {
                    //Possible file path as we are not sure what block the file is in
                    string PossibleFilePath = BlockPath + "\\" + ObjectId + "." + ChosenStorageMethod;

                    if (File.Exists(PossibleFilePath))
                    {

                        File.Delete(PossibleFilePath);
                        if (!File.Exists(PossibleFilePath))
                        {
                            //Check if encrypted details file exist with it. If so, delete it
                            if (File.Exists(PossibleFilePath + EncryptedDetailsFileExtension))
                            {
                                File.Delete(PossibleFilePath + EncryptedDetailsFileExtension);
                            }
                            FileDeleted = true;
                        }

                        //Delete object from index file
                        DeleteObjectIndexRecord(ObjectId, DatabaseId, CollectionId);

                        //Check how many files left in directory. if 0, delete directory.
                        int FileCount = Directory.EnumerateFiles(BlockPath).Count();
                        if (FileCount == 0)
                        {
                            Directory.Delete(BlockPath);
                        }
                            
                        state.Break();    
                    }
                });
            }
            return FileDeleted;
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
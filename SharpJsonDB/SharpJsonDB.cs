using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpJsonDB
{
    public class SharpJsonDB
    {
        private string DbPath;
        public SharpJsonDB(string DatabaseStorePath)
        {
            this.DbPath = DatabaseStorePath;
        }

        /// <summary>
        /// Inserts object into collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="OnjectData"></param>
        /// <returns>Automated Id of newly inserted object</returns>
        public string InsertObject<T>(T OnjectData, string DatabaseId, string CollectionId)
        {
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

            if (NumberOfFilesInBlock >= 10)
            {
                DirectoryBlockPath = DirectoryPath + "\\" + (LatestBlockNumber + 1);
                Directory.CreateDirectory(DirectoryBlockPath);
            }

            FilePath = DirectoryBlockPath + "\\" + FileName + ".json";

            //Serialise object using Json.net
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(FilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, OnjectData);
                    return FileName;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

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
                    string PossibleFilePath = BlockPath + "\\" + ObjectId + ".json";

                    if (File.Exists(PossibleFilePath))
                    {
                        try
                        {
                            File.Delete(PossibleFilePath);
                            //Check how many files left in directory. if 0, delete directory.
                            int FileCount = Directory.EnumerateFiles(BlockPath).Count();
                            FileDeleted = true;
                            if (FileCount == 0)
                            {
                                Directory.Delete(BlockPath);
                            }
                            
                            state.Break();
                        }
                        catch (Exception ex) {  }
                    }
                });
            }
            return FileDeleted;
        }

        public T SelectObject<T>(string ObjectId, string DatabaseId, string CollectionId)
        {
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            if (Directory.Exists(DirectoryPath))
            {
                IEnumerable<string> BlockPaths = Directory.EnumerateDirectories(DirectoryPath);
                foreach (string BlockPath in BlockPaths)
                {
                    //Possible file path as we are not sure what block the file is in
                    string PossibleFilePath = BlockPath + "\\" + ObjectId + ".json";

                    if (File.Exists(PossibleFilePath))
                    {
                        try
                        {
                            using (StreamReader sr = new StreamReader(PossibleFilePath))
                            {
                                using (JsonReader reader = new JsonTextReader(sr))
                                {
                                    JsonSerializer serializer = new JsonSerializer();
                                    var result = serializer.Deserialize<T>(reader);
                                    return result;
                                }
                            }
                        }
                        catch (Exception ex) { throw ex; }
                    }
                }
            }
            return default(T);
        }

        /// <summary>
        /// Selects all objects (with skip and take taken into consideration) in a collection.
        /// Returns a JObject as it maybe possible that a collection may hold different types of files.
        /// </summary>
        /// <param name="DatabaseId"></param>
        /// <param name="CollectionId"></param>
        /// <param name="Skip"></param>
        /// <param name="Take"></param>
        /// <returns></returns>
        public List<JObject> SelectObjects(string DatabaseId, string CollectionId, out int TotalFiles, int Skip = 0, int Take = 0)
        {
            //how to convert jobject to object - Album album = jalbum.ToObject<Album>();

            List<JObject> Objects = new List<JObject>();
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            TotalFiles = 0;
            if (Directory.Exists(DirectoryPath))
            {
                IEnumerable<string> Files;
                Files = Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories).OrderBy(m => m);
                TotalFiles = Files.Count();
                if (Skip != 0 && Take != 0)
                {
                    Files = Files.Skip(Skip).Take(Take);
                }

                Parallel.ForEach(Files, currentFile =>
                {
                    using (StreamReader sr = new StreamReader(currentFile))
                    {
                        using (JsonReader reader = new JsonTextReader(sr))
                        {
                            JsonSerializer serializer = new JsonSerializer();

                            //Deserialize jsonfile to JObject
                            JObject obj = serializer.Deserialize<JObject>(reader);

                            //Add JObject to list
                            Objects.Add(obj);
                        }
                    }
                }); 
            }
            return Objects;
        }
    }
}
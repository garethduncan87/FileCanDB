using Newtonsoft.Json;
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
            //Max nunber of files in folder = 1000
            //So count number of files in folder, and if 1000 create new folder and start there

            string FileName = DateTime.Now.Ticks.ToString();
            string DirectoryPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId;
            string DirectoryBlockPath;
            string FilePath;
            //Does Directory exist? If not create it

            
            //Does directory contain child directories (blocks)?
            //If so, findest highest number named directory 
            //and insert in there if the total of files in that directory is less than 1000
                
            IList<string> BlockNames = Directory.EnumerateDirectories(DirectoryPath).ToList();
            int DirectoryBlockCount = BlockNames.Count();
            if (DirectoryBlockCount == 0)
            {
                DirectoryBlockPath = DirectoryPath + "\\1";
                Directory.CreateDirectory(DirectoryBlockPath);
            }
            

            //Find highest value block name in directory
            //listofIDs.Select(int.Parse).ToList()
            IList<int> BlockNumbers = BlockNames.Select(int.Parse).ToList();
            int LatestBlock = BlockNumbers.Max();
            DirectoryBlockPath = DirectoryPath + "\\" + LatestBlock.ToString();
            //Check count in this directory
            int NumberOfFilesInBlock = Directory.EnumerateFiles(DirectoryBlockPath).Count();

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
            return true;
        }

        public T SelectObject<T>(string ObjectId, string DatabaseId, string CollectionId)
        {

            return default(T);
        }

        public List<T> SelectObjects<T>( string DatabaseId, string CollectionId, int Skip, int Take)
        {
            List<T> Results = null;
            return Results;
        }
        

    }
}

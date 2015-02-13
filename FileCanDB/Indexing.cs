using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Duncan.FileCanDB
{
    public static class Indexing
    {
        public static void IndexObject(string DbPath, string ObjectId, string DatabaseId, string CollectionId, List<string> KeyWords)
        {
            string IndexPath = DbPath + "\\" + DatabaseId + "\\" + CollectionId + "\\index.txt";

            if (!File.Exists(IndexPath))
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

                    string keyword = line.Split(' ')[0];
                    if (KeyWords.Contains(keyword))
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


                if (sr.ReadLine() != null || indexadded == false)
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

        public static void DeleteObjectIndexRecord(string DbPath, string ObjectId, string DatabaseId, string CollectionId)
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

                        //If there are objects in the list, then right the line to the index file
                        if (objectIdsList.Count > 0)
                        {
                            sw.WriteLine(keyword + " " + string.Join(",", new List<string>(objectIdsList).ToArray()));
                        }
                    }
                    else
                    {
                        //If the line does not contain the object id, then write the line back to the index file
                        sw.WriteLine(line);
                    }
                }
            }

            //Delete the original index file
            File.Delete(IndexPath);

            //recreate the new index file
            if (File.ReadLines(IndexPath).Count() > 0)
            {
                File.Move(tempFile, IndexPath);
            }
        }
    }
}

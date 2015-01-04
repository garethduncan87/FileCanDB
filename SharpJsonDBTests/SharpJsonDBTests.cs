using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace SharpJsonDBTests
{
    [TestClass]
    public class SharpJsonDBTests
    {
        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");

            SharpJsonDB.SharpJsonDB MySharpJsonDB = new SharpJsonDB.SharpJsonDB(@"c:\sharpjsondb");

            string EntryId = MySharpJsonDB.InsertObject(MyTestObject, "Blog", "GarethsBlog");
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }
        }

        [TestMethod]
        public void SelectObjectsTest()
        {
            SharpJsonDB.SharpJsonDB MySharpJsonDB = new SharpJsonDB.SharpJsonDB(@"c:\sharpjsondb");
            List<string> CreatedFileIds = new List<string>();
            int n = 100;
            Parallel.For(0, n, i =>
            {
                TestObject MyTestObject = new TestObject(i.ToString(), DateTime.Now, i.ToString(), i.ToString());
                string EntryId = MySharpJsonDB.InsertObject(MyTestObject, "Blog", "GarethsBlog");
                CreatedFileIds.Add(EntryId);
                if (string.IsNullOrEmpty(EntryId))
                {
                    Assert.Fail("No ID returned for newly inserted object");
                }
            });
                
            int TotalFilesFound;
            List<JObject> result = MySharpJsonDB.SelectObjects("Blog", "GarethsBlog", out TotalFilesFound, 3, 4); //Should return 4,5,6,7 from the 10 files created.
            if (result.Count == 0)
            {
                Assert.Fail("No files found");
            }

            Parallel.ForEach(CreatedFileIds, fileid =>
            {
                if (!MySharpJsonDB.DeleteObject(fileid, "Blog", "GarethsBlog"))
                {
                    Assert.Fail("Failed to delete file");
                }
            });

        }


        [TestMethod]
        public void SelectObjectTest()
        {
            //Craete file first
            SharpJsonDB.SharpJsonDB MySharpJsonDB = new SharpJsonDB.SharpJsonDB(@"c:\sharpjsondb");
            TestObject MyTestObject = new TestObject("Select option test", DateTime.Now, "select option test " + Guid.NewGuid().ToString("N"), "select option test" );
            string EntryId = MySharpJsonDB.InsertObject(MyTestObject, "Blog", "GarethsBlog");
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            //Select file
            TestObject ReturnedObject;
            ReturnedObject = MySharpJsonDB.SelectObject<TestObject>(EntryId, "Blog", "GarethsBlog");
            if (ReturnedObject == null)
            {
                Assert.Fail("No file returned");
            }

            //Delete file as its only a test
            if (!MySharpJsonDB.DeleteObject(EntryId, "Blog", "GarethsBlog"))
            {
                Assert.Fail("Failed to delete file");
            }
        }

        [TestMethod]
        public void DeleteObjectTest()
        {
            SharpJsonDB.SharpJsonDB MySharpJsonDB = new SharpJsonDB.SharpJsonDB(@"c:\sharpjsondb");
            //Create file first
            TestObject MyTestObject = new TestObject("delete option test", DateTime.Now, "delete option test " + Guid.NewGuid().ToString("N"), "delete option test");
            string EntryId = MySharpJsonDB.InsertObject(MyTestObject, "Blog", "GarethsBlog");

            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            if (!MySharpJsonDB.DeleteObject(EntryId, "Blog", "GarethsBlog"))
            {
                Assert.Fail("Failed to delete file");
            }
        }
    }
}

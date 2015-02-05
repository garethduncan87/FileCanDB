using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Duncan.FileCanDB;
using System.Collections.Generic;


namespace Duncan.FileCanDB.Tests
{
    [TestClass]
    public class FileCanDBTests
    {
        public StorageMethod ChosenStorage;
        public string Password;
        public FileCanDBTests()
        {
            this.ChosenStorage = StorageMethod.encrypted;

            Password = string.Empty;
            if (ChosenStorage == StorageMethod.encrypted)
            {
                Password = "12345678";
            }
        }

        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");
            IFileCanDB MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage);
            string EntryId = MySharpFileDB.InsertObject(MyTestObject, "Blog", "GarethsBlog", Password);
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }
        }

        [TestMethod]
        public void SelectObjectsTest()
        {
            IFileCanDB MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage);
            //Insert 10 files
            List<string> EntryIds = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                
                TestObject MyTestObject = new TestObject("Select option test", DateTime.Now, "select option test " + Guid.NewGuid().ToString("N"), "select option test");
                string EntryId = MySharpFileDB.InsertObject(MyTestObject, "Blog", "GarethsBlog", Password);
                EntryIds.Add(EntryId);
                if (string.IsNullOrEmpty(EntryId))
                {
                    Assert.Fail("No ID returned for newly inserted object");
                }
            }

            var results = MySharpFileDB.GetObjects<TestObject>("Blog", "GarethsBlog", 0, 100, Password);

            if (results.Count >= EntryIds.Count)
            {
                
            }
            else
            {
                Assert.Fail(string.Format("Not enough files returned. There were {0} files created, but only {1} was returned", EntryIds.Count, results.Count));
            }
            //Get all objects
            


            foreach (string EntryId in EntryIds)
            {
                //Delete file as its only a test
                if (!MySharpFileDB.DeleteObject(EntryId, "Blog", "GarethsBlog"))
                {
                    Assert.Fail("Failed to delete file");
                }
            }

        }

        [TestMethod]
        public void SelectObjectTest()
        {

            //Craete file first
            IFileCanDB MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage);
            TestObject MyTestObject = new TestObject("Select option test", DateTime.Now, "select option test " + Guid.NewGuid().ToString("N"), "select option test" );
            string EntryId = MySharpFileDB.InsertObject(MyTestObject, "Blog", "GarethsBlog", Password);
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            //Select file
            TestObject ReturnedObject;
            ReturnedObject = MySharpFileDB.GetObject<TestObject>(EntryId, "Blog", "GarethsBlog", Password);
            if (ReturnedObject == null)
            {
                Assert.Fail("No file returned");
            }

            //Delete file as its only a test
            if (!MySharpFileDB.DeleteObject(EntryId, "Blog", "GarethsBlog"))
            {
                Assert.Fail("Failed to delete file");
            }
        }

        [TestMethod]
        public void DeleteObjectTest()
        {
            IFileCanDB MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage);
            //Create file first
            TestObject MyTestObject = new TestObject("delete option test", DateTime.Now, "delete option test " + Guid.NewGuid().ToString("N"), "delete option test");
            string EntryId = MySharpFileDB.InsertObject(MyTestObject, "Blog", "GarethsBlog");

            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            if (!MySharpFileDB.DeleteObject(EntryId, "Blog", "GarethsBlog"))
            {
                Assert.Fail("Failed to delete file");
            }
        }
    }
}

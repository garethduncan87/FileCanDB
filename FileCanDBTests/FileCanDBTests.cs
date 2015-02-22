using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Duncan.FileCanDB;
using System.Collections.Generic;
using Duncan.FileCanDB.Models;


namespace Duncan.FileCanDB.Tests
{
    [TestClass]
    public class FileCanDBTests
    {
        public StorageMethod ChosenStorage;
        public string Password;
        bool EnableIndexing;
        FileCanDB MySharpFileDB;
        public FileCanDBTests()
        {
            this.ChosenStorage = StorageMethod.encrypted;
            this.EnableIndexing = true;
            Password = string.Empty;
            if (ChosenStorage == StorageMethod.encrypted)
            {
                Password = "12345678";
            }

            MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage, EnableIndexing);
        }


        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");
            
            string EntryId = MySharpFileDB.InsertPacket(MyTestObject, "Blog", "GarethsBlog", Password);
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }
        }

        [TestMethod]
        public void NamePacketTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");
            string EntryId = MySharpFileDB.InsertPacket(MyTestObject, "Blog", "GarethsBlog", Password);
            MySharpFileDB.NamePacket(EntryId, "GarethsBlog", "Blog", "TestPacketId");

            PacketModel<TestObject> result = MySharpFileDB.GetPacketByName<TestObject>("TestPacketId", "Blog", "GarethsBlog", Password);
            if(result.Data == null)
            {
                Assert.Fail("Failed to name packet");
            }
        }


        [TestMethod]
        public void SelectObjectTest()
        {

            //Craete file first
            FileCanDB MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage, EnableIndexing);
            TestObject MyTestObject = new TestObject("Select option test", DateTime.Now, "select option test " + Guid.NewGuid().ToString("N"), "select option test" );
            List<string> Keywords = new List<string>();
            Keywords.Add("Programming");
            Keywords.Add("Coding");
            Keywords.Add("Generics");
            string PacketId = MySharpFileDB.InsertPacket(MyTestObject, "Blog", "GarethsBlog", "", Password, Keywords);
            if (string.IsNullOrEmpty(PacketId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            //Select file
            PacketModel<TestObject> ReturnedObject = MySharpFileDB.GetPacket<TestObject>(PacketId, "Blog", "GarethsBlog", Password);
            if (ReturnedObject == null)
            {
                Assert.Fail("No file returned");
            }

            //Delete file as its only a test
            if (!MySharpFileDB.DeletePacket(PacketId, "Blog", "GarethsBlog"))
            {
                Assert.Fail("Failed to delete file");
            }
        }

        [TestMethod]
        public void DeleteObjectTest()
        {
            FileCanDB MySharpFileDB = new FileCanDB(@"c:\SharpFileDB", ChosenStorage, EnableIndexing);
            //Create file first
            TestObject MyTestObject = new TestObject("delete option test", DateTime.Now, "delete option test " + Guid.NewGuid().ToString("N"), "delete option test");
            var EntryId = MySharpFileDB.InsertPacket(MyTestObject, "Blog", "GarethsBlog");

            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            if (!MySharpFileDB.DeletePacket(EntryId, "Blog", "GarethsBlog"))
            {
                Assert.Fail("Failed to delete file");
            }
        }
    }
}

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
        private string _area = "Blog";
        private string _collection = "GarethsBlog";
        private string _databaseLocation = @"c:\FileCanDB";
        private StorageType _chosenStorage;
        private string _password;
        private bool _enableIndexing;
        private FileCanDB<TestObject> _myFileCanDb;

        public FileCanDBTests()
        {
            this._chosenStorage = StorageType.encrypted;
            this._enableIndexing = true;
            _password = string.Empty;
            if (_chosenStorage == StorageType.encrypted)
            {
                _password = "12345678";
            }

            _myFileCanDb = new FileCanDB<TestObject>(_databaseLocation, _area, _collection, _chosenStorage, _enableIndexing);
        }

        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");
            
            string EntryId = _myFileCanDb.InsertPacket(MyTestObject, _password);
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }
        }

        [TestMethod]
        public void NamePacketTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");
            string EntryId;
            if (_chosenStorage == StorageType.encrypted)
            {
                EntryId = _myFileCanDb.InsertPacket(MyTestObject, _password);
            }
            else
            {
                EntryId = _myFileCanDb.InsertPacket(MyTestObject);
            }

            //Provide the packet with a name
            _myFileCanDb.NamePacket(EntryId, "TestPacketId");

            PacketModel<TestObject> result = _myFileCanDb.GetPacketByName("TestPacketId", _password);
            if(result.Data == null)
            {
                Assert.Fail("Failed to name packet");
            }
        }


        [TestMethod]
        public void SelectObjectTest()
        {

            //Craete file first
            TestObject MyTestObject = new TestObject("Select option test", DateTime.Now, "select option test " + Guid.NewGuid().ToString("N"), "select option test" );
            List<string> KeyWords = new List<string>();
            KeyWords.Add("coding");
            KeyWords.Add("generics");
            string PacketId;
            if (_chosenStorage == StorageType.encrypted)
            {
                PacketId = _myFileCanDb.InsertPacket(MyTestObject, _password);
                
            }
            else
            {
                PacketId = _myFileCanDb.InsertPacket(MyTestObject);
            }

            // index file
            _myFileCanDb.IndexPacket(PacketId, KeyWords);

            if (string.IsNullOrEmpty(PacketId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            //Select file
            PacketModel<TestObject> ReturnedObject;
            if (_chosenStorage == StorageType.encrypted)
            {
                ReturnedObject = _myFileCanDb.GetPacket(PacketId, _password);
            }
            else
            {
                 ReturnedObject = _myFileCanDb.GetPacket(PacketId);
            }

            //Find by index
            IList<string> results = _myFileCanDb.FindPacketsUsingIndex("coding");

            

            if (ReturnedObject == null)
            {
                Assert.Fail("No file returned");
            }

            //Delete file as its only a test
            if (!_myFileCanDb.DeletePacket(PacketId))
            {
                Assert.Fail("Failed to delete file");
            }
        }

        [TestMethod]
        public void DeleteObjectTest()
        {
            //Create file first
            TestObject MyTestObject = new TestObject("delete option test", DateTime.Now, "delete option test " + Guid.NewGuid().ToString("N"), "delete option test");
            string EntryId;
            if (_chosenStorage == StorageType.encrypted)
            {
                EntryId = _myFileCanDb.InsertPacket(MyTestObject, _password);
            }
            else
            {
                EntryId = _myFileCanDb.InsertPacket(MyTestObject);
            }

            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            if (!_myFileCanDb.DeletePacket(EntryId))
            {
                Assert.Fail("Failed to delete file");
            }
        }
    }
}

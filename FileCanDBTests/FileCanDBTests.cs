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
        private IFileCanDB<TestObject> _myFileCanDb;

        public FileCanDBTests()
        {
            this._chosenStorage = StorageType.encrypted;
            this._enableIndexing = true;
            _password = string.Empty;
            if (_chosenStorage == StorageType.encrypted)
            {
                _password = "12345678";
            }

            _myFileCanDb = new FileCanDB<TestObject>(_databaseLocation, _area, _collection, _chosenStorage, _enableIndexing, _password);
        }

        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");

            string id = _myFileCanDb.generateId();
            if (!_myFileCanDb.InsertPacket(id, MyTestObject))
            {
                Assert.Fail("No ID returned for newly inserted object");
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
            string PacketId = _myFileCanDb.generateId();
            if (_chosenStorage == StorageType.encrypted)
            {
                _myFileCanDb.InsertPacket(PacketId, MyTestObject);
                
            }
            else
            {
                _myFileCanDb.InsertPacket(PacketId, MyTestObject);
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
                ReturnedObject = _myFileCanDb.GetPacket(PacketId);
            }
            else
            {
                 ReturnedObject = _myFileCanDb.GetPacket(PacketId);
            }

            //Find by index
            IEnumerable<string> results = _myFileCanDb.FindPacketsUsingIndex("coding", 0, 1000);

            

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
            string EntryId = _myFileCanDb.generateId();
            if (_chosenStorage == StorageType.encrypted)
            {
                 if(!_myFileCanDb.InsertPacket(EntryId, MyTestObject))
                 {
                     Assert.Fail("Failed to insert object");
                 }
            }
            else
            {
                _myFileCanDb.InsertPacket(EntryId, MyTestObject);
            }

            if (!_myFileCanDb.DeletePacket(EntryId))
            {
                Assert.Fail("Failed to delete file");
            }
        }
    }
}

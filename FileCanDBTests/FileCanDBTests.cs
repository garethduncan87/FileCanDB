using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using FileCanDB.Models;


namespace FileCanDB.Tests
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
            this._chosenStorage = StorageType.json;
            this._enableIndexing = true;
            _password = string.Empty;
            if (_chosenStorage == StorageType.encrypted)
                _password = "12345678";

            _myFileCanDb = new FileCanDB<TestObject>(_databaseLocation, _area, _collection, _enableIndexing, _chosenStorage);
        }

        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject("insert option test", DateTime.Now, "insert option test " + Guid.NewGuid().ToString("N"), "insert option test");

            string id = _myFileCanDb.CreateId();
            if (!_myFileCanDb.Insert(id, MyTestObject))
                Assert.Fail("No ID returned for newly inserted object");
        }

        [TestMethod]
        public void SelectObjectTest()
        {

            //Craete file first
            TestObject MyTestObject = new TestObject("Select option test", DateTime.Now, "select option test " + Guid.NewGuid().ToString("N"), "select option test" );
            List<string> KeyWords = new List<string>();
            KeyWords.Add("coding");
            KeyWords.Add("generics");
            string PacketId = _myFileCanDb.CreateId();
            if (_chosenStorage == StorageType.encrypted)
                _myFileCanDb.Insert(PacketId, MyTestObject);
            else
                _myFileCanDb.Insert(PacketId, MyTestObject);

            // index file
            _myFileCanDb.AddIndexEntry(PacketId, KeyWords);

            if (string.IsNullOrEmpty(PacketId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }

            //Select file
            PacketModel<TestObject> ReturnedObject;
            if (_chosenStorage == StorageType.encrypted)
                ReturnedObject = _myFileCanDb.Read(PacketId);
            else
                 ReturnedObject = _myFileCanDb.Read(PacketId);

            //Find by index
            IEnumerable<string> results = _myFileCanDb.Find("coding", 0, 1000);

            if (ReturnedObject == null)
                Assert.Fail("No file returned");

            //Delete file as its only a test
            if (!_myFileCanDb.Delete(PacketId))
                Assert.Fail("Failed to delete file");
        }

        [TestMethod]
        public void DeleteObjectTest()
        {
            //Create file first
            TestObject MyTestObject = new TestObject("delete option test", DateTime.Now, "delete option test " + Guid.NewGuid().ToString("N"), "delete option test");
            string EntryId = _myFileCanDb.CreateId();
            if (_chosenStorage == StorageType.encrypted)
            {
                 if(!_myFileCanDb.Insert(EntryId, MyTestObject))
                     Assert.Fail("Failed to insert object");
            }
            else
                _myFileCanDb.Insert(EntryId, MyTestObject);

            if (!_myFileCanDb.Delete(EntryId))
                Assert.Fail("Failed to delete file");
        }
    }
}

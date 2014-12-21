using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpJsonDBTests
{
    [TestClass]
    public class SharpJsonDBTests
    {
        [TestMethod]
        public void InsertObjectTest()
        {
            TestObject MyTestObject = new TestObject();

            SharpJsonDB.SharpJsonDB MySharpJsonDB = new SharpJsonDB.SharpJsonDB(@"c:\sharpjsondb");

            string EntryId = MySharpJsonDB.InsertObject(MyTestObject, "Blog", "GarethsBlog");
            if (string.IsNullOrEmpty(EntryId))
            {
                Assert.Fail("No ID returned for newly inserted object");
            }
        }
    }
}

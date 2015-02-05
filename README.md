FileCanDB
===========
A flat file system allowing you to quickly store any object as a file in a database.

Object > Collection > Database
Database can hold many collections. A collection can hold many objects.
NB A collection splits objects into sub directories with a max count of 1000 files in each.

Insert an object
----------------
IFileCanDB MyFileCanDB = new FileCanDB(@"c:\MyDatabaseDirectory", StorageMethod.encrypted);
BlogEntry MyBlogEntry = GetBlogEntry(BlogEntryId);
string EntryId = MySharpFileDB.InsertObject(MyBlogEntry, "BlogDatabase", "GarethsBlogCollection", "Password12345");

Get objects in database
-----------------------
int skip = 0;
int take = 100;
IEnumerable<string> ListObjects("BlogDatabase", "GarethsBlogCollection", skip, take, "Password12345");

Get an object
-------------
BlogEntry MyBlogEntry = GetObject<BlogEntry>(EntryId, "BlogDatabase", "GarethsBlogCollection", "Password12345");

Delete an object
----------------
bool deleted = false;
if(DeleteObject(EntryId, "BlogDatabase", "GarethsBlogCollection"))
{
  deleted = true;
}

Other current available methods
-------------------------------
GetCollections
DeleteCollection
DeleteDatabase
GetObjects (Returns multiple objects in memory)

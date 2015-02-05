FileCanDB
===========
A flat file system allowing you to quickly store any object as a file in a database.<br />
<br />
Object > Collection > Database<br />
Database can hold many collections. A collection can hold many objects.<br />
NB A collection splits objects into sub directories with a max count of 1000 files in each.<br />
<br />
Insert an object
----------------
IFileCanDB MyFileCanDB = new FileCanDB(@"c:\MyDatabaseDirectory", StorageMethod.encrypted);<br />
BlogEntry MyBlogEntry = GetBlogEntry(BlogEntryId);<br />
string EntryId = MySharpFileDB.InsertObject(MyBlogEntry, "BlogDatabase", "GarethsBlogCollection", "Password12345");<br />
<br />
Get objects in database
-----------------------
int skip = 0;<br />
int take = 100;<br />
IEnumerable<string> ListObjects("BlogDatabase", "GarethsBlogCollection", skip, take, "Password12345");<br />
<br />
Get an object
-------------
BlogEntry MyBlogEntry = GetObject<BlogEntry>(EntryId, "BlogDatabase", "GarethsBlogCollection", "Password12345");<br />
<br />
Delete an object
----------------
bool deleted = false;<br />
if(DeleteObject(EntryId, "BlogDatabase", "GarethsBlogCollection"))<br />
{<br />
  deleted = true;<br />
}<br />
<br />
Other current available methods
-------------------------------
GetCollections<br />
DeleteCollection<br />
DeleteDatabase<br />
GetObjects (Returns multiple objects in memory)<br />

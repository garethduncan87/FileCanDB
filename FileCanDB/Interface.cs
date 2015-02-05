using System.Collections.Generic;
namespace Duncan.FileCanDB
{
    public interface IFileCanDB
    {
        /// <summary>
        /// Inserts object into collection. If password is provided the enum StorageMethod value will be changed to "encrypted".
        /// </summary>
        /// <typeparam name="T">Type of object to store in the database</typeparam>
        /// <param name="ObjectData">The object to store in the database</param>
        /// <returns>Returns an ID of the newly inserted object into the database</returns>
        string InsertObject<T>(T ObjectData, string DatabaseId, string CollectionId, string Password = "");

        /// <summary>
        /// Deletes an object from the database
        /// </summary>
        /// <param name="ObjectId">Object ID</param>
        /// <param name="DatabaseId">Database ID</param>
        /// <param name="CollectionId">Collection ID</param>
        /// <returns>Returns true if file has been deleted</returns>
        bool DeleteObject(string ObjectId, string DatabaseId, string CollectionId);

        /// <summary>
        /// Get an object from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ObjectId">Object ID</param>
        /// <param name="DatabaseId">Database ID</param>
        /// <param name="CollectionId">Collection ID</param>
        /// <param name="Password">Optional password parametert to enable encryption</param>
        /// <returns>Object stored in database</returns>
        T GetObject<T>(string ObjectId, string DatabaseId, string CollectionId, string Password = "");

        /// <summary>
        /// Retrieves all objects in a collection in a database
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="DatabaseId">Database Id</param>
        /// <param name="CollectionId">Collection Id</param>
        /// <param name="Password">Optional password parametert to enable encryption</param>
        /// <returns></returns>
        IList<T> GetObjects<T>(string DatabaseId, string CollectionId, int skip, int take, string Password = "");

        bool DeleteDatabase(string DatabaseId);
        bool DeleteCollection(string DatabaseId, string CollectionId);
        IList<string> GetCollections(string DatabaseId);
        long DatabaseCollectionsCount(string DatabaseId);
        long CollectionObjectsCount(string DatabaseId, string CollectionId);
        IEnumerable<string> ListObjects(string DatabaseId, string CollectionId, int skip, int take, string Password = "");
    }
}

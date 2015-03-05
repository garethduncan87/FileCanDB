using Duncan.FileCanDB.Models;
using System.Collections.Generic;
namespace Duncan.FileCanDB
{
    public interface IFileCanDB<T>
    {


        /// <summary>
        /// Check that a Packet exists.
        /// </summary>
        /// <param name="PacketId">Packet Id</param>
        /// <returns>bool: Returns true if packet exists</returns>
        bool CheckPacketExits(string PacketId);



        /// <summary>
        /// Inserts Packet in an encrypted form into collection.
        /// </summary>
        /// <typeparam name="T">Type of Packet to store in the database</typeparam>
        /// <param name="PacketData">The Packet to store in the database</param>
        /// <returns>string: Returns automaticlally generated Id of inputed packet</returns>
        bool InsertPacket(T PacketData, string Password);
        bool InsertPacket(T PacketData, string Id, string Password);
        /// <summary>
        /// Update a packet
        /// </summary>
        /// <typeparam name="T">Packet data type</typeparam>
        /// <param name="PacketId">Packet Id</param>
        /// <param name="PacketData">Packet Data</param>
        /// <param name="KeyWords">Packet keywords</param>
        /// <returns></returns>
        bool UpdatePacket(string PacketId, T PacketData);

        /// <summary>
        /// Update an encrypted packet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId"></param>
        /// <param name="PacketData"></param>
        /// <param name="Password"></param>
        /// <param name="KeyWords"></param>
        /// <returns></returns>
        bool UpdatePacket(string PacketId, T PacketData, string Password);

        /// <summary>
        /// Deletes an Packet from the database
        /// </summary>
        /// <param name="PacketId">Packet ID</param>
        /// <param name="Area">Database ID</param>
        /// <param name="Collection">Collection ID</param>
        /// <returns>Returns true if file has been deleted</returns>
        bool DeletePacket(string PacketId);

        string generateId();
        IEnumerable<string> FindPacketsUsingIndex(string query, int skip, int take);

        /// <summary>
        /// List all packet ids in a collection
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> ListPackets();

        /// <summary>
        /// List all packet ids in a collection
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> ListPackets(int skip);

        /// <summary>
        /// List all packet ids in a collection
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> ListPackets(int skip, int take);

        int CollectionPacketCount();

        int AreaCollectionsCount();

        /// <summary>
        /// Gets all packets in a databases collection. Optionl password parameter. Parallel method.
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Password"></param>
        /// <returns></returns>
        IList<PacketModel<T>> GetPackets(int skip, int take);

        /// <summary>
        /// Gets all encrypted packets in a databases collection. Optionl password parameter. Parallel method.
        /// If password provided, only files with the same password will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Password"></param>
        /// <returns></returns>
        IList<PacketModel<T>> GetPackets(int skip, int take, string Password);

        /// <summary>
        /// Get packet by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketName"></param>
        /// <param name="Area"></param>
        /// <param name="Collection"></param>
        /// <returns></returns>
        PacketModel<T> GetPacketByName(string PacketName);

        /// <summary>
        /// Get encrypted packet by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketName"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        PacketModel<T> GetPacketByName(string PacketName, string Password);

        /// <summary>
        /// Get Id of Packet by its name
        /// </summary>
        /// <param name="PacketName"></param>
        /// <returns></returns>
        string GetPacketId(string PacketName);

        /// <summary>
        /// Get an packet from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId">Packet ID</param>
        /// <returns>Packet stored in database</returns>
        PacketModel<T> GetPacket(string PacketId);

        /// <summary>
        /// Get an packet from the database. If the Password is provided, the enum StorageMethod value will changed to "encrypted"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PacketId">Packet ID</param>
        /// <param name="Password">Optional password parametert to enable encryption</param>
        /// <returns>Packet stored in database</returns>
        PacketModel<T> GetPacket(string PacketId, string Password);

        /// <summary>
        /// Returns a list of Collection names found in a database
        /// </summary>
        /// <param name="Area"></param>
        /// <returns>An IList of string</returns>
        IList<string> GetCollections();

        /// <summary>
        /// Delete a database collection. Warning! Will delete all sub files and folders
        /// </summary>
        /// <param name="Area">Database Id</param>
        /// <param name="Collection">Collection Id</param>
        /// <returns>Returns true if the collection was deleted</returns>
        bool DeleteCollection();

        /// <summary>
        /// Delete a database. Warning! Will remove the database folder including all sub folders and files. 
        /// </summary>
        /// <param name="Area"></param>
        /// <returns>Returns true if the datbase was deleted</returns>
        bool DeleteDatabase();

        void IndexPacket(string PacketId, List<string> KeyWords);

        void DeletePacketIndex(string PacketId);
    }
}

using Duncan.FileCanDB.Models;
using System.Collections.Generic;
namespace Duncan.FileCanDB
{
    public interface IFileCanDB<T>
    {

        bool CheckPacketExits(string PacketId);
        bool InsertPacket(string Id, T PacketData);
        bool UpdatePacket(string PacketId, T PacketData);
        bool DeletePacket(string PacketId);
        string generateId();
        IEnumerable<string> FindPacketsUsingIndex(string query, int skip, int take);
        IEnumerable<string> ListPackets();
        IEnumerable<string> ListPackets(int skip);
        IEnumerable<string> ListPackets(int skip, int take);
        int CollectionPacketCount();
        int AreaCollectionsCount();
        IList<PacketModel<T>> GetPackets(int skip, int take);
        PacketModel<T> GetPacket(string PacketId);
        IList<string> GetCollections();
        bool DeleteCollection();
        bool DeleteDatabase();
        void IndexPacket(string PacketId, List<string> KeyWords);
        void DeletePacketIndex(string PacketId);
    }
}

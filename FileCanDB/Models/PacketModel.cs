using System;

namespace FileCanDB.Models
{
    public class  PacketModel<T>
    {
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public T Data { get; set; }
    }
}

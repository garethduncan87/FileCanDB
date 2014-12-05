using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpJsonDB.Models
{
    public class Collection
    {
        public string Id { get; set; }
        public string DatabaseId { get; set; } //Link to the database
    }
}

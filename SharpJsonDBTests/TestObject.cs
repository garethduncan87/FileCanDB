using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpJsonDBTests
{
    class TestObject
    {
        public TestObject(string Content, DateTime Created, string Description, string Title)
        {
            this.Content = Content;
            this.Created = Created;
            this.Description = Description;
            this.Title = Title;
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }
    }
}

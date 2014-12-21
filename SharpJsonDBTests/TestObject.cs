using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpJsonDBTests
{
    class TestObject
    {
        public TestObject()
        {
            this.Content = "Hello world";
            this.Created = DateTime.Now;
            this.Description = "An article about Hello world";
            this.Title = "This is Hello World";
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }
    }
}

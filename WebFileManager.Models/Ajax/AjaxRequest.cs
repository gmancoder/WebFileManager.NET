using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFileManager.Models.Ajax
{
    public class AjaxRequest
    {
        public string action { get; set; }
        public string method { get; set; }
        public IDictionary<string,object> data { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFileManager.Models
{
    public class FolderItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public bool edit { get; set; }
        public bool view { get; set; }
        public bool download { get; set; }
        public bool extract { get; set; }
        public bool copy { get; set; }
        public bool move { get; set; }
        public string icon { get; set; }
        public string uid { get; set; }
        public string gid { get; set; }
        public string size { get; set; }
        public string mtime { get; set; }
        public string atime { get; set; }
        public string ctime { get; set; }
        public string mime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFileManager.Models
{
    public class Globals
    {
        public string current_folder { get; set; }
        public string root_folder { get; set; }

        public bool tfs { get; set; }

        public User current_user { get; set; }
    }
}

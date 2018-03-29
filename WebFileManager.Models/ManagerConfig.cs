using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFileManager.Models
{
    public class ManagerConfig
    {
        public List<string> EDITABLE_EXTENSIONS { get; set; }
        public List<Shortcut> SHORTCUTS { get; set; }
        public List<User> USERS { get; set; }
    }
}

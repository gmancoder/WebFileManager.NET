using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebFileManager.Models.ViewModels
{
    public class UploadViewModel
    {
        public UploadViewModel()
        {
            Files = new List<HttpPostedFileBase>();
            Overwrite = true;
        }

        public List<HttpPostedFileBase> Files { get; set; }
        public bool Overwrite { get; set; }
        public string DestinationPath { get; set; }
    }
}

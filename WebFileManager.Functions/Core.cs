using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebFileManager.Models;

namespace WebFileManager.Functions
{
    public static class Core
    {
        public static Globals Init()
        {

            Globals g = new Globals();
            if(SessionKeyExists("root_folder"))
            {
                g.root_folder = GetSession("root_folder");
            }
            if(SessionKeyExists("current_folder"))
            {
                g.current_folder = GetSession("current_folder");
            }
            else
            {
                try
                {
                    g.current_folder = g.root_folder;
                    SetSession("current_folder", g.current_folder);
                }
                catch { }
            }

            g.tfs = false;
            if(SessionKeyExists("tfs"))
            {
                g.tfs = Convert.ToBoolean(GetSession("tfs"));
            }

            g.current_user = Config.GetCurrentUser(HttpContext.Current.User.Identity.Name);

            return g;
        }

        public static dynamic PopulateViewBag(object items, string flashStatus = "", string flashMessage = "")
        {
            dynamic viewBag = null;

            viewBag.globals = Init();
            viewBag.items = items;

            if(flashStatus != "")
            {
                List<FlashMessage> flash = new List<FlashMessage>();
                flash.Add(new FlashMessage { Category = flashStatus, Message = flashMessage });
                viewBag.flash = flash;
            }

            return viewBag;
        }

        public static bool SessionKeyExists(string key)
        {
            return HttpContext.Current.Session[key] != null;
        }

        public static string GetSession(string key)
        {
            return HttpContext.Current.Session[key].ToString();
        }

        public static void SetSession(string key, object value)
        {
            HttpContext.Current.Session[key] = value;
        }

        public static void RunProcess(string procName, string args, string workPath)
        {
            ProcessStartInfo processStartInfo;

            Process process;
            processStartInfo = new ProcessStartInfo();

            //Specifies the boot file name

            processStartInfo.FileName =procName;

            //Specifies the command to start the file、parameter

            processStartInfo.Arguments = args;

            //Specify start window mode：hide

            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            //Specify arrival path after compression

            processStartInfo.WorkingDirectory = workPath;

            //Create process object

            process = new Process();

            //Specifies the process object to start the information object

            process.StartInfo = processStartInfo;

            //Boot process

            process.Start();

            //Specify the process of self regression

            process.WaitForExit();
        }
    }
}

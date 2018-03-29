using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebFileManager.Models;

namespace WebFileManager.Functions
{
    public static class Config
    {
        private static ManagerConfig LoadConfig()
        {
            string json = File.ReadAllText(ConfigurationManager.AppSettings["CONFIG"]);
            return JsonConvert.DeserializeObject<ManagerConfig>(json);
        }
        public static List<Shortcut> GetShortcuts()
        {
            ManagerConfig config = LoadConfig();
            return config.SHORTCUTS;
        }

        public static List<string> GetEditableExtensions()
        {
            ManagerConfig config = LoadConfig();
            return config.EDITABLE_EXTENSIONS;
        }

        public static User GetCurrentUser(string username)
        {
            ManagerConfig config = LoadConfig();
            return config.USERS.Where(u => u.username == username).FirstOrDefault();
        }
    }
}

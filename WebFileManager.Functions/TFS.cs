using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebFileManager.Models;
using Microsoft.TeamFoundation.Core;
using System.Configuration;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;
using System.Security.Principal;
using System.IO;

namespace WebFileManager.Functions
{
    public static class TFS
    {
        public static RequestMethodStatus Commit(string path, string message)
        {
            try
            {
                NetworkCredential netCred = new NetworkCredential(ConfigurationManager.AppSettings["TFSUser"], ConfigurationManager.AppSettings["TFSPwd"]);
                Microsoft.VisualStudio.Services.Common.WindowsCredential windowsCred = new Microsoft.VisualStudio.Services.Common.WindowsCredential(netCred);
                VssCredentials tfsCred = new VssCredentials(windowsCred);
                using (TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(ConfigurationManager.AppSettings["TFSUri"]), tfsCred))
                {
                    tpc.Authenticate();
                    var versionControlServer = tpc.GetService<VersionControlServer>();
                    string computerName = Environment.MachineName;
                    WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                    // get yours local workspaces
                    Workspace[] workspaces = versionControlServer.QueryWorkspaces(null, windowsIdentity.Name, computerName);
                    string msg = "";
                    bool update_status = true;
                    List<string> tfs_file = File.ReadAllLines(Folders.AppendEndSlash(path) + "tfs_enabled.txt").ToList();

                    foreach (Workspace workspace in workspaces)
                    {
                        var local_path = workspace.TryGetLocalItemForServerItem(tfs_file.First());
                        if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
                        {
                            if(local_path == path)
                            {
                                DateTime last_commit;
                                if (tfs_file.Count() == 1)
                                {
                                    last_commit = new DateTime(1982, 5, 18);
                                }
                                else
                                {
                                    string l_commit = tfs_file.Last();
                                    if (!DateTime.TryParse(l_commit, out last_commit))
                                    {
                                        return new RequestMethodStatus() { status = false, results = String.Format("{0} not a datetime", l_commit) };
                                    }
                                }
                                Workstation.Current.EnsureUpdateWorkspaceInfoCache(versionControlServer, "Administrator");

                                PendingChange[] changes = GetChanges(workspace, path, last_commit);
                                if (changes.Length > 0)
                                {
                                    workspace.CheckIn(changes, message);
                                }
                                msg = String.Format("{0} additions/changes committed", changes.Length.ToString());
                                tfs_file.Add(DateTime.Now.ToString());
                                File.WriteAllLines(Folders.AppendEndSlash(path) + "tfs_enabled.txt", tfs_file.ToArray());
                            }
                            else
                            {
                                update_status = false;
                                msg = String.Format("Local Path for Server Path {0} does not match current local path ({1}, {2})", tfs_file[0], local_path, path);
                            }
                        }
                        else
                        {
                            update_status = false;
                            msg = String.Format("Unable to find server path {0}", tfs_file[0]);
                        }
                    }
                    if (update_status)
                    {
                        return new RequestMethodStatus() { status = true, results = new Dictionary<string, object>() { { "message", msg }, { "folders", Folders.DrawFolderTree(Core.GetSession("root_folder")) } } };
                    }
                    else
                    {
                        return new RequestMethodStatus() { status = false, results = msg };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        private static PendingChange[] GetChanges(Workspace workspace, string path, DateTime last_commit)
        {
            ProcessDirectoryForChanges(workspace, path, last_commit);
            PendingChange[] pendingChanges = workspace
                .GetPendingChanges()
                .Where(x => x.LocalOrServerFolder.Contains(path))
                .ToArray();
            return pendingChanges;
            
        }

        private static void ProcessDirectoryForChanges(Workspace workspace, string path, DateTime last_commit)
        {
            string[] directories = Directory.GetDirectories(path);
            foreach(string directory in directories)
            {
                ProcessDirectoryForChanges(workspace, directory, last_commit);
            }
            string[] files = Directory.GetFiles(path);
            foreach(string file in files)
            {
                FileInfo finfo = new FileInfo(file);
                if(finfo.CreationTime > last_commit)
                {
                    workspace.PendAdd(file, true);
                }
                else if(finfo.LastWriteTime > last_commit)
                {
                    workspace.PendEdit(file, RecursionType.Full);
                }
            }
        }
    }
}

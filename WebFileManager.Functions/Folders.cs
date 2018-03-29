using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebFileManager.Models;

namespace WebFileManager.Functions
{
    public static class Folders
    {
        public static string AppendEndSlash(string path)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            return path;
        }
        public static string DrawFolderTree(string path)
        {
            path = AppendEndSlash(path);
            try
            {
                var items = Directory.GetDirectories(path);
                if (items.Count() > 0)
                {
                    string children = "<ul>";
                    foreach (string item in items)
                    {
                        string[] item_spl = item.Split('\\');
                        string folder = String.Format("<li><a href='{0}'>{1}</a>", item, item_spl[item_spl.Count() - 1]);
                        folder += DrawFolderTree(item);
                        folder += "</li>";
                        children += folder;
                    }
                    children += "</ul>";

                    return children;
                }
            }
            catch { }
            return "";
        }

        public static RequestMethodStatus GetFolderItems(string path)
        {
            RequestMethodStatus response = new RequestMethodStatus();
            try
            {
                Core.SetSession("current_folder", path);
                Core.SetSession("tfs", TFSEnabled(path));
                List<FolderItem> items = new List<FolderItem>();
                int idx = 0;
                string[] folders = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path);

                foreach (string folder in folders)
                {
                    idx += 1;
                    items.Add(GetItem(folder, idx));
                }
                foreach (string file in files)
                {
                    idx += 1;
                    items.Add(GetItem(file, idx));
                }
                response.status = true;
                response.results = new Dictionary<string, object> { { "items", items }, { "tfs", TFSEnabled(path) } };
            }
            catch (Exception ex)
            {
                response.status = false;
                response.results = ex.Message;
            }
            return response;
        }

        public static FolderItem GetItem(string file, int idx)
        {
            string[] file_spl = file.Split('\\');
            FolderItem item = new FolderItem()
            {
                id = idx,
                name = file_spl[file_spl.Count() - 1],
                path = file,
                edit = false,
                view = false,
                download = false,
                extract = false,
                copy = false,
                move = false
            };
            string ext = "";
            if(IsDirectory(file))
            {
                item.icon = "folder.png";
                DirectoryInfo dinfo = new DirectoryInfo(file);
                item.size = "4 KB";
                item.uid = Directory.GetAccessControl(file).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                item.gid = "";
                item.mtime = dinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                item.atime = dinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss");
                item.ctime = dinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                item.copy = true;
                item.move = true;
                item.download = true;
                string[] name_spl = item.name.Split('.');
                ext = name_spl[name_spl.Count() - 1].ToLower();
                item.icon = String.Format("{0}-icon-24x24.png", ext);
                string icon_path = HttpContext.Current.Server.MapPath("~") + "\\Content\\img\\doc_icons\\" + item.icon;
                if(!File.Exists(icon_path))
                {
                    item.icon = item.icon.Replace(ext, "txt");
                }
                FileInfo finfo = new FileInfo(file);
                item.size = String.Format(new FileSizeFormatProvider(), "{0:fs}", finfo.Length);
                item.uid = File.GetAccessControl(file).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                item.gid = "";
                item.mtime = finfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                item.atime = finfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss");
                item.ctime = finfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            item.mime = MimeMapping.GetMimeMapping(file);
            item.edit = Editable(ext);
            item.extract = Extractable(ext, item.name);
            item.view = Viewable(ext);

            return item;
        }

        public static bool Editable(string ext)
        {
            return Config.GetEditableExtensions().Contains(ext);
        }

        public static RequestMethodStatus CreateFile(string path, string name, string root)
        {
            RequestMethodStatus response = new RequestMethodStatus();
            try
            {
                path = AppendEndSlash(path);
                string full_path = path + name;
                StreamWriter new_file = File.CreateText(full_path);
                string[] name_spl = name.Split('.');
                string ext = name_spl[name_spl.Count() - 1];
                new_file.Close();
                return new RequestMethodStatus() { status = true, results = new List<object> { full_path, Editable(ext), Viewable(ext) } };
            }
            catch (Exception ex)
            {
                response.status = false;
                response.results = new List<object> { ex.Message };
            }
            return response;
        }
        public static RequestMethodStatus CreateFolder(string path, string name, string root)
        {
            RequestMethodStatus response = new RequestMethodStatus();
            try
            {
                path = AppendEndSlash(path);
                string full_path = path + name;
                Directory.CreateDirectory(full_path);
                return new RequestMethodStatus() { status = true, results = new List<object> { full_path, DrawFolderTree(root) } };
            }
            catch (Exception ex)
            {
                response.status = false;
                response.results = new List<object> { ex.Message, DrawFolderTree(root) };
            }
            return response;
        }

        public static bool Viewable(string ext)
        {
            return ext != "";
        }

        public static bool Extractable(string ext, string name)
        {
            return ext == "rar" || ext == "zip" || ext == "tar" || name.EndsWith("tar.gz");
        }

        public static bool TFSEnabled(string path)
        {
            return File.Exists(AppendEndSlash(path) + "tfs_enabled.txt");
        }

        public static bool IsDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }

        public static RequestMethodStatus ExtractFile(string file, string dest_path, string current_folder, bool overwrite, bool go_to_item)
        {
            try
            {
                if(file.EndsWith(".zip"))
                {
                    using (ZipArchive zip = ZipFile.OpenRead(file))
                    {
                        zip.ExtractToDirectory(dest_path);
                    }
                }
                else if(file.EndsWith(".tar.gz"))
                {
                    Stream inStream = File.OpenRead(file);
                    Stream gzipStream = new GZipInputStream(inStream);

                    TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                    tarArchive.ExtractContents(dest_path);
                    tarArchive.Close();

                    gzipStream.Close();
                    inStream.Close();
                }
                else if(file.EndsWith(".rar"))
                {
                    string process_path = @"C:\Program Files\WinRAR\Unrar.exe";
                    string rarCmd = "x -inul -y -o+ -ep1 \"" + file + "\" \"" + dest_path + "\"";
                    Core.RunProcess(process_path, rarCmd, dest_path);
                }
                else
                {
                    return new RequestMethodStatus() { status = false, results = String.Format("File {0} not a valid archive", file) };
                }
                return new RequestMethodStatus() { status = true,
                    results = new Dictionary<string, object>() {
                        { "path", go_to_item ? dest_path : current_folder },
                        { "folders", Core.GetSession("root_folder") != "" ? DrawFolderTree(Core.GetSession("root_folder")) : "" },
                        { "errors", new List<string>() },
                        { "extracted", 1 }
                    }
                };
            }
            catch (Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        public static RequestMethodStatus CopyFile(string src, string dest, bool overwrite)
        {
            try
            {
                string s_path, s_name = "";
                SplitPath(src, out s_path, out s_name);
                string dest_path = AppendEndSlash(dest) + s_name;
                if(!File.Exists(dest_path) || overwrite)
                {
                    File.Copy(src, dest_path);
                    return new RequestMethodStatus() { status = true, results = "OK" };
                }
                return new RequestMethodStatus() { status = false, results = String.Format("File {0} already exists, check the overwrite checkbox to overwrite", dest_path) };
            }
            catch(Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        public static RequestMethodStatus MoveFiles(List<string> inFiles, string dest, bool overwrite)
        {
            int moved = 0;
            List<string> errors = new List<string>();
            try
            {
                foreach (string src in inFiles)
                {
                    string s_path, s_name = "";
                    SplitPath(src, out s_path, out s_name);
                    string dest_path = AppendEndSlash(dest) + s_name;
                    if (!File.Exists(dest_path) || overwrite)
                    {
                        File.Move(src, dest_path);
                        moved += 1;
                    }
                    else
                    {
                        errors.Add(String.Format("File {0} already exists, check the overwrite checkbox to overwrite", dest_path));
                    }
                }
                if(moved == 0)
                {
                    return new RequestMethodStatus() { status = false, results = String.Join(",", errors.ToArray()) };
                }
                return new RequestMethodStatus() { status = true, results = new Dictionary<string, object>() { { "Moved", moved }, { "Errors", errors } } };
            }
            catch (Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        public static void SplitPath(string path, out string head, out string tail)
        {

            // Get the directory separation character (i.e. '\').
            string separator = System.IO.Path.DirectorySeparatorChar.ToString();

            // Trim any separators at the end of the path
            string lastCharacter = path.Substring(path.Length - 1);
            if (separator == lastCharacter)
            {
                path = path.Substring(0, path.Length - 1);
            }

            int lastSeparatorIndex = path.LastIndexOf(separator);

            head = path.Substring(0, lastSeparatorIndex);
            tail = path.Substring(lastSeparatorIndex + separator.Length,
                path.Length - lastSeparatorIndex - separator.Length);

        }

        public static RequestMethodStatus DeleteFiles(List<string> inFiles)
        {
            try
            {
                foreach(string file in inFiles)
                {
                    if(IsDirectory(file))
                    {
                        Directory.Delete(file, true);
                    }
                    else
                    {
                        File.Delete(file);
                    }
                }
                string folders = "";
                if(Core.GetSession("root_folder") != "")
                {
                    folders = DrawFolderTree(Core.GetSession("root_folder"));
                }

                return new RequestMethodStatus() { status = true, results = new Dictionary<string, object>() { { "status", "OK" }, { "folders", folders } } };
            }
            catch(Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        public static RequestMethodStatus RenameFile(string dest_path, string inFile, string new_name)
        {
            try
            {
                string new_path = AppendEndSlash(dest_path) + new_name;
                if(!File.Exists(new_path))
                {
                    File.Move(inFile, new_path);
                    return new RequestMethodStatus() { status = true, results = "OK" };
                }
                else
                {
                    return new RequestMethodStatus() { status = false, results = String.Format("File {0} already exists", new_path) };
                }
            }
            catch(Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        public static RequestMethodStatus CreateArchive(string dest_path, string name, string type, List<string> inFiles, bool overwrite)
        {
            try
            {
                string archive_file = AppendEndSlash(dest_path) + name;
                if (!archive_file.EndsWith(type))
                {
                    archive_file += "." + type;
                }

                if (!File.Exists(archive_file) || overwrite)
                {
                    switch (type)
                    {
                        case "zip":
                            using (ZipArchive zip = ZipFile.Open(archive_file, File.Exists(archive_file) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                            {
                                foreach (string file in inFiles)
                                {
                                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                                }
                            }
                            break;
                        case "tar.gz":
                            using (var outStream = File.Create(archive_file))
                            using (var gzoStream = new GZipOutputStream(outStream))
                            using (var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
                            {
                                foreach (string file in inFiles)
                                {
                                    var tarEntry = TarEntry.CreateEntryFromFile(file);
                                    tarEntry.Name = Path.GetFileName(Path.GetFileName(file));

                                    tarArchive.WriteEntry(tarEntry, true);
                                }
                            }
                            break;
                        case "rar":
                            string process_path = @"C:\Program Files\WinRAR\Rar.exe";
                            foreach(string file in inFiles)
                            {
                                string rarCmd = " a " + archive_file + " \"" + file + "\" -r -ep1";
                                Core.RunProcess(process_path, rarCmd, dest_path);
                            }
                            break;

                        default:
                            return new RequestMethodStatus() { status = false, results = String.Format("Type {0} not implemented", type) };
                            break;
                    }

                    return new RequestMethodStatus() { status = true, results = new Dictionary<string, object>() { { "path", dest_path }, { "file", archive_file } } };
                }
                else
                {
                    return new RequestMethodStatus() { status = false, results = String.Format("File {0} already exists, check the overwrite checkbox to overwrite", dest_path) };
                }
            }
            catch (Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }

        public static RequestMethodStatus SaveContent(string inFile, string content)
        {
            try
            {
                File.WriteAllText(inFile, content);
                return new RequestMethodStatus() { status = true, results = "OK" };
            }
            catch(Exception ex)
            {
                return new RequestMethodStatus() { status = false, results = ex.Message };
            }
        }
    }
}

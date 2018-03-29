using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using WebFileManager.Functions;
using WebFileManager.Models;
using WebFileManager.Models.Ajax;

namespace WebFileManager.NET.Controllers
{
    public class AjaxController : Controller
    {
        // GET: Ajax
        [HttpPost]
        [Authorize]
        public string HandleRequest(string ajax_action, string ajax_method)
        {
            AjaxRequest request = CreateRequestObject(ajax_action, ajax_method);
            AjaxResponse response = ProcessRequest(ajax_action, ajax_method, request);

            return JsonConvert.SerializeObject(response);
        }

        private AjaxRequest CreateRequestObject(string action, string method)
        {
            AjaxRequest request = new AjaxRequest();
            request.action = action;
            request.method = method;
            request.data = new Dictionary<string, object>();
            Request.Form.CopyTo(request.data, false);
            return request;
        }

        private AjaxResponse ProcessRequest(string action, string method, AjaxRequest request)
        {
            AjaxResponse response = new AjaxResponse();
            RequestMethodStatus method_status = new RequestMethodStatus();
            switch (action)
            {
                #region Case tfs
                case "tfs":
                    switch (method)
                    {
                        case "commit":
                            if (!request.data.ContainsKey("path"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("path not specified") } });
                                break;
                            }
                            string path = request.data["path"].ToString();
                            if (!request.data.ContainsKey("message"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("message not specified") } });
                                break;
                            }
                            string message = request.data["message"].ToString();
                            method_status = TFS.Commit(path, message);
                            if(method_status.status)
                            {
                                response = new AjaxResponse(true, "OK", request, method_status.results as Dictionary<string, object>, null);
                                break;
                            }
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });
                            break;
                        default:
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("Method {1} for Action {0} not defined", action, method) } });
                            break;
                    }
                    break;
                #endregion
                #region Case folder
                case "folder":
                case "folders":
                    switch(method)
                    {
                        case "get":
                            if (!request.data.ContainsKey("path"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("path not specified") } });
                                break;
                            }
                            string path = request.data["path"].ToString();
                            method_status = Folders.GetFolderItems(path);
                            if(!method_status.status)
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });
                                break;
                            }
                            response = new AjaxResponse(true, "OK", request, method_status.results as Dictionary<string, object>, null);
                            break;
                        case "new":
                            if (!request.data.ContainsKey("current_folder"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("current_folder not specified") } });
                                break;
                            }
                            string current_folder = request.data["current_folder"].ToString();
                            if (!request.data.ContainsKey("root_folder"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("root_folder not specified") } });
                                break;
                            }
                            string root_folder = request.data["root_folder"].ToString();
                            if (!request.data.ContainsKey("name"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("name not specified") } });
                                break;
                            }
                            string name = request.data["name"].ToString();
                            bool go_to_item = false;
                            if(request.data.ContainsKey("go_to_item"))
                            {
                                if (!Regex.IsMatch(request.data["go_to_item"].ToString().ToLower(), "(false|f|no|0|true|t|yes|1)"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("go_to_item not a boolean") } });
                                    break;
                                }
                                else
                                {
                                    go_to_item = Regex.IsMatch(request.data["go_to_item"].ToString().ToLower(), "(true|t|yes|1)");
                                }
                            }

                            method_status = Folders.CreateFolder(current_folder, name, root_folder);
                            List<object> returnVals = method_status.results as List<object>;
                            if(method_status.status == false)
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", returnVals[0].ToString() } });
                            }
                            else
                            {
                                Dictionary<string, object> results = new Dictionary<string,object> { { "path", current_folder }, { "folder_html", returnVals[1].ToString() }, { "go_to_item", go_to_item }, { "item_type", "folder" }, { "file_editable", false }, { "root_folder", root_folder } };
                                if(go_to_item)
                                {
                                    results["path"] = returnVals[0];
                                }
                                response = new AjaxResponse(true, "OK", request, results, null);
                            }
                            break;
                        default:
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("Method {1} for Action {0} not defined", action, method) } });
                            break;
                    }
                    break;
                #endregion
                #region Case file
                case "file":
                case "files":
                    switch (method)
                    {
                        case "new":
                            if (!request.data.ContainsKey("current_folder"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("current_folder not specified") } });
                                break;
                            }
                            string current_folder = request.data["current_folder"].ToString();
                            if (!request.data.ContainsKey("root_folder"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("root_folder not specified") } });
                                break;
                            }
                            string root_folder = request.data["root_folder"].ToString();
                            if (!request.data.ContainsKey("name"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("name not specified") } });
                                break;
                            }
                            string name = request.data["name"].ToString();
                            bool go_to_item = false;
                            if (request.data.ContainsKey("go_to_item"))
                            {
                                if (!Regex.IsMatch(request.data["go_to_item"].ToString().ToLower(), "(false|f|no|0|true|t|yes|1)"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("go_to_item not a boolean") } });
                                    break;
                                }
                                else
                                {
                                    go_to_item = Regex.IsMatch(request.data["go_to_item"].ToString().ToLower(), "(true|t|yes|1)");
                                }
                            }

                            method_status = Folders.CreateFile(current_folder, name, root_folder);
                            List<object> returnVals = method_status.results as List<object>;
                            if (method_status.status == false)
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", returnVals[0].ToString() } });
                            }
                            else
                            {
                                Dictionary<string, object> results = new Dictionary<string, object> { { "file_viewable", returnVals[2] }, { "path", current_folder }, { "go_to_item", go_to_item }, { "item_type", "file" }, { "file_editable", returnVals[1] }, { "root_folder", root_folder } };
                                if (go_to_item)
                                {
                                    results["path"] = returnVals[0];
                                }
                                response = new AjaxResponse(true, "OK", request, results, null);
                            }
                            break;
                        case "copy":
                        case "move-file":
                        case "move":
                        case "extract":
                            if (!request.data.ContainsKey("current_folder"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("current_folder not specified") } });
                                break;
                            }
                            current_folder = request.data["current_folder"].ToString();
                            if (!request.data.ContainsKey("dest_path"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("dest_path not specified") } });
                                break;
                            }
                            string dest_path = request.data["dest_path"].ToString();
                            bool overwrite = false;
                            if (request.data.ContainsKey("overwrite"))
                            {
                                if (!Regex.IsMatch(request.data["overwrite"].ToString().ToLower(), "(false|f|no|0|true|t|yes|1)"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("overwrite not a boolean") } });
                                    break;
                                }
                                else
                                {
                                    overwrite = Regex.IsMatch(request.data["overwrite"].ToString().ToLower(), "(true|t|yes|1)");
                                }
                            }
                            List<string> inFiles = new List<string>();
                            object files = null;
                            string file = "";
                            string[] file_spl;
                            string id = "";
                            if (method == "extract")
                            {
                                if (!request.data.ContainsKey("file"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("file not specified") } });
                                    break;
                                }
                                go_to_item = false;
                                if (request.data.ContainsKey("go_to_item"))
                                {
                                    if (!Regex.IsMatch(request.data["go_to_item"].ToString().ToLower(), "(false|f|no|0|true|t|yes|1)"))
                                    {
                                        response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("go_to_item not a boolean") } });
                                        break;
                                    }
                                    else
                                    {
                                        go_to_item = Regex.IsMatch(request.data["go_to_item"].ToString().ToLower(), "(true|t|yes|1)");
                                    }
                                }
                                file = request.data["file"].ToString();
                                file_spl = file.Split('|');
                                id = file_spl[0];
                                file = file_spl[1];

                                method_status = Folders.ExtractFile(file, dest_path, current_folder, overwrite, go_to_item);
                            }
                            else
                            {
                                if (!request.data.ContainsKey("files[]"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("files not specified") } });
                                    break;
                                }

                                files = request.data["files[]"];
                                if (files is String)
                                {
                                    inFiles = files.ToString().Split(',').ToList();
                                }
                                else if (files is List<string>)
                                {
                                    inFiles = files as List<string>;
                                }
                                else
                                {
                                    return new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", "files in unexpected format. Expecting string or list got " + files.GetType().Name } });
                                }

                                for (int idx = 0; idx < inFiles.Count(); idx++)
                                {
                                    inFiles[idx] = inFiles[idx].Split('|')[1];
                                }

                                if (method == "copy")
                                {
                                    method_status = Folders.CopyFile(inFiles[0], dest_path, overwrite);
                                }
                                else
                                {
                                    method_status = Folders.MoveFiles(inFiles, dest_path, overwrite);
                                }
                            }

                            if (method_status.status == false)
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });
                            }
                            else
                            {
                                response = new AjaxResponse(true, "OK", request, new Dictionary<string, object>() { { "path", dest_path }, { "files", inFiles }, { "Response", method_status.results } }, null);
                            }

                            break;
                        case "delete":
                            if (!request.data.ContainsKey("files[]"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("files not specified") } });
                                break;
                            }

                            files = request.data["files[]"];
                            if (files is String)
                            {
                                inFiles = files.ToString().Split(',').ToList();
                            }
                            else if (files is List<string>)
                            {
                                inFiles = files as List<string>;
                            }
                            else
                            {
                                return new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", "files in unexpected format. Expecting string or list got " + files.GetType().Name } });
                            }

                            for (int idx = 0; idx < inFiles.Count(); idx++)
                            {
                                inFiles[idx] = inFiles[idx].Split('|')[1];
                            }

                            method_status = Folders.DeleteFiles(inFiles);
                            if (method_status.status == false)
                            {

                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });
                            }
                            else
                            {
                                response = new AjaxResponse(true, "OK", request, method_status.results as Dictionary<string, object>, null);
                            }
                            break;
                        case "properties":
                            if (!request.data.ContainsKey("file"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("file not specified") } });
                                break;
                            }
                            string inFile = request.data["file"].ToString();
                            file_spl = inFile.Split('|');
                            id = file_spl[0];
                            inFile = file_spl[1];

                            response = new AjaxResponse(true, "OK", request, new Dictionary<string, object>() { { "File", Folders.GetItem(inFile, 1) } }, null);

                            break;
                        case "rename":
                            if (!request.data.ContainsKey("dest_path"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("dest_path not specified") } });
                                break;
                            }
                            dest_path = request.data["dest_path"].ToString();
                            if (!request.data.ContainsKey("file"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("file not specified") } });
                                break;
                            }
                            inFile = request.data["file"].ToString();
                            file_spl = inFile.Split('|');
                            id = file_spl[0];
                            inFile = file_spl[1];
                            if (!request.data.ContainsKey("new_name"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("new_name not specified") } });
                                break;
                            }
                            string new_name = request.data["new_name"].ToString();

                            method_status = Folders.RenameFile(dest_path, inFile, new_name);
                            if (method_status.status)
                            {
                                response = new AjaxResponse(true, "OK", request, new Dictionary<string, object>() { { "path", dest_path }, { "file", new_name } }, null);
                                break;
                            }
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });

                            break;
                        case "compress":
                            if (!request.data.ContainsKey("dest_path"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("dest_path not specified") } });
                                break;
                            }
                            dest_path = request.data["dest_path"].ToString();
                            if (!request.data.ContainsKey("files[]"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("files not specified") } });
                                break;
                            }

                            files = request.data["files[]"];
                            if (files is String)
                            {
                                inFiles = files.ToString().Split(',').ToList();
                            }
                            else if (files is List<string>)
                            {
                                inFiles = files as List<string>;
                            }
                            else
                            {
                                return new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", "files in unexpected format. Expecting string or list got " + files.GetType().Name } });
                            }

                            for (int idx = 0; idx < inFiles.Count(); idx++)
                            {
                                inFiles[idx] = inFiles[idx].Split('|')[1];
                            }

                            if (!request.data.ContainsKey("name"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("name not specified") } });
                                break;
                            }
                            name = request.data["name"].ToString();
                            if (!request.data.ContainsKey("type"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("type not specified") } });
                                break;
                            }
                            string type = request.data["type"].ToString();
                            overwrite = false;
                            if (request.data.ContainsKey("overwrite"))
                            {
                                if (!Regex.IsMatch(request.data["overwrite"].ToString().ToLower(), "(false|f|no|0|true|t|yes|1)"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("overwrite not a boolean") } });
                                    break;
                                }
                                else
                                {
                                    overwrite = Regex.IsMatch(request.data["overwrite"].ToString().ToLower(), "(true|t|yes|1)");
                                }
                            }

                            method_status = Folders.CreateArchive(dest_path, name, type, inFiles, overwrite);
                            if (method_status.status)
                            {
                                response = new AjaxResponse(true, "OK", request, method_status.results as Dictionary<string, object>, null);
                                break;
                            }
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });

                            break;
                        case "save":
                            if (!request.data.ContainsKey("file"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("file not specified") } });
                                break;
                            }
                            inFile = request.data["file"].ToString();
                            if (!request.data.ContainsKey("content"))
                            {
                                response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("content not specified") } });
                                break;
                            }
                            string content = request.data["content"].ToString();
                            bool close_after = false;
                            if (request.data.ContainsKey("close_after"))
                            {
                                if (!Regex.IsMatch(request.data["close_after"].ToString().ToLower(), "(false|f|no|0|true|t|yes|1)"))
                                {
                                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("close_after not a boolean") } });
                                    break;
                                }
                                else
                                {
                                    close_after = Regex.IsMatch(request.data["close_after"].ToString().ToLower(), "(true|t|yes|1)");
                                }
                            }

                            method_status = Folders.SaveContent(inFile, content);
                            if (method_status.status)
                            {
                                response = new AjaxResponse(true, "OK", request, new Dictionary<string, object>() { { "close_after", close_after }, { "response", method_status.results } }, null);
                                break;
                            }
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", method_status.results.ToString() } });

                            break;
                        default:
                            response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("Method {1} for Action {0} not defined", action, method) } });
                            break;
                    }
                    break;
                #endregion
                default:
                    response = new AjaxResponse(false, "Error", request, null, new Dictionary<string, string>() { { "Message", String.Format("Action {0} not defined", action) } });
                    break;
            }
            return response;
        }

        
    }
}
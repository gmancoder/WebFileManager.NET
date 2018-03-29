using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebFileManager.Functions;
using WebFileManager.Models;
using WebFileManager.Models.ViewModels;

namespace WebFileManager.NET.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index(string e = null, string i = null)
        {
            if(Request.HttpMethod == HttpMethod.Post.Method)
            {
                string root_folder = Request.Form["root_folder"];
                Session["root_folder"] = root_folder;
                
            }

            string page = "Index";
            if(Core.SessionKeyExists("root_folder"))
            {
                page = "Manager";
                ViewBag.globals = Core.Init();
                ViewBag.items = Folders.DrawFolderTree(Core.GetSession("root_folder"));
            }
            else
            {
                ViewBag.items = Config.GetShortcuts();
            }

            if(!String.IsNullOrWhiteSpace(e))
            {
                List<FlashMessage> flash = new List<FlashMessage>();
                flash.Add(new FlashMessage { Category = "danger", Message = e });
                ViewBag.flash = flash;
            }
            else if(!String.IsNullOrWhiteSpace(i))
            {
                List<FlashMessage> flash = new List<FlashMessage>();
                flash.Add(new FlashMessage { Category = "info", Message = i });
                ViewBag.flash = flash;
            }
            return View(page);
        }

        [Authorize]
        public ActionResult ChangeFolder()
        {
            Session["current_folder"] = null;
            Session["root_folder"] = null;
            return RedirectToAction("Index");
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel vm)
        {
            if(FormsAuthentication.Authenticate(vm.username, vm.password))
            {
                FormsAuthentication.SetAuthCookie(vm.username, false);
                return RedirectToAction("Index");
            }
            else
            {
                List<FlashMessage> flash = new List<FlashMessage>();
                flash.Add(new FlashMessage { Category = "danger", Message = "Login failed" });
                ViewBag.flash = flash;
            }
            return View(vm);
        }

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Upload(UploadViewModel vm)
        {
            var file = vm.Files[0];
            string full_path = Folders.AppendEndSlash(vm.DestinationPath) + file.FileName;
            if(!System.IO.File.Exists(full_path) || vm.Overwrite)
            {
                file.SaveAs(full_path);
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index", new { e = String.Format("File {0} already exists, check the overwrite checkbox to overwrite", full_path) });
            }
            
        }

        public ActionResult Download(string dl)
        {
            return _View(dl, false);
        }

        public ActionResult ViewFile(string f)
        {
            return _View(f, true);
        }

        public ActionResult Edit(string f)
        {
            ViewBag.globals = Core.Init();
            ViewBag.items = Folders.DrawFolderTree(Core.GetSession("root_folder"));
            if (String.IsNullOrWhiteSpace(f))
            {
                return RedirectToAction("Index", new { e = "file not specified" });
            }

            string full_path = Folders.AppendEndSlash(Core.GetSession("current_folder")) + f;
            string content = System.IO.File.ReadAllText(full_path);

            EditViewModel evm = new EditViewModel()
            {
                full_path = full_path,
                content = content
            };

            ViewBag.Title = "Edit " + full_path;

            return View(evm);
        }

        private void SetupViewBag(dynamic vb)
        {
            ViewBag.globals = vb.globals;
            ViewBag.items = vb.items;
            if(vb.flash != null)
            {
                ViewBag.flash = vb.flash;
            }
        }

        private ActionResult _View(string f, bool view)
        {
            try
            {
                string current_folder = Session["current_folder"].ToString();
                byte[] fileBytes = System.IO.File.ReadAllBytes(Folders.AppendEndSlash(current_folder) + f);
                var cd = new System.Net.Mime.ContentDisposition
                {
                    // for example foo.bak
                    FileName = f,

                    // always prompt the user for downloading, set to true if you want 
                    // the browser to try to show the file inline
                    Inline = view,
                };
                Response.AppendHeader("Content-Disposition", cd.ToString());
                return File(fileBytes, MimeMapping.GetMimeMapping(f));
            }
            catch(Exception ex)
            {
                List<FlashMessage> flash = new List<FlashMessage>();
                flash.Add(new FlashMessage { Category = "danger", Message = ex.Message });
                ViewBag.flash = flash;
                return View("Manager");
            }
        }
    }
}
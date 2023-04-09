using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.DB;
using Platini.Models;
using MvcPaging;

namespace Platini.Areas.Common.Controllers
{
    public class BackgroundsController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index(int? page, string sortOrder, string sortColumn = "SortOrder")
        {
            if (string.IsNullOrEmpty(sortOrder))
            {
                sortOrder = "asc";
                ViewBag.currentOrderParam = "asc";
                ViewBag.sortOrderParam = "desc";
            }
            ViewBag.currentOrderParam = sortOrder;
            ViewBag.sortOrderParam = (sortOrder == "desc") ? "asc" : "desc";

            ViewBag.sortColumnParam = sortColumn;

            var pics = db.BackgroundPictures.Where(x => x.IsDelete == false).OrderBy(x => x.SortOrder).ToList();

            List<BackgroundPicture> sortedList = pics;
            if (typeof(BackgroundPicture).GetProperty(sortColumn) != null)
            {
                Type sortByPropType = typeof(BackgroundPicture).GetProperty(sortColumn).PropertyType;
                sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(BackgroundPicture), sortByPropType })
                                            .Invoke(pics, new object[] { pics, sortColumn, sortOrder }) as List<BackgroundPicture>;
            }

            var retList = sortedList.Select(x => new CommonClass
            {
                DateCreated = x.DateCreated,
                Id = x.PictureId,
                Name = x.Picture,
                SortOrder = x.SortOrder.HasValue ? x.SortOrder.Value : 0,
                IsActive = x.IsActive.HasValue ? x.IsActive.Value : false
            });
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(retList.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult Create()
        {
            var retModel = new BackgroundClass();
            return View("CreateOrEdit", retModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BackgroundClass ret, IEnumerable<HttpPostedFileBase> fMUpBackGround, HttpPostedFileBase fUpBackGround)
        {
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                if ((fMUpBackGround != null && fMUpBackGround.Count(x => x != null) > 0) || fUpBackGround != null)
                {
                    List<string> msgs = new List<string>();
                    var pic = new BackgroundPicture();
                    pic.SortOrder = ret.SortOrder;
                    pic.IsActive = ret.IsActive;
                    pic.IsDelete = false;
                    if (fUpBackGround != null && fUpBackGround.ContentLength != 0 && fUpBackGround.InputStream != null)
                    {
                        string fileName;
                        System.Drawing.Imaging.ImageFormat format;
                        if (VerifyImage(fUpBackGround.ContentType.ToLower(), out fileName, out format))
                        {
                            pic.Picture = fileName;
                            System.Drawing.Image.FromStream(fUpBackGround.InputStream).Save(Server.MapPath("~/Library/Backgrounds/" + fileName), format);
                        }
                        else
                            msgs.Add(string.Format("{0} does not appear to be a valid image type", fUpBackGround.FileName));
                    }
                    foreach (var upPic in fMUpBackGround)
                    {
                        if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                        {
                            string fileName;
                            System.Drawing.Imaging.ImageFormat format;
                            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
                            {
                                using (var image = System.Drawing.Image.FromStream(upPic.InputStream))
                                {
                                    bool used = false;
                                    int H = image.Height;
                                    int W = image.Width;
                                    if (W == 750 && H == 1334)
                                    {
                                        pic.Picture_iPhone6 = fileName;
                                        used = true;
                                    }
                                    else if (W == 1242 && H == 2208)
                                    {
                                        pic.Picture_iPhone6s = fileName;
                                        used = true;
                                    }
                                    else if (W == 768 && H == 1024)
                                    {
                                        pic.Picture_iPadP = fileName;
                                        used = true;
                                    }
                                    else if (W == 1024 && H == 768)
                                    {
                                        pic.Picture_iPadL = fileName;
                                        used = true;
                                    }
                                    else if (W == 1536 && H == 2048)
                                    {
                                        pic.Picture_iPadRP = fileName;
                                        used = true;
                                    }
                                    else if (W == 2048 && H == 1536)
                                    {
                                        pic.Picture_iPadRL = fileName;
                                        used = true;
                                    }
                                    if (used)
                                        image.Save(Server.MapPath("~/Library/Backgrounds/" + fileName), format);
                                    else
                                        msgs.Add(string.Format("{0} is too large to upload through this area." +Environment.NewLine + "Image should be in these sizes" + Environment.NewLine + "[750 x 1334], [1242 x 2208], [768 x 1024],[1024 x 768], [1536 x 2048],[2048 x 1536]", upPic.FileName));
                                }
                            }
                            else
                                msgs.Add(string.Format("{0} does not appear to be a valid image type", upPic.FileName));
                        }
                    }
                    pic.DateCreated = pic.DateUpdated = DateTime.UtcNow;
                    db.BackgroundPictures.Add(pic);
                    db.SaveChanges();
                    if (msgs.Count > 0)
                        TempData["PageMessage"] = string.Join(". ", msgs);
                    return RedirectToAction("Index");
                }
                else
                    ViewBag.PageMessage = "No file provided.";

            }
            return View("CreateOrEdit", ret);
        }

        public ActionResult Edit(int Id = 0)
        {
            var pic = db.BackgroundPictures.Find(Id);
            if (pic != null)
            {
                var ret = new BackgroundClass()
                {
                    Id = pic.PictureId,
                    IsActive = pic.IsActive.HasValue ? pic.IsActive.Value : false,
                    SortOrder = pic.SortOrder.HasValue ? pic.SortOrder.Value : 0
                };
                if (!string.IsNullOrEmpty(pic.Picture))
                    ret.MainPic = pic.Picture;
                if (!string.IsNullOrEmpty(pic.Picture_iPhone6))
                    ret.MobPics.Add(new SelectedStringValues() { Id = "iPhone6", Value = pic.Picture_iPhone6 });
                if (!string.IsNullOrEmpty(pic.Picture_iPhone6s))
                    ret.MobPics.Add(new SelectedStringValues() { Id = "iPhone6+", Value = pic.Picture_iPhone6s });
                if (!string.IsNullOrEmpty(pic.Picture_iPadL))
                    ret.MobPics.Add(new SelectedStringValues() { Id = "iPad Landscape", Value = pic.Picture_iPadL });
                if (!string.IsNullOrEmpty(pic.Picture_iPadP))
                    ret.MobPics.Add(new SelectedStringValues() { Id = "iPad Portrait", Value = pic.Picture_iPadP });
                if (!string.IsNullOrEmpty(pic.Picture_iPadRL))
                    ret.MobPics.Add(new SelectedStringValues() { Id = "iPad(Retina) Landscape", Value = pic.Picture_iPadRL });
                if (!string.IsNullOrEmpty(pic.Picture_iPadRP))
                    ret.MobPics.Add(new SelectedStringValues() { Id = "iPad(Retina) Portrait ", Value = pic.Picture_iPadRP });

                return View("CreateOrEdit", ret);
            }
            ViewBag.PageMessage = "Record not found";
            return View("CreateOrEdit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(BackgroundClass ret, IEnumerable<HttpPostedFileBase> fMUpBackGround, HttpPostedFileBase fUpBackGround)
        {
            if (ModelState.IsValid)
            {
                var pic = db.BackgroundPictures.Find(ret.Id);
                if (pic != null)
                {
                    List<string> msgs = new List<string>();
                    if (fUpBackGround != null && fUpBackGround.ContentLength != 0 && fUpBackGround.InputStream != null)
                    {
                        string fileName;
                        System.Drawing.Imaging.ImageFormat format;
                        if (VerifyImage(fUpBackGround.ContentType.ToLower(), out fileName, out format))
                        {
                            if (!string.IsNullOrEmpty(pic.Picture))
                                System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture));
                            pic.Picture = fileName;
                            System.Drawing.Image.FromStream(fUpBackGround.InputStream).Save(Server.MapPath("~/Library/Backgrounds/" + fileName), format);
                        }
                        else
                            msgs.Add(string.Format("{0} does not appear to be a valid image type", fUpBackGround.FileName));
                    }
                    foreach (var upPic in fMUpBackGround)
                    {
                        if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                        {
                            string fileName;
                            System.Drawing.Imaging.ImageFormat format;
                            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
                            {
                                using (var image = System.Drawing.Image.FromStream(upPic.InputStream))
                                {
                                    bool used = false;
                                    int H = image.Height;
                                    int W = image.Width;
                                    if (W == 750 && H == 1334)
                                    {
                                        if (!string.IsNullOrEmpty(pic.Picture_iPhone6))
                                            System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPhone6));
                                        pic.Picture_iPhone6 = fileName;
                                        used = true;
                                    }
                                    else if (W == 1242 && H == 2208)
                                    {
                                        if (!string.IsNullOrEmpty(pic.Picture_iPhone6s))
                                            System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPhone6s));
                                        pic.Picture_iPhone6s = fileName;
                                        used = true;
                                    }
                                    else if (W == 768 && H == 1024)
                                    {
                                        if (!string.IsNullOrEmpty(pic.Picture_iPadP))
                                            System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadP));
                                        pic.Picture_iPadP = fileName;
                                        used = true;
                                    }
                                    else if (W == 1024 && H == 768)
                                    {
                                        if (!string.IsNullOrEmpty(pic.Picture_iPadL))
                                            System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadL));
                                        pic.Picture_iPadL = fileName;
                                        used = true;
                                    }
                                    else if (W == 1536 && H == 2048)
                                    {
                                        if (!string.IsNullOrEmpty(pic.Picture_iPadRP))
                                            System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadRP));
                                        pic.Picture_iPadRP = fileName;
                                        used = true;
                                    }
                                    else if (W == 2048 && H == 1536)
                                    {
                                        if (!string.IsNullOrEmpty(pic.Picture_iPadRL))
                                            System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadRL));
                                        pic.Picture_iPadRL = fileName;
                                        used = true;
                                    }
                                    if (used)
                                        image.Save(Server.MapPath("~/Library/Backgrounds/" + fileName), format);
                                    else
                                        msgs.Add(string.Format("{0} is too large to upload through this area." + Environment.NewLine + "Image should be in these sizes" + Environment.NewLine + "[750 x 1334], [1242 x 2208], [768 x 1024],[1024 x 768], [1536 x 2048],[2048 x 1536]", upPic.FileName));
                                }
                            }
                            else
                                msgs.Add(string.Format("{0} does not appear to be a valid image type", upPic.FileName));
                        }
                    }
                    pic.IsActive = ret.IsActive;
                    pic.DateUpdated = DateTime.UtcNow;
                    pic.SortOrder = ret.SortOrder;
                    db.SaveChanges();
                    if (msgs.Count > 0)
                        TempData["PageMessage"] = string.Join(". ", msgs);
                    return RedirectToAction("Index");
                }
                else
                    ViewBag.PageMessage = "Record not found";
            }
            return View("CreateOrEdit", ret);
        }

        public ActionResult Delete(int Id = 0)
        {
            var pic = db.BackgroundPictures.Find(Id);
            if (pic != null)
            {
                if (!string.IsNullOrEmpty(pic.Picture))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture));
                if (!string.IsNullOrEmpty(pic.Picture_iPhone6))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPhone6));
                if (!string.IsNullOrEmpty(pic.Picture_iPhone6s))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPhone6s));
                if (!string.IsNullOrEmpty(pic.Picture_iPadP))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadP));
                if (!string.IsNullOrEmpty(pic.Picture_iPadL))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadL));
                if (!string.IsNullOrEmpty(pic.Picture_iPadRP))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadRP));
                if (!string.IsNullOrEmpty(pic.Picture_iPadRL))
                    System.IO.File.Delete(Server.MapPath("~/Library/Backgrounds/" + pic.Picture_iPadRL));
                db.BackgroundPictures.Remove(pic);
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
            }
            else
            {
                TempData["PageMessage"] = "Record not found";
            }
            return RedirectToAction("Index");
        }

        [NonAction]
        protected bool VerifyImage(string contentType, out string fileName, out System.Drawing.Imaging.ImageFormat format)
        {
            bool proceed = true;
            fileName = Guid.NewGuid().ToString().GetHashCode().ToString("x");
            format = System.Drawing.Imaging.ImageFormat.Jpeg;
            switch (contentType)
            {
                case "image/jpg":
                    fileName += ".jpg";
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case "image/jpeg":
                    fileName += ".jpeg";
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case "image/png":
                    fileName += ".png";
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    break;
                case "image/gif":
                    fileName += ".gif";
                    format = System.Drawing.Imaging.ImageFormat.Gif;
                    break;
                case "image/bmp":
                    fileName += ".bmp";
                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                default:
                    fileName = "";
                    format = null;
                    proceed = false;
                    break;
            }
            return proceed;
        }

        [HttpPost]       
        public ActionResult UpdateVisibility(int Id, bool state)
        {
            var pic = db.BackgroundPictures.Find(Id);
            if(pic!=null)
            {
                pic.IsActive = state;
                db.SaveChanges();
                return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        }
    }
}

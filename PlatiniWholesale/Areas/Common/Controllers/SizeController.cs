using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.DB;
using Platini.Models;
using System.Web.Script.Serialization;
using MvcPaging;

namespace Platini.Areas.Common.Controllers
{
    public class SizeController : Controller
    {
        Entities db = new Entities();
        public ActionResult Home(int? sizeGroupId, int? page, string searchString, string sortOrder, string sortColumn = "SortOrder")
        {
            if (sizeGroupId.HasValue)
            {
                ViewBag.SizeGroupIdValueIfExist = sizeGroupId;
            }
            if (sortOrder == null)
            {
                sortOrder = "asc";
                ViewBag.currentOrderParam = "asc";
                ViewBag.sortOrderParam = "desc";
            }
            ViewBag.currentOrderParam = sortOrder;
            ViewBag.sortOrderParam = (sortOrder == "desc") ? "asc" : "desc";

            ViewBag.sortColumnParam = sortColumn;
            ViewBag.searchStringParam = searchString;

            var sizes = new List<Size>();
            if (sizeGroupId.HasValue)
            {
                sizes = db.Sizes.Where(i => i.SizeGroupId == sizeGroupId && i.IsDelete == false).OrderBy(x => x.SizeGroupId).ThenBy(x => x.SortOrder).ToList();
            }
            else
            {
                sizes = db.Sizes.Where(i => i.IsDelete == false).OrderBy(x => x.SizeGroupId).ThenBy(x => x.SortOrder).ToList();
            }
            
            string searchText = searchString;
            if (!ReferenceEquals(searchText, null))
                sizes = sizes.Where(e => e.Name.ToLower().Contains(searchText.ToLower())).ToList();

            Type sortByPropType = typeof(Size).GetProperty(sortColumn).PropertyType;
            List<Size> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Size), sortByPropType })
                                        .Invoke(sizes, new object[] { sizes, sortColumn, sortOrder }) as List<Size>;

            List<SizeClass> retList = new List<SizeClass>().InjectFrom(sortedList);
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(retList.ToPagedList(currentPageIndex, SiteConfiguration.defaultpageSize));
        }


        public ActionResult Delete(int Id = 0)
        {
            Size dbSize = db.Sizes.Find(Id);
            if (dbSize != null)
            {
                dbSize.IsDelete = true;
                dbSize.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        public string GetCategories(int Id = 0)
        {
            string strParent = "";
            while (Id > 0)
            {
                Category dbCategory = db.Categories.Find(Id);
                if (dbCategory != null)
                {
                    if (string.IsNullOrEmpty(strParent))
                        strParent = dbCategory.Name;
                    else
                        strParent = dbCategory.Name + " >> " + strParent;
                    if (dbCategory.ParentId > 0)
                    {
                        Id = dbCategory.ParentId;
                    }
                    else
                    {
                        Id = 0;
                    }
                }
                else
                {
                    Id = 0;
                }
            }
            return strParent;
        }

        public string GetSizeGroup(int Id = 0)
        {
            string strSizeGroup = "";
            var sizegroup = db.SizeGroups.Where(x => x.SizeGroupId == Id).FirstOrDefault();
            if (sizegroup != null)
                strSizeGroup = sizegroup.Name;
            return strSizeGroup;
        }


        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSubCategoryByCategoryId(string categoryId)
        {
            if (String.IsNullOrEmpty(categoryId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(categoryId, out id);
            if (isValid == true)
            {
                var result = db.Categories.Where(x => x.ParentId == id && x.IsActive == true && x.IsDelete == false).OrderBy(y => y.SortOrder).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.CategoryId.ToString() });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSubCategoryTypeBySubCategoryId(string subCategoryId)
        {
            if (String.IsNullOrEmpty(subCategoryId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(subCategoryId, out id);
            if (isValid == true)
            {
                var result = db.Categories.Where(x => x.ParentId == id && x.IsActive == true && x.IsDelete == false).OrderBy(y => y.SortOrder).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.CategoryId.ToString() });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSizeGroupBySubCategoryTypeId(string subCategoryTypeId)
        {
            if (String.IsNullOrEmpty(subCategoryTypeId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(subCategoryTypeId, out id);
            if (isValid == true)
            {
                var result = db.SizeGroups.Where(x => x.CategoryId == id && x.IsActive == true && x.IsDelete == false).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.SizeGroupId.ToString() }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSizeBySubCategoryTypeId(string subCategoryTypeId)
        {
            if (String.IsNullOrEmpty(subCategoryTypeId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(subCategoryTypeId, out id);
            if (isValid == true)
            {
                var result = db.Sizes.Where(x => x.SizeGroupId == id && x.IsActive == true && x.IsDelete == false).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.SizeId.ToString() }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSizesBySizeId(string sizeGroupId)
        {
            if (String.IsNullOrEmpty(sizeGroupId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(sizeGroupId, out id);
            if (isValid == true)
            {
                var result = db.Sizes.Where(x => x.SizeGroupId == id && x.IsActive == true && x.IsDelete == false).Select(y => new { Id = y.SizeId, Name = y.Name, SubCategoryTypesId = y.CategoryId, SizeId = y.SizeGroupId }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Index()
        {
            Models.SizeAdminModel model = new Models.SizeAdminModel();
            var categories = db.Categories.Where(x => x.ParentId == 0 && x.IsActive == true && x.IsDelete == false).ToList();
            model.CategoryList = categories;
            model.SubCategoryList = new List<Category>();
            model.CategoryTypeList = new List<Category>();
            model.SizeGroupList = new List<Platini.DB.SizeGroup>();
            return View(model);
        }


        public JsonResult DeleteSize(int sizeId)
        {
            Size dbSize = db.Sizes.Find(sizeId);
            if (dbSize != null)
            {
                dbSize.IsDelete = true;
                dbSize.DateUpdated = DateTime.Now;
                db.SaveChanges();
            }

            return Json(sizeId, JsonRequestBehavior.AllowGet);
        }

        public JsonResult EditSize(int sizeId, string sizeValue, int groupId)
        {
            string str = string.Empty;
            var chkExist = db.Sizes.Where(x => x.Name == sizeValue && x.SizeGroupId == groupId && x.SizeId!=sizeId && x.IsDelete == false).Any();
            if (!chkExist)
            {
                Size dbSize = db.Sizes.Find(sizeId);
                if (dbSize != null)
                {
                    dbSize.Name = sizeValue;
                    dbSize.DateUpdated = DateTime.Now;
                    db.SaveChanges();
                }
            }
            else
            {
                str = "A size with this name already exists.s";
            }

            return Json(str, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreateSizeGroup(string name, int categoryId)
        {
            string str = string.Empty;
            if (!String.IsNullOrEmpty(name))
            {
                var chkExist = db.SizeGroups.Where(x => x.Name == name && x.CategoryId == categoryId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    SizeGroup sg = new SizeGroup();
                    sg.Name = name;
                    sg.CategoryId = categoryId;
                    sg.IsActive = true;
                    sg.IsDelete = false;
                    sg.DateCreated = DateTime.Now;
                    sg.DateUpdated = DateTime.Now;
                    db.SizeGroups.Add(sg);
                    db.SaveChanges();
                    str = sg.SizeGroupId + "-" + sg.Name;
                }
                else
                {
                    str = "A size group with this name already exists.";
                }
            }
            return Json(str, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult AddSize(List<int> arrList, string name)
        {
            string str = string.Empty;
            if (!String.IsNullOrEmpty(name))
            {
                int sizeGroupId = arrList[0];
                int categoryTypeId = arrList[1];
                var chkExist = db.Sizes.Where(x => x.Name == name && x.SizeGroupId == sizeGroupId && x.CategoryId == categoryTypeId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    Size s = new Size();
                    s.Name = name;
                    s.SizeGroupId = sizeGroupId;
                    s.CategoryId = categoryTypeId;
                    s.IsActive = true;
                    s.IsDelete = false;
                    s.DateCreated = DateTime.Now;
                    s.DateUpdated = DateTime.Now;
                    db.Sizes.Add(s);
                    db.SaveChanges();
                    str = s.SizeId + "-" + s.Name;
                }
                else
                {
                    str = "A size with this name already exists.";
                }
            }

            return Json(str, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeleteSizeGroup(int sizeGroupId)
        {
            SizeGroup dbSizeGroup = db.SizeGroups.Find(sizeGroupId);
            if (dbSizeGroup != null)
            {
                dbSizeGroup.IsDelete = true;
                dbSizeGroup.DateUpdated = DateTime.Now;
                db.SaveChanges();
            }

            return Json(sizeGroupId, JsonRequestBehavior.AllowGet);
        }
    }
}
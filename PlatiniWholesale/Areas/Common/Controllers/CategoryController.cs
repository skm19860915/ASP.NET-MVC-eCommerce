using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.DB;
using Platini.Models;
using MvcPaging;

namespace Platini.Areas.Common.Controllers
{
    public class CategoryController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index(int? page, string searchString, string sortOrder, string sortColumn = "SortOrder")
        {
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

            var categories = db.Categories.Where(i => i.IsDelete == false && i.CategoryLevel == 0).OrderBy(x => x.SortOrder).ToList();
            string nameCategory = searchString;
            if (!ReferenceEquals(nameCategory, null))
                categories = categories.Where(e => e.Name.ToLower().Contains(nameCategory.ToLower())).ToList();

            List<Category> sortedList = new List<Category>();
            if (sortColumn != "SortOrder")
            {
                Type sortByPropType = typeof(Category).GetProperty(sortColumn).PropertyType;
                sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Category), sortByPropType })
                                            .Invoke(categories, new object[] { categories, sortColumn, sortOrder }) as List<Category>;
            }
            else if (sortColumn == "SortOrder")
            {
                if (sortOrder == "desc")
                {
                    sortedList = categories.OrderByDescending(x => x.SortOrder).ToList();
                }
                else if (sortOrder == "asc")
                {
                    sortedList = categories.OrderBy(x => x.SortOrder).ToList();
                }
            }

            List<CategoryClass> retList = new List<CategoryClass>().InjectFrom(sortedList);
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(retList.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult Create()
        {
            CategoryClass category = new CategoryClass();
            category.parentCategory = null;
            return View("CreateOrEdit", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryClass category)
        {
            ModelState.Remove("CategoryId");
            ModelState.Remove("ParentId");
            if (ModelState.IsValid)
            {
                var chkExist = db.Categories.Where(x => x.Name == category.Name && x.ParentId == category.ParentId && x.CategoryId != category.CategoryId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    Category dbCategory = new Category();
                    dbCategory.InjectClass(category);
                    dbCategory.ParentId = 0;
                    dbCategory.IsDelete = false;
                    dbCategory.CategoryLevel = 0;
                    dbCategory.DateCreated = DateTime.UtcNow;
                    dbCategory.DateUpdated = DateTime.UtcNow;
                    db.Categories.Add(dbCategory);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A category with this name already exists.";
                }
                
            }
            category.parentCategory = null;
            return View("CreateOrEdit", category);
        }

        public ActionResult Edit(int Id = 0)
        {
            Category dbCategory = db.Categories.Find(Id);
            if (dbCategory!=null)
            {
                CategoryClass category = new CategoryClass();
                category.InjectClass(dbCategory);
                category.parentCategory = null;
                return View("CreateOrEdit", category);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CategoryClass category)
        {
            ModelState.Remove("ParentId");
            if (ModelState.IsValid)
            {
                Category dbCategory = db.Categories.Find(category.CategoryId);
                if (dbCategory != null)
                {
                    var chkExist = db.Categories.Where(x => x.Name == category.Name && x.ParentId == category.ParentId && x.CategoryId != category.CategoryId && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbCategory.InjectClass(category);                       
                        dbCategory.ParentId = 0;
                        dbCategory.CategoryLevel = 0;
                        dbCategory.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "A category with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            category.parentCategory = null;
            return View("CreateOrEdit", category);
        }

        public ActionResult Delete(int Id = 0)
        {
            Category dbCategory = db.Categories.Find(Id);
            if (dbCategory != null)
            {
                db.Categories.Where(x => x.ParentId == Id && x.IsDelete == false).ToList().ForEach(x => { x.IsDelete = true; x.DateUpdated = DateTime.UtcNow; });
                dbCategory.IsDelete = true;
                dbCategory.DateUpdated = DateTime.UtcNow;                             
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }        
    }
}
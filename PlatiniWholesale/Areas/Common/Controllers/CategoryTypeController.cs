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
    public class CategoryTypeController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index(int? CatId, int? page, string searchString, string sortOrder, string sortColumn = "SortOrder")
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
            var categories= new List<Category>();
            if (CatId.HasValue)
            {
                categories = db.Categories.Where(i => i.IsDelete == false && i.CategoryLevel == 2 && i.ParentId == CatId.Value).OrderBy(x => x.SortOrder).ToList();
                ViewBag.CatId = CatId;
            }
            else
            {
                categories = db.Categories.Where(i => i.IsDelete == false && i.CategoryLevel == 2).OrderBy(x => x.SortOrder).ToList();
            }
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

        public ActionResult Create(int? CatId)
        {            
            CategoryClass category = new CategoryClass();
            category.parentCategory = GetParentClass();
            if (CatId.HasValue)
            {
                ViewBag.CatId = CatId;
                category.ParentId = CatId.Value;
            }
            return View("CreateOrEdit", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryClass category)
        {
            int? CatId = null;
            if (Request.QueryString["CatId"] != null)
            {
                CatId = Convert.ToInt32(Request.QueryString["CatId"]);
                ViewBag.CatId = CatId;
            }
            ModelState.Remove("CategoryId");           
            if (ModelState.IsValid)
            {
                var chkExist = db.Categories.Where(x => x.Name == category.Name && x.ParentId == category.ParentId && x.CategoryId != category.CategoryId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    Category dbCategory = new Category();
                    dbCategory.InjectClass(category);                    
                    dbCategory.IsDelete = false;
                    dbCategory.CategoryLevel = 2;
                    dbCategory.DateCreated = DateTime.UtcNow;
                    dbCategory.DateUpdated = DateTime.UtcNow;
                    db.Categories.Add(dbCategory);
                    db.SaveChanges();
                    if(CatId.HasValue)
                       return RedirectToAction("Index", new { @CatId = CatId });
                    else
                        return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A category with this name already exists.";
                }
                
            }
            category.parentCategory = GetParentClass();            
            return View("CreateOrEdit", category);
        }

        public ActionResult Edit(int? CatId, int Id = 0)
        {
            if (CatId.HasValue)
            {
                ViewBag.CatId = CatId;
            }
            Category dbCategory = db.Categories.Find(Id);
            if (dbCategory!=null)
            {
                CategoryClass category = new CategoryClass();
                category.InjectClass(dbCategory);
                category.parentCategory = GetParentClass();
                return View("CreateOrEdit", category);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CategoryClass category)
        {
            int? CatId = null;
            if (Request.QueryString["CatId"] != null)
            {
                CatId = Convert.ToInt32(Request.QueryString["CatId"]);
                ViewBag.CatId = CatId;
            }            
            if (ModelState.IsValid)
            {
                Category dbCategory = db.Categories.Find(category.CategoryId);
                if (dbCategory != null)
                {
                    var chkExist = db.Categories.Where(x => x.Name == category.Name && x.ParentId == category.ParentId && x.CategoryId != category.CategoryId && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbCategory.InjectClass(category);
                        if (ReferenceEquals(category.ParentId, null))
                            category.ParentId = 0;
                        dbCategory.CategoryLevel = 2;
                        dbCategory.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        if (CatId.HasValue)
                           return RedirectToAction("Index", new { @CatId = CatId });
                        else
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
            category.parentCategory = GetParentClass();          
            return View("CreateOrEdit", category);
        }

        public ActionResult Delete(int? CatId, int Id = 0)
        {            
            if (CatId.HasValue)
            {
                ViewBag.CatId = CatId;
            }
            Category dbCategory = db.Categories.Find(Id);
            if (dbCategory != null)
            {
                dbCategory.IsDelete = true;
                dbCategory.DateUpdated = DateTime.UtcNow;                             
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index", new { @CatId = CatId });
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        public IEnumerable<ParentCategory> GetParentClass()
        {
            List<ParentCategory> listPC = new List<ParentCategory>();
            var list = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryLevel == 1).OrderBy(x => x.ParentId).OrderBy(x => x.SortOrder).ToList();
            foreach (Category c in list)
            {
                ParentCategory pc = new ParentCategory();
                pc.Id = c.CategoryId;
                pc.Name = GetParents(c.CategoryId);
                pc.SortOrder = c.SortOrder;
                pc.ParentId = c.ParentId;
                listPC.Add(pc);
            }
            return listPC;
        }

        public int GetParentId(int Id = 0)
        {
            int ParentId = 0;
            Category dbCategory = db.Categories.Find(Id);
            if (dbCategory != null)
            {
                ParentId = dbCategory.ParentId;
            }
            return ParentId;
        }

        public string GetParents(int Id = 0)
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
                        strParent = dbCategory.Name + " >>" + strParent; 
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
    }
}
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
    public class BrandController : Controller
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
            
            var brands = db.Brands.Where(i => i.IsDeleted == false).OrderBy(x => x.SortOrder).ToList();
            string nameBrand = searchString;
            if (!ReferenceEquals(nameBrand, null))
                brands = brands.Where(e => e.Name.ToLower().Contains(nameBrand.ToLower())).ToList();

            Type sortByPropType = typeof(Brand).GetProperty(sortColumn).PropertyType;
            List<Brand> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Brand), sortByPropType })
                                        .Invoke(brands, new object[] { brands, sortColumn, sortOrder }) as List<Brand>;

            List<CommonClass> retList = new List<CommonClass>().InjectFrom(sortedList);
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];           
            return View(retList.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult Create()
        {
            return View("CreateOrEdit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CommonClass brand)
        {
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                var chkExist = db.Brands.Where(x => x.Name == brand.Name && x.Id != brand.Id && x.IsDeleted == false).Any();
                if (!chkExist)
                {
                    Brand dbBrand = new Brand();
                    dbBrand.InjectClass(brand);
                    dbBrand.IsDeleted = false;
                    dbBrand.DateCreated = DateTime.UtcNow;
                    dbBrand.DateUpdated = DateTime.UtcNow;
                    db.Brands.Add(dbBrand);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A brand with this name already exists.";
                }
            }
            return View("CreateOrEdit", brand);
        }

        public ActionResult Edit(int Id = 0)
        {
            Brand dbBrand = db.Brands.Find(Id);
            if (dbBrand != null)
            {
                CommonClass brand = new CommonClass();
                brand.InjectClass(dbBrand);
                return View("CreateOrEdit", brand);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CommonClass brand)
        {
            if (ModelState.IsValid)
            {
                Brand dbBrand = db.Brands.Find(brand.Id);
                if (dbBrand != null)
                {
                    var chkExist = db.Brands.Where(x => x.Name == brand.Name && x.Id != brand.Id && x.IsDeleted == false).Any();
                    if (!chkExist)
                    {
                        dbBrand.InjectClass(brand);
                        dbBrand.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "A brand with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", brand);
        }

        public ActionResult Delete(int Id = 0)
        {
            Brand dbBrand = db.Brands.Find(Id);
            if (dbBrand != null)
            {
                dbBrand.IsDeleted = true;
                dbBrand.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }
    }
}
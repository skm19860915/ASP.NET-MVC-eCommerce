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
    public class FitController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index(int? page,string searchString, string sortOrder, string sortColumn = "SortOrder")
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

            var fits = db.Fits.Where(i => i.IsDelete == false).OrderBy(x => x.SortOrder).ToList();
            string nameFit = searchString;
            if (!ReferenceEquals(nameFit, null))
                fits = fits.Where(e => e.Name.ToLower().Contains(nameFit.ToLower())).ToList();

            Type sortByPropType = typeof(Fit).GetProperty(sortColumn).PropertyType;
            List<Fit> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Fit), sortByPropType })
                                        .Invoke(fits, new object[] { fits, sortColumn, sortOrder }) as List<Fit>;

            List<FitClass> retList = new List<FitClass>().InjectFrom(sortedList);
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
        public ActionResult Create(FitClass fit)
        {
            ModelState.Remove("FitId");
            if (ModelState.IsValid)
            {
                var chkExist = db.Fits.Where(x => x.Name == fit.Name && x.FitId != fit.FitId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    Fit dbFit = new Fit();
                    dbFit.InjectClass(fit);
                    dbFit.IsDelete = false;
                    dbFit.DateCreated = DateTime.UtcNow;
                    dbFit.DateUpdated = DateTime.UtcNow;
                    db.Fits.Add(dbFit);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A fit with this name already exists.";
                }
            }
            return View("CreateOrEdit", fit);
        }

        public ActionResult Edit(int Id = 0)
        {
            Fit dbFit = db.Fits.Find(Id);
            if (dbFit != null)
            {
                FitClass fit = new FitClass();
                fit.InjectClass(dbFit);
                return View("CreateOrEdit", fit);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FitClass fit)
        {
            if (ModelState.IsValid)
            {
                Fit dbFit = db.Fits.Find(fit.FitId);
                if (dbFit != null)
                {
                    var chkExist = db.Fits.Where(x => x.Name == fit.Name && x.FitId != fit.FitId && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbFit.InjectClass(fit);
                        dbFit.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "A fit with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", fit);
        }

        public ActionResult Delete(int Id = 0)
        {
            Fit dbFit = db.Fits.Find(Id);
            if (dbFit != null)
            {
                dbFit.IsDelete = true;
                dbFit.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }
    }
}
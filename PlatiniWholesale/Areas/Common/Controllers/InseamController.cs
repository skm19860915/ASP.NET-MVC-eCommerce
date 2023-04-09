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
    public class InseamController : Controller
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

            var inseams = db.Inseams.Where(i => i.IsDelete == false).OrderBy(x => x.SortOrder).ToList();
            string nameInseam = searchString;
            if (!ReferenceEquals(nameInseam, null))
                inseams = inseams.Where(e => e.Name.ToLower().Contains(nameInseam.ToLower())).ToList();

            Type sortByPropType = typeof(Inseam).GetProperty(sortColumn).PropertyType;
            List<Inseam> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Inseam), sortByPropType })
                                        .Invoke(inseams, new object[] { inseams, sortColumn, sortOrder }) as List<Inseam>;

            List<InseamClass> retList = new List<InseamClass>().InjectFrom(sortedList);
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
        public ActionResult Create(InseamClass inseam)
        {
            ModelState.Remove("InseamId");
            if (ModelState.IsValid)
            {
                var chkExist = db.Inseams.Where(x => x.Name == inseam.Name && x.InseamId != inseam.InseamId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    Inseam dbInseam = new Inseam();
                    dbInseam.InjectClass(inseam);
                    dbInseam.IsDelete = false;
                    dbInseam.DateCreated = DateTime.UtcNow;
                    dbInseam.DateUpdated = DateTime.UtcNow;
                    db.Inseams.Add(dbInseam);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "An inseam with this name already exists.";
                }
            }
            return View("CreateOrEdit", inseam);
        }

        public ActionResult Edit(int Id = 0)
        {
            Inseam dbInseam = db.Inseams.Find(Id);
            if (dbInseam != null)
            {
                InseamClass inseam = new InseamClass();
                inseam.InjectClass(dbInseam);
                return View("CreateOrEdit", inseam);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(InseamClass inseam)
        {
            if (ModelState.IsValid)
            {
                Inseam dbInseam = db.Inseams.Find(inseam.InseamId);
                if (dbInseam != null)
                {
                    var chkExist = db.Inseams.Where(x => x.Name == inseam.Name && x.InseamId != inseam.InseamId && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbInseam.InjectClass(inseam);
                        dbInseam.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "An inseam with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", inseam);
        }

        public ActionResult Delete(int Id = 0)
        {
            Inseam dbInseam = db.Inseams.Find(Id);
            if (dbInseam != null)
            {
                dbInseam.IsDelete = true;
                dbInseam.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }
    }
}
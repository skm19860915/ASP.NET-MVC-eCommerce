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
    public class TermController : Controller
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

            var terms = db.Terms.Where(i => i.IsDelete == false).OrderBy(x => x.SortOrder).ToList();
            string nameTerm = searchString;
            if (!ReferenceEquals(nameTerm, null))
                terms = terms.Where(e => e.Name.ToLower().Contains(nameTerm.ToLower())).ToList();

            Type sortByPropType = typeof(Term).GetProperty(sortColumn).PropertyType;
            List<Term> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Term), sortByPropType })
                                        .Invoke(terms, new object[] { terms, sortColumn, sortOrder }) as List<Term>;

            List<CommonClass> retList = sortedList.Select(x => new CommonClass() { Id = x.TermId, Name = x.Name, SortOrder = x.SortOrder, IsActive = x.IsActive }).ToList();
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
        public ActionResult Create(CommonClass term)
        {
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                var chkExist = db.Terms.Where(x => x.Name == term.Name && x.TermId != term.Id && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    Term dbTerm = new Term();
                    dbTerm.InjectClass(term);
                    dbTerm.IsDelete = false;
                    dbTerm.DateCreated = DateTime.UtcNow;
                    dbTerm.DateUpdated = DateTime.UtcNow;
                    db.Terms.Add(dbTerm);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A term with this name already exists.";
                }
            }
            return View("CreateOrEdit", term);
        }

        public ActionResult Edit(int Id = 0)
        {
            Term dbTerm = db.Terms.Find(Id);
            if (dbTerm != null)
            {
                CommonClass term = new CommonClass();
                term.InjectClass(dbTerm);
                return View("CreateOrEdit", term);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CommonClass term)
        {
            if (ModelState.IsValid)
            {
                Term dbTerm = db.Terms.Find(term.Id);
                if (dbTerm != null)
                {
                    var chkExist = db.Terms.Where(x => x.Name == term.Name && x.TermId != term.Id && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbTerm.InjectClass(term);
                        dbTerm.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "A term with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", term);
        }

        public ActionResult Delete(int Id = 0)
        {
            Term dbTerm = db.Terms.Find(Id);
            if (dbTerm != null)
            {
                dbTerm.IsDelete = true;
                dbTerm.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }
    }
}
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
    public class ShipViaController : Controller
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

            var shipvias = db.ShipVias.Where(i => i.IsDelete == false).OrderBy(x => x.SortOrder).ToList();
            string nameShipVia = searchString;
            if (!ReferenceEquals(nameShipVia, null))
                shipvias = shipvias.Where(e => e.Name.ToLower().Contains(nameShipVia.ToLower())).ToList();

            Type sortByPropType = typeof(ShipVia).GetProperty(sortColumn).PropertyType;
            List<ShipVia> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(ShipVia), sortByPropType })
                                        .Invoke(shipvias, new object[] { shipvias, sortColumn, sortOrder }) as List<ShipVia>;

            List<CommonClass> retList = sortedList.Select(x => new CommonClass() { Id = x.ShipViaId, Name = x.Name, SortOrder = x.SortOrder, IsActive = x.IsActive }).ToList();
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
        public ActionResult Create(CommonClass shipvia)
        {
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                var chkExist = db.ShipVias.Where(x => x.Name == shipvia.Name && x.ShipViaId != shipvia.Id && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    ShipVia dbShipVia = new ShipVia();
                    dbShipVia.InjectClass(shipvia);
                    dbShipVia.IsDelete = false;
                    dbShipVia.DateCreated = DateTime.UtcNow;
                    dbShipVia.DateUpdated = DateTime.UtcNow;
                    db.ShipVias.Add(dbShipVia);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A shipping option with this name already exists.";
                }
            }
            return View("CreateOrEdit", shipvia);
        }

        public ActionResult Edit(int Id = 0)
        {
            ShipVia dbShipVia = db.ShipVias.Find(Id);
            if (dbShipVia != null)
            {
                CommonClass shipvia = new CommonClass();
                shipvia.InjectClass(dbShipVia);
                return View("CreateOrEdit", shipvia);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CommonClass shipvia)
        {
            if (ModelState.IsValid)
            {
                ShipVia dbShipVia = db.ShipVias.Find(shipvia.Id);
                if (dbShipVia != null)
                {
                    var chkExist = db.ShipVias.Where(x => x.Name == shipvia.Name && x.ShipViaId != shipvia.Id && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbShipVia.InjectClass(shipvia);
                        dbShipVia.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "A shipping option with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", shipvia);
        }

        public ActionResult Delete(int Id = 0)
        {
            ShipVia dbShipVia = db.ShipVias.Find(Id);
            if (dbShipVia != null)
            {
                dbShipVia.IsDelete = true;
                dbShipVia.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }
    }
}
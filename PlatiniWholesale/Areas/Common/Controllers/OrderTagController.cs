using Platini.DB;
using Platini.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcPaging;

namespace Platini.Areas.Common.Controllers
{
    public class OrderTagController : Controller
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

            var tags = db.OrderTags.Where(i => i.IsDelete == false).ToList();
            string tag = searchString;
            if (!ReferenceEquals(tag, null))
                tags = tags.Where(e => e.Name.ToLower().Contains(tag.ToLower())).ToList();

            Type sortByPropType = typeof(OrderTag).GetProperty(sortColumn).PropertyType;
            List<OrderTag> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(OrderTag), sortByPropType })
                                        .Invoke(tags, new object[] { tags, sortColumn, sortOrder }) as List<OrderTag>;

            List<TagClass> retList = new List<TagClass>().InjectFrom(sortedList);
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
        public ActionResult Create(TagClass tag)
        {
            ModelState.Remove("OrderTagId");
            if (ModelState.IsValid)
            {
                var chkExist = db.OrderTags.Where(x => x.Name == tag.Name && x.OrderTagId != tag.OrderTagId && x.IsDelete == false).Any();
                if (!chkExist)
                {
                    OrderTag dbTag = new OrderTag();
                    dbTag.Name = tag.Name;
                    dbTag.SortOrder = tag.SortOrder.HasValue ? tag.SortOrder.Value : 0;
                    dbTag.IsActive = tag.IsActive.HasValue ? tag.IsActive.Value : false;
                    dbTag.IsDelete = false;
                    dbTag.IsDefault = false;
                    dbTag.DateCreated = dbTag.DateUpdated = DateTime.UtcNow;
                    db.OrderTags.Add(dbTag);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "A tag with this name already exists.";
                }
            }
            return View("CreateOrEdit", tag);
        }

        public ActionResult Edit(int Id = 0)
        {
            OrderTag dbOrderTag = db.OrderTags.Find(Id);
            if (dbOrderTag != null)
            {
                TagClass orderTag = new TagClass();
                orderTag.InjectClass(dbOrderTag);
                return View("CreateOrEdit", orderTag);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TagClass tag)
        {
            if (ModelState.IsValid)
            {
                OrderTag dbBrand = db.OrderTags.Find(tag.OrderTagId);
                if (dbBrand != null)
                {
                    var chkExist = db.OrderTags.Where(x => x.Name == tag.Name && x.OrderTagId != tag.OrderTagId && x.IsDelete == false).Any();
                    if (!chkExist)
                    {
                        dbBrand.InjectClass(tag);
                        dbBrand.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "A tag with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", tag);
        }

        public ActionResult Delete(int Id = 0)
        {
            OrderTag dbTag = db.OrderTags.Find(Id);
            if (dbTag != null)
            {
                dbTag.IsDelete = true;
                dbTag.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }


    }
}

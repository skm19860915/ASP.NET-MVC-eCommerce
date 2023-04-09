using iTextSharp.text;
using iTextSharp.text.pdf;
using Platini.Areas.Common.Models;
using Platini.DB;
using Platini.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Platini.Areas.Common.Controllers
{
    public class ReportController : Controller
    {

        private Entities db = new Entities();


        //
        // GET: /Admin/Report/

        public ActionResult Index()
        {

            return View("Index", "~/Areas/Common/Views/Shared/_Layout.cshtml", null);
        }


        //
        // GET: Report filtered by customer

        public ActionResult Report_SalesByCustomer()
        {
            Platini.Areas.Common.Models.SalesByCustomerReport model = new Platini.Areas.Common.Models.SalesByCustomerReport();
            model.SalesPersonId = 0;
            //model.SalesPersonList = (from sp in db.Accounts
            //                         join r in db.Roles on sp.RoleId equals r.Id
            //                         where r.Role1 == "SalesPerson" && sp.IsActive == true && sp.IsDeleted == false && r.IsActive == true
            //                         select sp).ToList();
            model.SalesPersonList = (from sp in db.Accounts
                                     join r in db.Roles on sp.RoleId equals r.RoleId
                                     where r.RoleName == "SalesPerson" && sp.IsActive == true && sp.IsDelete == false && r.IsActive == true
                                     select new DropDownList { Id = sp.AccountId, Name = sp.FirstName + " " + sp.LastName }).ToList();
            model.OrderStatusId = 0;
            model.OrderSatusList = db.OrderStatus.Where(x => x.IsShown == true).ToList().Select(x => new SelectedListValues()
                {
                    Id = x.OrderStatusId,
                    Value = x.Status,
                    IsSelected = false
                }).ToList();
            model.Accounts =
            db.Accounts.Where(x => x.IsActive == true && x.IsDelete == false).ToList().SelectMany(x =>
                db.Companies.Where(y => y.AccountId == x.AccountId && y.IsActive == true && x.IsDelete == false).DefaultIfEmpty(), (x, y) => new SelectedListValues()
                {
                    Id = x.AccountId,
                    Value = y != null ? (y.Name) : x.FirstName + " " + x.LastName,
                    IsSelected = false
                }).Where(x => !string.IsNullOrEmpty(x.Value)).OrderBy(x => x.Value).ToList();
            return View(model);
        }


        //
        // GET: Report filtered by product

        public ActionResult Report_SalesByProduct()
        {
            Models.InventoryReport model = new Models.InventoryReport();
            var categories = db.Categories.Where(x => x.ParentId == 0 && x.IsActive == true && x.IsDelete == false).ToList();
            model.CategoryList = categories;
            model.SubCategoryList = new List<Category>();
            model.CategoryTypeList = new List<Category>();
            model.Products = new List<Cloth>();

            return View("Report_SalesByProduct", model);
        }

        public ActionResult Report_SalesByTag()
        {
            SBTModel model = new SBTModel();
            model.SalesPersons = db.Accounts.Where(x => x.IsActive == true && x.IsDelete == false && x.RoleId == (int)RolesEnum.SalesPerson).Select(x => new SelectedListValues() { Id = x.AccountId, Value = x.FirstName + " " + x.LastName, IsSelected = false }).ToList();
            model.OrderStatuses = db.OrderStatus.Where(x => x.IsShown == true).OrderBy(x => x.SortOrder).Select(x => new SelectedListValues() { Id = x.OrderStatusId, Value = x.Status, IsSelected = false }).ToList();
            model.OrderTags = db.OrderTags.Where(x => x.IsActive == true && x.IsDelete == false).OrderBy(x => x.IsDefault).Select(x => new SelectedListValues() { Id = x.OrderTagId, Value = x.Name, IsSelected = false }).ToList();
            return View(model);
        }


        //
        // GET: Report filtered by inventory

        public ActionResult Report_SalesByInventory()
        {
            Models.InventoryReport model = new Models.InventoryReport();
            var categories = db.Categories.Where(x => x.ParentId == 0 && x.IsActive == true && x.IsDelete == false).ToList();
            model.CategoryList = categories;
            model.SubCategoryList = new List<Category>();
            model.CategoryTypeList = new List<Category>();
            model.Products = new List<Cloth>();

            return View("Report_SalesByInventory", model);
        }


        [HttpPost]
        public ActionResult Report_SalesByInventory(Platini.Areas.Common.Models.InventoryReport model)
        {
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {

            }
            model.CategoryList = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            model.SubCategoryList = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == model.CategoryId).ToList().OrderBy(x => x.SortOrder).ToList();
            model.CategoryTypeList = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == model.SubCategoryId).ToList().OrderBy(x => x.SortOrder).ToList();
            return View("Report_SalesByInventory", model);
        }

        public JsonResult UpdateList(string categoryId)
        {
            //List<Cloth> clothes = db.Clothes.Where(x => x.CategoryId.Equals(Id.Value)).ToList();
            if (String.IsNullOrEmpty(categoryId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(categoryId, out id);
            if (isValid == true)
            {
                var result = db.Clothes.Where(x => x.CategoryId == id && x.IsActive == true && x.IsDelete == false).OrderBy(y => y.SortOrder).ToList().Select(z => new SelectListItem { Text = z.StyleNumber, Value = z.ClothesId.ToString() });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult GetList(int pId, int sId, int tId, DateTime? to, DateTime? from)
        {
            from = !from.HasValue ? DateTime.Now.AddDays(-30).Date : from;
            to = !to.HasValue ? DateTime.Now.Date : to;
            var oList = db.Orders.Where(x => x.IsDelete == false && (sId > 0 ? x.StatusId == sId : true) && (tId > 0 ? tId == x.TagId : true) && (x.DateCreated >= from && x.DateCreated <= to)).ToList();
            if (pId > 0)
            {
                var aList = db.CustomerSalesPersons.Where(x => x.SalesPersonId == pId).ToList().Select(x => x.AccountId);
                oList = oList.FindAll(x => aList.Contains(x.AccountId));
            }
            if (oList.Any())
            {
                var cList = new List<SelectedListValues>();
                foreach (var order in oList)
                {
                    var cIds = new List<DB.Cloth>();
                    cIds.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                    cIds.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                    cList.AddRange(cIds.Where(x => x.IsActive == true && x.IsDelete == false && !cList.Any(y => y.Id == x.ClothesId)).Select(x => new SelectedListValues() { Id = x.ClothesId, Value = x.StyleNumber }));
                }
                if (cList.Any())

                    return Json(cList, JsonRequestBehavior.AllowGet);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetProducts(string Ids, string printon)
        {
            if (!string.IsNullOrEmpty(Ids))
            {
                var values = Ids.Trim(',').Split(',').Select(x => (int.Parse(x)));
                ViewBag.PrintMode = !string.IsNullOrEmpty(printon);
                var list = new List<InvRept>();
                var dbClothes = db.Clothes.Where(x => x.IsActive == true && values.Contains(x.ClothesId)).ToList();
                foreach (var item in dbClothes)
                {
                    var newRept = new InvRept();
                    newRept.Id = item.ClothesId;
                    newRept.Style = item.StyleNumber;
                    newRept.Pic = "NO_IMAGE.jpg";
                    if (item.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                        newRept.Pic = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath;
                    var tempDate = new DateTime(1900, 1, 1);
                    newRept.iDate = (item.FutureDeliveryDate == null || item.FutureDeliveryDate == DateTime.MinValue || item.FutureDeliveryDate == tempDate) ? item.DateCreated.Value : item.FutureDeliveryDate.Value;
                    newRept.oQty = item.OriginalQty.HasValue ? item.OriginalQty.Value : 0;
                    newRept.aQty = item.AdjustQty.HasValue ? item.AdjustQty.Value : 0;
                    newRept.cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));

                    var tempList1 = item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).
                        SelectMany(x => x.ClothesScaleSizes.Where(y => y.OrderSizes.Any()), (x, y) => y).
                        SelectMany(y => db.OrderSizes.Where(z => z.ClothesSizeId == y.ClothesScaleSizeId), (y, z) => z);

                    var tempList2 = item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false && x.OrderScales.Any()).
                        Select(x => new { pack = x, quant = x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)) }).
                        SelectMany(y => db.OrderScales.Where(z => z.ClothesScaleId == y.pack.ClothesScaleId), (y, z) => new { oPack = z, y.quant });
                    var masterorderList = new List<Order>();
                    masterorderList.AddRange(tempList1.Select(x => x.Order));
                    masterorderList.AddRange(tempList2.Select(x => x.oPack.Order));
                    masterorderList = masterorderList.Distinct().ToList();
                    newRept.sQty = tempList1.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0) + tempList2.Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                    newRept.Cost = item.ProductCost.HasValue ? item.ProductCost.Value : 0;
                    newRept.aCost = Math.Abs(newRept.aQty * newRept.Cost);
                    newRept.Days = (DateTime.UtcNow - newRept.iDate).Days;
                    if (newRept.cQty + newRept.sQty > 0)
                        newRept.pQty = (newRept.sQty / (newRept.sQty + newRept.cQty)) * 100;
                    else
                        newRept.pQty = 0;
                    newRept.Amount = 0;
                    foreach (var ord in masterorderList)
                    {

                        bool customPrice = item.CustomerItemPrices.Any(x => x.AccountId == ord.AccountId);
                        int qty = tempList1.Where(x => x.OrderId == ord.OrderId).Sum(x => (x.Quantity.HasValue ? x.Quantity.Value : 0)) + tempList2.Where(x => x.oPack.OrderId == ord.OrderId).Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                        decimal sum = qty * (customPrice ? (item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.HasValue ? item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.Value : 0) :
                            item.Price.HasValue ? item.Price.Value : 0);
                        sum = sum - ((ord.Discount.HasValue ? ord.Discount.Value : 0) * sum / 100);
                        newRept.Amount += sum;
                    }
                    newRept.Profit = newRept.Amount - (newRept.sQty * newRept.Cost);
                    list.Add(newRept);
                }
                if (list != null && list.Count > 0 && !string.IsNullOrEmpty(printon))
                {
                    var document = new Document(PageSize.A3, 0, 0, 40, 20);
                    var output = new MemoryStream();
                    var writer = PdfWriter.GetInstance(document, output);
                    string ContentType = "application/pdf";
                    writer.CloseStream = false;
                    document.Open();
                    document = PlatiniWebService.Report_SaleByInventory_Pdfs(document, writer, list);
                    document.Close();
                    byte[] pdfBytes = new byte[output.Position];
                    output.Position = 0;
                    output.Read(pdfBytes, 0, pdfBytes.Length);
                    Response.AppendHeader("Content-Disposition", "inline;filename=SalesByProduct.pdf");
                    return File(pdfBytes, ContentType);
                }
                return PartialView("ReportGrid", list);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }



        public ActionResult FilterRecords(string Type, int Id, string printon)
        {
            if (!string.IsNullOrEmpty(Type) && Id > 0)
            {
                ViewBag.PrintMode = !string.IsNullOrEmpty(printon);
                var values = new List<int>();
                Type = Type.Trim().ToLower();
                switch (Type)
                {
                    case "c":
                        values.AddRange(db.Categories.Where(x => x.ParentId == Id && x.IsActive == true && x.IsDelete == false).ToList().SelectMany(
                            x => db.Categories.Where(y => y.ParentId == x.CategoryId && y.IsActive == true && y.IsDelete == false), (x, y) => y.CategoryId));
                        break;
                    case "s":
                        values.AddRange(db.Categories.Where(x => x.ParentId == Id && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.CategoryId));
                        break;
                    case "t":
                        values.Add(Id);
                        break;
                    default:
                        return Json("", JsonRequestBehavior.AllowGet);
                }
                var list = new List<InvRept>();
                var dbClothes = db.Clothes.Where(x => x.IsActive == true && values.Contains(x.CategoryId)).ToList();
                foreach (var item in dbClothes)
                {
                    var newRept = new InvRept();
                    newRept.Id = item.ClothesId;
                    newRept.Style = item.StyleNumber;
                    newRept.Pic = "NO_IMAGE.jpg";
                    if (item.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                        newRept.Pic = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath;
                    var tempDate = new DateTime(1900, 1, 1);
                    newRept.iDate = (item.FutureDeliveryDate == null || item.FutureDeliveryDate == DateTime.MinValue || item.FutureDeliveryDate == tempDate) ? (item.DateCreated.HasValue ? item.DateCreated.Value : DateTime.MinValue) : item.FutureDeliveryDate.Value;
                    newRept.oQty = item.OriginalQty.HasValue ? item.OriginalQty.Value : 0;
                    newRept.aQty = item.AdjustQty.HasValue ? item.AdjustQty.Value : 0;
                    newRept.cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));

                    var tempList1 = item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).
                        SelectMany(x => x.ClothesScaleSizes.Where(y => y.OrderSizes.Any()), (x, y) => y).
                        SelectMany(y => db.OrderSizes.Where(z => z.ClothesSizeId == y.ClothesScaleSizeId), (y, z) => z);

                    var tempList2 = item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false && x.OrderScales.Any()).
                        Select(x => new { pack = x, quant = x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)) }).
                        SelectMany(y => db.OrderScales.Where(z => z.ClothesScaleId == y.pack.ClothesScaleId), (y, z) => new { oPack = z, y.quant });
                    var masterorderList = new List<Order>();
                    masterorderList.AddRange(tempList1.Select(x => x.Order));
                    masterorderList.AddRange(tempList2.Select(x => x.oPack.Order));
                    masterorderList = masterorderList.Distinct().ToList();
                    newRept.sQty = tempList1.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0) + tempList2.Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                    newRept.Cost = item.ProductCost.HasValue ? item.ProductCost.Value : 0;
                    newRept.aCost = Math.Abs(newRept.aQty * newRept.Cost);
                    newRept.Days = (DateTime.UtcNow - newRept.iDate).Days;
                    if (newRept.cQty + newRept.sQty > 0)
                        newRept.pQty = (newRept.sQty / (newRept.sQty + newRept.cQty)) * 100;
                    else
                        newRept.pQty = 0;
                    newRept.Amount = 0;
                    foreach (var ord in masterorderList)
                    {

                        bool customPrice = item.CustomerItemPrices.Any(x => x.AccountId == ord.AccountId);
                        int qty = tempList1.Where(x => x.OrderId == ord.OrderId).Sum(x => (x.Quantity.HasValue ? x.Quantity.Value : 0)) + tempList2.Where(x => x.oPack.OrderId == ord.OrderId).Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                        decimal sum = qty * (customPrice ? (item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.HasValue ? item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.Value : 0) :
                            item.Price.HasValue ? item.Price.Value : 0);
                        sum = sum - ((ord.Discount.HasValue ? ord.Discount.Value : 0) * sum / 100);
                        newRept.Amount += sum;
                    }
                    newRept.Profit = newRept.Amount - (newRept.sQty * newRept.Cost);
                    list.Add(newRept);
                }
                if (list != null && list.Count > 0 && !string.IsNullOrEmpty(printon))
                {
                    var document = new Document(PageSize.A3, 0, 0, 40, 20);
                    var output = new MemoryStream();
                    var writer = PdfWriter.GetInstance(document, output);
                    string ContentType = "application/pdf";
                    writer.CloseStream = false;
                    document.Open();
                    document = PlatiniWebService.Report_SaleByInventory_Pdfs(document, writer, list);
                    document.Close();
                    byte[] pdfBytes = new byte[output.Position];
                    output.Position = 0;
                    output.Read(pdfBytes, 0, pdfBytes.Length);
                    Response.AppendHeader("Content-Disposition", "inline;filename=SalesByProduct.pdf");
                    return File(pdfBytes, ContentType);
                }
                return PartialView("ReportGrid", list);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult RetrieveProducts(string Ids, DateTime to, DateTime from, string printon)
        {
            if (!string.IsNullOrEmpty(Ids))
            {
                var values = Ids.Trim(',').Split(',').Select(x => (int.Parse(x)));
                var list = new List<InvRept>();
                ViewBag.PrintMode = !string.IsNullOrEmpty(printon);
                var dbClothes = db.Clothes.Where(x => x.IsActive == true && values.Contains(x.ClothesId)).ToList();
                foreach (var item in dbClothes)
                {
                    var newRept = new InvRept();
                    newRept.Id = item.ClothesId;
                    newRept.Style = item.StyleNumber;
                    newRept.Pic = "NO_IMAGE.jpg";
                    if (item.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                        newRept.Pic = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath;
                    var tempDate = new DateTime(1900, 1, 1);
                    newRept.iDate = (item.FutureDeliveryDate == null || item.FutureDeliveryDate == DateTime.MinValue || item.FutureDeliveryDate == tempDate) ? item.DateCreated.Value : item.FutureDeliveryDate.Value;
                    newRept.oQty = item.OriginalQty.HasValue ? item.OriginalQty.Value : 0;
                    newRept.cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));

                    var tempList1 = item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).
                        SelectMany(x => x.ClothesScaleSizes.Where(y => y.OrderSizes.Any()), (x, y) => y).
                        SelectMany(y => db.OrderSizes.Where(z => z.ClothesSizeId == y.ClothesScaleSizeId), (y, z) => z);

                    var tempList2 = item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false && x.OrderScales.Any()).
                        Select(x => new { pack = x, quant = x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)) }).
                        SelectMany(y => db.OrderScales.Where(z => z.ClothesScaleId == y.pack.ClothesScaleId), (y, z) => new { oPack = z, y.quant });
                    var masterorderList = new List<Order>();
                    masterorderList.AddRange(tempList1.Select(x => x.Order));
                    masterorderList.AddRange(tempList2.Select(x => x.oPack.Order));
                    masterorderList = masterorderList.Distinct().Where(x => x.DateCreated >= from && x.DateCreated <= to).ToList();
                    newRept.Cost = item.ProductCost.HasValue ? item.ProductCost.Value : 0;
                    newRept.Total = newRept.cQty * (item.Price.HasValue ? item.Price.Value : 0);
                    newRept.Days = (DateTime.UtcNow - newRept.iDate).Days;
                    newRept.sQty = 0;
                    newRept.Amount = 0;
                    foreach (var ord in masterorderList)
                    {

                        bool customPrice = item.CustomerItemPrices.Any(x => x.AccountId == ord.AccountId);
                        int qty = tempList1.Where(x => x.OrderId == ord.OrderId).Sum(x => (x.Quantity.HasValue ? x.Quantity.Value : 0)) + tempList2.Where(x => x.oPack.OrderId == ord.OrderId).Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                        decimal sum = qty * (customPrice ? (item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.HasValue ? item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.Value : 0) :
                            item.Price.HasValue ? item.Price.Value : 0);
                        sum = sum - ((ord.Discount.HasValue ? ord.Discount.Value : 0) * sum / 100);
                        newRept.Amount += sum;
                        newRept.sQty += qty;
                    }
                    if (newRept.cQty + newRept.sQty > 0)
                        newRept.pQty = (newRept.sQty / (newRept.sQty + newRept.cQty)) * 100;
                    else
                        newRept.pQty = 0;
                    newRept.Profit = newRept.Amount - (newRept.sQty * newRept.Cost);
                    list.Add(newRept);
                }
                if (list != null && list.Count > 0 && !string.IsNullOrEmpty(printon))
                {
                    var document = new Document(PageSize.A3, 0, 0, 40, 20);
                    var output = new MemoryStream();
                    var writer = PdfWriter.GetInstance(document, output);
                    string ContentType = "application/pdf";
                    writer.CloseStream = false;
                    document.Open();
                    document = PlatiniWebService.Report_SaleByProduct_Pdfs(document, writer, list);
                    document.Close();
                    byte[] pdfBytes = new byte[output.Position];
                    output.Position = 0;
                    output.Read(pdfBytes, 0, pdfBytes.Length);
                    Response.AppendHeader("Content-Disposition", "inline;filename=SalesByProduct.pdf");
                    return File(pdfBytes, ContentType);
                }
                return PartialView("ReportGrid3", list);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult ShowRecords(string Type, int Id, DateTime to, DateTime from, string printon)
        {
            if (!string.IsNullOrEmpty(Type) && Id > 0)
            {
                ViewBag.PrintMode = !string.IsNullOrEmpty(printon);
                var values = new List<int>();
                Type = Type.Trim().ToLower();
                switch (Type)
                {
                    case "c":
                        values.AddRange(db.Categories.Where(x => x.ParentId == Id && x.IsActive == true && x.IsDelete == false).ToList().SelectMany(
                            x => db.Categories.Where(y => y.ParentId == x.CategoryId && y.IsActive == true && y.IsDelete == false), (x, y) => y.CategoryId));
                        break;
                    case "s":
                        values.AddRange(db.Categories.Where(x => x.ParentId == Id && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.CategoryId));
                        break;
                    case "t":
                        values.Add(Id);
                        break;
                    default:
                        return Json("", JsonRequestBehavior.AllowGet);
                }
                var list = new List<InvRept>();
                var dbClothes = db.Clothes.Where(x => x.IsActive == true && values.Contains(x.CategoryId)).ToList();
                foreach (var item in dbClothes)
                {
                    var newRept = new InvRept();
                    newRept.Id = item.ClothesId;
                    newRept.Style = item.StyleNumber;
                    newRept.Pic = "NO_IMAGE.jpg";
                    if (item.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                        newRept.Pic = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath;
                    var tempDate = new DateTime(1900, 1, 1);
                    newRept.iDate = (item.FutureDeliveryDate == null || item.FutureDeliveryDate == DateTime.MinValue && item.FutureDeliveryDate == tempDate) ? item.DateCreated.Value : item.FutureDeliveryDate.Value;
                    newRept.oQty = item.OriginalQty.HasValue ? item.OriginalQty.Value : 0;
                    newRept.cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));

                    var tempList1 = item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).
                        SelectMany(x => x.ClothesScaleSizes.Where(y => y.OrderSizes.Any()), (x, y) => y).
                        SelectMany(y => db.OrderSizes.Where(z => z.ClothesSizeId == y.ClothesScaleSizeId), (y, z) => z);

                    var tempList2 = item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false && x.OrderScales.Any()).
                        Select(x => new { pack = x, quant = x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)) }).
                        SelectMany(y => db.OrderScales.Where(z => z.ClothesScaleId == y.pack.ClothesScaleId), (y, z) => new { oPack = z, y.quant });
                    var masterorderList = new List<Order>();
                    masterorderList.AddRange(tempList1.Select(x => x.Order));
                    masterorderList.AddRange(tempList2.Select(x => x.oPack.Order));
                    masterorderList = masterorderList.Distinct().Where(x => x.DateCreated >= from && x.DateCreated <= to).ToList();
                    newRept.Cost = item.ProductCost.HasValue ? item.ProductCost.Value : 0;
                    newRept.Total = newRept.cQty * (item.Price.HasValue ? item.Price.Value : 0);
                    newRept.Days = (DateTime.UtcNow - newRept.iDate).Days;
                    newRept.sQty = 0;
                    newRept.Amount = 0;
                    foreach (var ord in masterorderList)
                    {

                        bool customPrice = item.CustomerItemPrices.Any(x => x.AccountId == ord.AccountId);
                        int qty = tempList1.Where(x => x.OrderId == ord.OrderId).Sum(x => (x.Quantity.HasValue ? x.Quantity.Value : 0)) + tempList2.Where(x => x.oPack.OrderId == ord.OrderId).Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                        decimal sum = qty * (customPrice ? (item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.HasValue ? item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.Value : 0) :
                            item.Price.HasValue ? item.Price.Value : 0);
                        sum = sum - ((ord.Discount.HasValue ? ord.Discount.Value : 0) * sum / 100);
                        newRept.Amount += sum;
                        newRept.sQty += qty;
                    }
                    if (newRept.cQty + newRept.sQty > 0)
                        newRept.pQty = (newRept.sQty / (newRept.sQty + newRept.cQty)) * 100;
                    else
                        newRept.pQty = 0;
                    newRept.Profit = newRept.Amount - (newRept.sQty * newRept.Cost);
                    list.Add(newRept);
                }
                if (list != null && list.Count > 0 && !string.IsNullOrEmpty(printon))
                {
                    var document = new Document(PageSize.A3, 0, 0, 40, 20);
                    var output = new MemoryStream();
                    var writer = PdfWriter.GetInstance(document, output);
                    string ContentType = "application/pdf";
                    writer.CloseStream = false;
                    document.Open();
                    document = PlatiniWebService.Report_SaleByProduct_Pdfs(document, writer, list);
                    document.Close();
                    byte[] pdfBytes = new byte[output.Position];
                    output.Position = 0;
                    output.Read(pdfBytes, 0, pdfBytes.Length);
                    Response.AppendHeader("Content-Disposition", "inline;filename=SalesByProduct.pdf");
                    return File(pdfBytes, ContentType);
                }
                return PartialView("ReportGrid3", list);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }


        public ActionResult TagRecords(int pId, int sId, int tId, string Ids, DateTime? from, DateTime? to, string printon)
        {
            from = !from.HasValue ? DateTime.Now.AddDays(-30).Date : from;
            to = !to.HasValue ? DateTime.Now.Date : to;
            ViewBag.PrintMode = !string.IsNullOrEmpty(printon);
            var oList = db.Orders.Where(x => x.IsDelete == false && (sId > 0 ? x.StatusId == sId : true) && (tId > 0 ? tId == x.TagId : true) && (x.DateCreated >= from && x.DateCreated <= to)).ToList();
            if (pId > 0)
            {
                var aList = db.CustomerSalesPersons.Where(x => x.SalesPersonId == pId).ToList().Select(x => x.AccountId);
                oList = oList.FindAll(x => aList.Contains(x.AccountId));
            }
            if (oList.Any())
            {
                var cList = new List<int>();
                foreach (var order in oList)
                {
                    var cIds = new List<DB.Cloth>();
                    cIds.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                    cIds.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                    cList.AddRange(cIds.Where(x => x.IsActive == true && x.IsDelete == false).Distinct().Select(x => x.ClothesId));
                }
                cList = cList.Distinct().ToList();
                if (!string.IsNullOrEmpty(Ids))
                {
                    var values = Ids.Trim(',').Split(',').Select(x => (int.Parse(x)));
                    cList = cList.FindAll(x => values.Contains(x));
                }
                if (cList.Any())
                {
                    var list = new List<InvRept>();
                    var dbClothes = db.Clothes.Where(x => x.IsActive == true && cList.Contains(x.ClothesId)).ToList();
                    foreach (var item in dbClothes)
                    {
                        var newRept = new InvRept();
                        newRept.Id = item.ClothesId;
                        newRept.Style = item.StyleNumber;
                        newRept.Pic = "NO_IMAGE.jpg";
                        if (item.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                            newRept.Pic = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath;
                        var tempDate = new DateTime(1900, 1, 1);
                        newRept.iDate = (item.FutureDeliveryDate == null || item.FutureDeliveryDate == DateTime.MinValue && item.FutureDeliveryDate == tempDate) ? item.DateCreated.Value : item.FutureDeliveryDate.Value;
                        newRept.oQty = item.OriginalQty.HasValue ? item.OriginalQty.Value : 0;
                        newRept.cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                            + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));

                        //var tempList1 = item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).
                        //SelectMany(x => x.ClothesScaleSizes.Where(y => y.OrderSizes.Any()), (x, y) => y).
                        //SelectMany(y => db.OrderSizes.Where(z => z.ClothesSizeId == y.ClothesScaleSizeId), (y, z) => z);

                        //var tempList2 = item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false && x.OrderScales.Any()).
                        //    Select(x => new { pack = x, quant = x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)) }).
                        //    SelectMany(y => db.OrderScales.Where(z => z.ClothesScaleId == y.pack.ClothesScaleId), (y, z) => new { oPack = z, y.quant });


                        newRept.Cost = item.ProductCost.HasValue ? item.ProductCost.Value : 0;
                        newRept.Total = newRept.cQty * (item.Price.HasValue ? item.Price.Value : 0);
                        newRept.Days = (DateTime.UtcNow - newRept.iDate).Days;
                        newRept.sQty = 0;
                        newRept.Amount = 0;
                        foreach (var ord in oList)
                        {

                            bool customPrice = item.CustomerItemPrices.Any(x => x.AccountId == ord.AccountId);
                            int qty = 0;
                            qty = ord.OrderSizes.Where(x => item.ClothesScales.Any(y => y.ClothesScaleSizes.Where(z => z.ClothesScaleSizeId == x.ClothesSizeId).Count() > 0)).Sum(x => (x.Quantity.HasValue ? x.Quantity.Value : 0));
                            qty += ord.OrderScales.Where(x => item.ClothesScales.Any(y => y.ClothesScaleId == x.ClothesScaleId)).Sum(x => (x.ClothesScale.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))) * (x.Quantity.HasValue ? x.Quantity.Value : 0));

                            decimal sum = qty * (customPrice ? (item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.HasValue ? item.CustomerItemPrices.FirstOrDefault(x => x.AccountId == ord.AccountId).Price.Value : 0) :
                                item.Price.HasValue ? item.Price.Value : 0);
                            sum = sum - ((ord.Discount.HasValue ? ord.Discount.Value : 0) * sum / 100);
                            newRept.Amount += sum;
                            newRept.sQty += qty;
                        }
                        if (newRept.cQty + newRept.sQty > 0)
                            newRept.pQty = (newRept.sQty / (newRept.sQty + newRept.cQty)) * 100;
                        else
                            newRept.pQty = 0;
                        newRept.Profit = newRept.Amount - (newRept.sQty * newRept.Cost);
                        list.Add(newRept);
                    }
                    if (list.Count > 0 && !string.IsNullOrEmpty(printon))
                    {
                        var document = new Document(PageSize.A3, 0, 0, 40, 20);
                        var output = new MemoryStream();
                        var writer = PdfWriter.GetInstance(document, output);
                        string ContentType = "application/pdf";
                        writer.CloseStream = false;
                        document.Open();
                        document = PlatiniWebService.Report_SaleByProduct_Pdfs(document, writer, list);
                        document.Close();
                        byte[] pdfBytes = new byte[output.Position];
                        output.Position = 0;
                        output.Read(pdfBytes, 0, pdfBytes.Length);
                        Response.AppendHeader("Content-Disposition", "inline;filename=SalesByTag.pdf");
                        return File(pdfBytes, ContentType);
                    }
                    return PartialView("ReportGrid3", list);
                }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult GetCustomerBySalesPerson(int salesPersonId)
        {
            if (salesPersonId > 0)
            {
                var result = db.CustomerSalesPersons.Where(x => x.SalesPersonId == salesPersonId).ToList().SelectMany(x => db.Accounts.Where(y => y.IsActive == true && y.IsDelete == false && y.AccountId == x.AccountId), (x, y) => (y)).
                    SelectMany(x =>
                db.Companies.Where(y => y.AccountId == x.AccountId && y.IsActive == true && x.IsDelete == false).DefaultIfEmpty(), (x, y) => new SelectedListValues()
                {
                    Id = x.AccountId,
                    Value = y != null ? (y.Name) : x.FirstName + " " + x.LastName,
                    IsSelected = false
                }).Where(x => !string.IsNullOrEmpty(x.Value)).OrderBy(x => x.Value).ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var result = db.Accounts.Where(x => x.IsActive == true && x.IsDelete == false).ToList().SelectMany(x =>
                db.Companies.Where(y => y.AccountId == x.AccountId && y.IsActive == true && x.IsDelete == false).DefaultIfEmpty(), (x, y) => new SelectedListValues()
                {
                    Id = x.AccountId,
                    Value = y != null ? (y.Name) : x.FirstName + " " + x.LastName,
                    IsSelected = false
                }).Where(x => !string.IsNullOrEmpty(x.Value)).OrderBy(x => x.Value).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult GetSalesData(DateTime to, DateTime From, int StatusId, int SPId, string Ids, string printon)
        {
            var status = db.OrderStatus.FirstOrDefault(x => x.OrderStatusId == StatusId);
            bool isPending = status != null && status.Status.ToLower() == "pending";
            string SPName = "";
            var accounts = new List<int>();
            ViewBag.PrintMode = !string.IsNullOrEmpty(printon);
            if (!string.IsNullOrEmpty(Ids))
            {
                accounts.AddRange(Ids.Trim(',').Split(',').Select(x => int.Parse(x)));
            }
            if (SPId > 0)
            {
                var sp = db.Accounts.FirstOrDefault(x => x.AccountId == SPId && x.IsActive == true && x.IsDelete == false);
                SPName = sp != null ? sp.FirstName + " " + sp.LastName : "";
            }
            var masterList = db.Orders.Where(x => x.StatusId == StatusId && (isPending ? (x.DateCreated.Value <= to && x.DateCreated.Value >= From) : (x.SubmittedOn.Value <= to && x.SubmittedOn.Value >= From)) && (accounts.Count > 0 ? (accounts.Contains(x.AccountId)) : true))
                .ToList().SelectMany(x => db.Accounts.Where(y => y.AccountId == x.AccountId), (x, y) => new { Order = x, Name = y.FirstName + " " + y.LastName });
            var List = new List<SBCRept>();
            foreach (var item in masterList)
            {
                var rptItem = new SBCRept();
                rptItem.Id = item.Order.OrderId;
                rptItem.iDate = item.Order.DateCreated.HasValue ? item.Order.DateCreated.Value : DateTime.MinValue;
                rptItem.Name = item.Name;
                rptItem.SalesPerson = SPName;
                if (string.IsNullOrEmpty(rptItem.SalesPerson))
                {
                    int Id = (db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == item.Order.AccountId) != null ? db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == item.Order.AccountId).CustomerSalesPersonId : 0);
                    var sp = db.Accounts.FirstOrDefault(x => x.AccountId == Id && x.IsActive == true && x.IsDelete == false);
                    rptItem.SalesPerson = sp != null ? sp.FirstName + " " + sp.LastName : "";
                }
                var tempList1 = item.Order.OrderScales.SelectMany(x => db.Clothes.Where(y => y.ClothesId == x.ClothesId), (x, y) => new
                    {
                        OrdQuant = (x.Quantity * (x.ClothesScale.ClothesScaleSizes.Sum(z => z.Quantity))),
                        Cloth = y
                    }).ToList();
                var tempList2 = item.Order.OrderSizes.SelectMany(x => db.Clothes.Where(y => y.ClothesId == x.ClothesId), (x, y) => new
                {
                    OrdQuant = x.Quantity,
                    Cloth = y
                }).ToList();
                tempList1.AddRange(tempList2);
                var total = tempList1.SelectMany(x => db.CustomerItemPrices.Where(y => y.AccountId == item.Order.AccountId && y.ClothesId == x.Cloth.ClothesId).DefaultIfEmpty(), (x, y) =>
                    new { Quant = x.OrdQuant, Price = y != null ? y.Price : x.Cloth.Price }).Sum(x => x.Price * x.Quant);
                var cost = tempList1.Sum(x => x.OrdQuant * x.Cloth.ProductCost);
                rptItem.Amount = total.HasValue ? total.Value : 0;
                rptItem.Cost = cost.HasValue ? cost.Value : 0;
                rptItem.Profit = rptItem.Amount - rptItem.Cost;
                List.Add(rptItem);
            }
            if (List != null && List.Count > 0 && !string.IsNullOrEmpty(printon))
            {
                var document = new Document(PageSize.A3, 0, 0, 40, 20);
                var output = new MemoryStream();
                var writer = PdfWriter.GetInstance(document, output);
                string ContentType = "application/pdf";
                writer.CloseStream = false;
                document.Open();
                document = PlatiniWebService.Report_SaleByCustomer_Pdfs(document, writer, List);
                document.Close();
                byte[] pdfBytes = new byte[output.Position];
                output.Position = 0;
                output.Read(pdfBytes, 0, pdfBytes.Length);
                Response.AppendHeader("Content-Disposition", "inline;filename=SalesByProduct.pdf");
                return File(pdfBytes, ContentType);
            }
            return PartialView("ReportGrid2", List);
        }

        public ActionResult PrintMe(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                string URL = "";
                var parts = url.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i == 0)
                        URL += parts[i];
                    else if (i == 1)
                        URL += "?" + parts[i].Replace(";", ",");
                    else
                        URL += "&" + parts[i].Replace(";", ",");
                }
                URL += "&printon=1";
                return Redirect(URL);
                //byte[] toPdf = PlatiniWebService.CreatePdf(ConfigurationManager.AppSettings["BaseUrl"] + "Common/Report/" + URL);
                //string ContentType = "application/pdf";
                //string FileName = "Cart.pdf";
                //Response.AppendHeader("Content-Disposition", "inline;filename=" + FileName);
                //return File(toPdf, ContentType);
            }
            return RedirectToAction("Index");
        }

        public void Test(string path)
        {
            new System.Web.Helpers.WebImage(Server.MapPath(path)).Resize(43, 64, false, true).Crop(1, 1).Write();
        }

    }
}

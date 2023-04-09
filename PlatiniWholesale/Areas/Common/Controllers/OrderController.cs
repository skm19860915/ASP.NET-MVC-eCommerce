using Platini.Areas.Common.Models;
using Platini.DB;
using Platini.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System.Data.SqlClient;
using MvcPaging;
using System.Text;
using ShipWSSample;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Hosting;
using System.Text.RegularExpressions;

namespace Platini.Areas.Common.Controllers
{
    public class OrderController : Controller
    {


        //QuickBookItems qb = new QuickBookItems();

        string Display = string.Empty;
        string Prepack = string.Empty;
        string size = string.Empty;

        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);
        private string filePath = System.Configuration.ConfigurationManager.AppSettings["UPSLabel"];

        public ActionResult Index(int? Id, int? page, int? selectedSalesPerson, string searchText, int? TagId)
        {
            ViewBag.isCustomer = SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower();
            bool cWH = ViewBag.isWarehouse = SiteIdentity.Roles.ToLower() == RolesEnum.Warehouse.ToString().ToLower();
            ViewBag.isSalesPerson = SiteIdentity.Roles.ToLower() == RolesEnum.SalesPerson.ToString().ToLower();
            ViewBag.UpsId = 0;
            var ship = db.ShipVias.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.Name.ToLower() == "ups");
            if (ship != null)
                ViewBag.UpsId = ship.ShipViaId;
            ViewBag.StatusId = 1;
            if (ViewBag.isWareHouse)
                ViewBag.StatusId = 2;
            if (Id.HasValue)
            {
                if (Id.Value > 0)
                    ViewBag.StatusId = Id.Value;
                else
                    ViewBag.StatusId = 0;
            }
            int salesPerson = 0;
            if (selectedSalesPerson.HasValue)
            {
                salesPerson = selectedSalesPerson.Value;
                ViewBag.selectedSalesPerson = salesPerson;
            }
            else if (ViewBag.isSalesPerson)
            {
                salesPerson = int.Parse(SiteIdentity.UserId);
            }

            string search = string.Empty;
            if (!string.IsNullOrEmpty(searchText))
            {
                search = searchText;
            }
            ViewBag.searchText = search;
            ViewBag.selectedTag = TagId;
            int roleId = SiteConfiguration.GetRoleId(SiteIdentity.Roles);
            int customerId = 0;
            if (roleId == (int)RolesEnum.Customer)
                customerId = int.Parse(SiteIdentity.UserId);
            var retList = db.Orders.Where(x => x.IsDelete == false && (Id.HasValue ? (Id.Value > 0 ? x.StatusId == Id : true) : (cWH ? x.StatusId == 2 : x.StatusId == 1)) && (customerId > 0 ? (x.AccountId == customerId) : true)).ToList();
            if (Id.HasValue ? Id.Value == 6 : false)
                retList = retList.FindAll(x => !x.Order1.Any());
            if (salesPerson > 0)
            {
                var accountIds = db.CustomerSalesPersons.Where(x => x.SalesPersonId == salesPerson).ToList().Select(x => x.AccountId);
                retList = retList.FindAll(x => accountIds.Contains(x.AccountId));
            }
            if (TagId.HasValue)
            {
                retList = retList.FindAll(x => x.TagId == TagId.Value);
            }
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                var accountIds = new List<int>();
                accountIds.AddRange(db.Accounts.Where(x => (x.FirstName.ToLower().Contains(search) || x.LastName.ToLower().Contains(search)) && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.AccountId));
                accountIds.AddRange(db.Companies.Where(x => x.Name.ToLower().Contains(search) && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.AccountId));
                accountIds.AddRange(db.Communications.Where(x => x.Phone.Contains(search) && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.AccountId.HasValue ? x.AccountId.Value : 0));
                accountIds = accountIds.Distinct().ToList();
                var orderIds = new List<Guid>();
                orderIds.AddRange(db.Clothes.Where(x => x.StyleNumber.ToLower().Contains(searchText) && x.IsActive == true && x.IsDelete == false).ToList().
                    SelectMany(c => db.ClothesScales.Where(x => x.ClothesId == c.ClothesId && x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false), (c, x) => new { x.ClothesScaleId }).ToList().
                    SelectMany(c => db.OrderScales.Where(x => x.ClothesScaleId == c.ClothesScaleId), (c, x) => new { x.OrderId }).ToList().Select(x => x.OrderId));
                orderIds.AddRange(db.Clothes.Where(x => x.StyleNumber.ToLower().Contains(searchText) && x.IsActive == true && x.IsDelete == false).ToList().
                    SelectMany(c => db.ClothesScales.Where(x => x.ClothesId == c.ClothesId && x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false), (c, x) => new { x.ClothesScaleId }).ToList().
                    SelectMany(c => db.ClothesScaleSizes.Where(x => x.ClothesScaleId == c.ClothesScaleId && x.IsActive == true && x.IsDelete == false), (c, x) => new { x.ClothesScaleSizeId }).
                    SelectMany(c => db.OrderSizes.Where(x => x.ClothesSizeId == c.ClothesScaleSizeId), (c, x) => new { x.OrderId }).ToList().Select(x => x.OrderId));
                retList = retList.FindAll(x => (accountIds.Contains(x.AccountId)) || (orderIds.Contains(x.OrderId)) || (x.OrderNumber == search));
            }
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];

            if (ViewBag.StatusId == 6)
            {
                retList = retList.OrderByDescending(x => x.PackedOn).ThenBy(x => x.OrderNumber).ToList();
            }
            else if (ViewBag.StatusId == 3)
                retList = retList.OrderByDescending(x => x.ShippedOn).ThenBy(x => x.OrderNumber).ToList();
            else if (ViewBag.StatusId == 7)
                retList = retList.OrderByDescending(x => x.CreatedOn).ThenBy(x => x.OrderNumber).ToList();
            else
                retList = retList.OrderByDescending(x => x.SubmittedOn).ThenBy(x => x.OrderNumber).ToList();

            if (TempData["OpenUPS"] != null)
                ViewBag.OpenUPS = (Guid)TempData["OpenUPS"];
            else
                ViewBag.OpenUPS = Guid.Empty;

            var files = new DirectoryInfo(Server.MapPath(filePath)).GetFiles("*.Gif");
            foreach (var file in files)
            {
                if (DateTime.UtcNow - file.CreationTimeUtc > TimeSpan.FromDays(30))
                {
                    System.IO.File.Delete(file.FullName);
                }
            }

            if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower())
                return View("Index", "~/Areas/Common/Views/Shared/_Layout2.cshtml", retList.ToPagedList(currentPageIndex, 100));
            return View(retList.ToPagedList(currentPageIndex, 100));
        }

        public ActionResult OrderHeader(int Id, int? selectedSalesPerson, string searchText, int? TagId)
        {
            var model = new OrderHeader();
            ViewBag.isCustomer = SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower();
            ViewBag.isWarehouse = SiteIdentity.Roles.ToLower() == RolesEnum.Warehouse.ToString().ToLower();
            ViewBag.isSalesPerson = SiteIdentity.Roles.ToLower() == RolesEnum.SalesPerson.ToString().ToLower();
            ViewBag.StatusId = Id;
            int salesPerson = 0;
            if (selectedSalesPerson.HasValue)
            {
                model.SalesMan = selectedSalesPerson.HasValue ? selectedSalesPerson.Value : 0;
                salesPerson = selectedSalesPerson.Value;
            }
            else if (ViewBag.isSalesPerson)
            {
                salesPerson = int.Parse(SiteIdentity.UserId);
            }
            int roleId = SiteConfiguration.GetRoleId(SiteIdentity.Roles);
            int customerId = 0;
            if (roleId == (int)RolesEnum.Customer)
                customerId = int.Parse(SiteIdentity.UserId);
            var Orders = db.Orders.Where(x => x.IsDelete == false && (customerId > 0 ? (x.AccountId == customerId) : true)).ToList();
            if (salesPerson > 0)
            {
                var accountIds = db.CustomerSalesPersons.Where(x => x.SalesPersonId == salesPerson).ToList().Select(x => x.AccountId);
                Orders = Orders.FindAll(x => accountIds.Contains(x.AccountId));
            }
            if (TagId.HasValue)
            {
                Orders = Orders.FindAll(x => x.TagId == TagId.Value);
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                var accountIds = new List<int>();
                accountIds.AddRange(db.Accounts.Where(x => (x.FirstName.ToLower().Contains(searchText) || x.LastName.ToLower().Contains(searchText)) && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.AccountId));
                accountIds.AddRange(db.Companies.Where(x => x.Name.ToLower().Contains(searchText) && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.AccountId));
                accountIds.AddRange(db.Communications.Where(x => x.Phone.Contains(searchText) && x.IsActive == true && x.IsDelete == false).ToList().Select(x => x.AccountId.HasValue ? x.AccountId.Value : 0));
                accountIds = accountIds.Distinct().ToList();
                var orderIds = new List<Guid>();
                orderIds.AddRange(db.Clothes.Where(x => x.StyleNumber.ToLower().Contains(searchText) && x.IsActive == true && x.IsDelete == false).ToList().
                    SelectMany(c => db.ClothesScales.Where(x => x.ClothesId == c.ClothesId && x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false), (c, x) => new { x.ClothesScaleId }).ToList().
                    SelectMany(c => db.OrderScales.Where(x => x.ClothesScaleId == c.ClothesScaleId), (c, x) => new { x.OrderId }).ToList().Select(x => x.OrderId));
                orderIds.AddRange(db.Clothes.Where(x => x.StyleNumber.ToLower().Contains(searchText) && x.IsActive == true && x.IsDelete == false).ToList().
                    SelectMany(c => db.ClothesScales.Where(x => x.ClothesId == c.ClothesId && x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false), (c, x) => new { x.ClothesScaleId }).ToList().
                    SelectMany(c => db.ClothesScaleSizes.Where(x => x.ClothesScaleId == c.ClothesScaleId && x.IsActive == true && x.IsDelete == false), (c, x) => new { x.ClothesScaleSizeId }).
                    SelectMany(c => db.OrderSizes.Where(x => x.ClothesSizeId == c.ClothesScaleSizeId), (c, x) => new { x.OrderId }).ToList().Select(x => x.OrderId));
                Orders = Orders.FindAll(x => (accountIds.Contains(x.AccountId)) || (orderIds.Contains(x.OrderId)) || (x.OrderNumber == searchText));
            }
            var OrderGroup = Orders.GroupBy(x => x.StatusId).Select(x => new { Id = x.Key, Count = x.Count(z => (z.StatusId == 6 ? !z.Order1.Any() : true)), Sum = x.Where(z => (z.StatusId == 6 ? !z.Order1.Any() : true)).Sum(y => y.FinalAmount) });
            model.Statuses = db.OrderStatus.Where(x => x.IsShown == true).OrderBy(x => x.SortOrder).ToList().SelectMany(y => OrderGroup.Where(x => y.OrderStatusId == x.Id).DefaultIfEmpty(), (y, x) => new SelectedListValues()
            {
                Id = y.OrderStatusId,
                Value = GetName(y.OrderStatusId) + " (" + (x != null ? x.Count : 0) + ")",
                IsSelected = y.OrderStatusId == Id
            }).ToList();
            model.Statuses.Insert(0, new SelectedListValues() { Id = 0, Value = "All", IsSelected = Id == 0 });
            model.GrandTotal = OrderGroup.FirstOrDefault(x => x.Id == Id) != null ? (OrderGroup.FirstOrDefault(x => x.Id == Id).Sum.HasValue ? OrderGroup.FirstOrDefault(x => x.Id == Id).Sum.Value : 0.0m) : 0.0m;
            model.SalesPersons = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson && x.IsActive == true && x.IsDelete == false).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = (selectedSalesPerson.HasValue ? selectedSalesPerson.Value == y.AccountId : false) }).ToList();
            model.Tags = db.OrderTags.Where(x => x.IsActive == true && x.IsDelete == false).ToList().Select(x => new SelectedListValues() { Id = x.OrderTagId, Value = x.Name, IsSelected = TagId.HasValue ? TagId.Value == x.OrderTagId : false }).ToList();
            model.Search = !string.IsNullOrEmpty(searchText) ? searchText : null;
            return PartialView(model);

        }

        [NonAction]
        public string GetName(int Id)
        {
            if (Id == (int)OrderMode.BackOrder)
                return OrderMode.BackOrder.Description();
            if (Id == (int)OrderMode.Completed)
                return OrderMode.Completed.Description();
            if (Id == (int)OrderMode.InProcess)
                return OrderMode.InProcess.Description();
            if (Id == (int)OrderMode.New)
                return OrderMode.New.Description();
            if (Id == (int)OrderMode.Packed)
                return OrderMode.Packed.Description();
            if (Id == (int)OrderMode.Pending)
                return OrderMode.Pending.Description();
            if (Id == (int)OrderMode.Shipped)
                return OrderMode.Shipped.Description();
            return "";
        }

        public ActionResult SubmitOrder(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                int oldstatus = order.StatusId;
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "new");
                if (status != null)
                    StatusId = status.OrderStatusId;
                if (StatusId > 0)
                {
                    order.StatusId = StatusId;
                    order.SubmittedOn = DateTime.UtcNow;
                    order.DateUpdated = DateTime.UtcNow;
                    order.IsSentToQuickBook = false;
                    order.OrderNumber = SiteConfiguration.OrderNumber();
                    if (order.Account != null)
                    {
                        if (order.Account.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false) != null)
                            order.CommunicationId = order.Account.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).CommunicationId;
                        if (order.Account.Addresses.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress) != null)
                            order.AddressId = order.Account.Addresses.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress).AddressId;
                    }
                    db.SaveChanges();
                    if (order.OrderScales.Any())
                    {
                        var scaleList = order.OrderScales.ToList();
                        foreach (var scale in scaleList)
                        {
                            var clothesScale = db.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == scale.ClothesScaleId && x.IsActive == true && x.IsDelete == false);
                            if (clothesScale != null)
                            {
                                var cloth = db.Clothes.Find(clothesScale.ClothesId);
                                clothesScale.InvQty = clothesScale.InvQty - (scale.Quantity.HasValue ? scale.Quantity.Value : 0);                                
                                clothesScale.DateUpdated = DateTime.UtcNow;
                                cloth.OriginalQty -= clothesScale.ClothesScaleSizes.Sum(x => x.Quantity) * (scale.Quantity.HasValue ? scale.Quantity.Value : 0);
                                db.SaveChanges();
                            }
                        }
                    }
                    if (order.OrderSizes.Any())
                    {
                        var sizeList = order.OrderSizes.ToList();
                        foreach (var size in sizeList)
                        {
                            var clothesScale = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleSizeId == size.ClothesSizeId && x.IsActive == true && x.IsDelete == false);
                            var cloth = db.Clothes.Find(size.ClothesId);
                            if (clothesScale != null)
                            {
                                clothesScale.Quantity = clothesScale.Quantity - (size.Quantity.HasValue ? size.Quantity.Value : 0);
                                clothesScale.DateUpdated = DateTime.UtcNow;
                                cloth.OriginalQty -= (size.Quantity.HasValue ? size.Quantity.Value : 0);
                                db.SaveChanges();
                            }
                        }
                    }
                    var customer = db.Accounts.Find(order.AccountId);
                    EmailManager.SendOrderEmailToCustomer(order.OrderId, order.OrderNumber, (customer != null ? customer.Email : ""), order.AccountId.ToString());
                    TempData["PageMessage"] = "Order has been succesfully submitted.";
                    return RedirectToAction("Index", new { @id = oldstatus });
                }
            }
            TempData["PageMessage"] = "No order found.";
            return RedirectToAction("Index");
        }

        public ActionResult MoveOrderToWareHouse(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                int oldstatus = order.StatusId;
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "inprocess");
                if (status != null)
                    StatusId = status.OrderStatusId;
                if (StatusId > 0)
                {
                    order.StatusId = StatusId;
                    order.SubmittedOn = DateTime.UtcNow;
                    order.DateUpdated = DateTime.UtcNow;
                    order.IsSentToQuickBook = false;
                    db.SaveChanges();
                    TempData["PageMessage"] = "The order has succesfully been moved to warehouse.";
                    return RedirectToAction("Index", new { @id = oldstatus });
                }
            }
            TempData["PageMessage"] = "No order found.";
            return RedirectToAction("Index");
        }

        public ActionResult DeleteOrderNoAdjust(Guid Id)
        {
            var delOrder = db.Orders.Find(Id);
            if (delOrder != null)
            {
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                if (status != null)
                    StatusId = status.OrderStatusId;
                if (delOrder.StatusId == StatusId)
                {
                    if (delOrder.OrderScales.Any())
                    {
                        var scaleList = delOrder.OrderScales.ToList();
                        foreach (var scale in scaleList)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "OrderScale";
                            ttd.TableKey = "OrderScaleId";
                            ttd.TableValue = scale.OrderScaleId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.OrderScales.Remove(scale);
                        }
                        db.SaveChanges();

                    }
                    if (delOrder.OrderSizes.Any())
                    {
                        var sizeList = delOrder.OrderSizes.ToList();
                        foreach (var size in sizeList)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "OrderSize";
                            ttd.TableKey = "OrderSizeId";
                            ttd.TableValue = size.OrderSizeId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.OrderSizes.Remove(size);
                        }
                        db.SaveChanges();
                    }
                    if (delOrder.Boxes.Any())
                    {
                        var boxList = delOrder.Boxes.ToList();
                        foreach (var box in boxList)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "Box";
                            ttd.TableKey = "Id";
                            ttd.TableValue = box.OrderId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.Boxes.Remove(box);
                        }
                    }
                    if (delOrder.Bags.Any())
                    {
                        var bagList = delOrder.Bags.ToList();
                        foreach (var bag in bagList)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "Bag";
                            ttd.TableKey = "Id";
                            ttd.TableValue = bag.OrderId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.Bags.Remove(bag);
                        }
                    }
                    var ttdo = new TableToDelete();
                    ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    ttdo.TableName = "Order";
                    ttdo.TableKey = "OrderId";
                    ttdo.TableValue = delOrder.OrderId.ToString();
                    ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                    db.TableToDeletes.Add(ttdo);
                    db.Orders.Remove(delOrder);
                    Session.Remove("Order");
                    db.SaveChanges();
                    TempData["PageMessage"] = "The Order was deleted succesfully.";
                    return RedirectToAction("Index", new { @id = StatusId });
                }
            }
            TempData["PageMessage"] = "No order found.";
            return RedirectToAction("Index");
        }

        public ActionResult DeleteOrderAdjust(Guid Id)
        {
            var delOrder = db.Orders.Find(Id);
            if (delOrder != null)
            {
                int StatusId = delOrder.StatusId;
                if (delOrder.OrderScales.Any())
                {
                    var scaleList = delOrder.OrderScales.ToList();
                    foreach (var scale in scaleList)
                    {
                        var clothesScale = db.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == scale.ClothesScaleId && x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false);
                        if (clothesScale != null)
                        {
                            clothesScale.InvQty = clothesScale.InvQty + (scale.Quantity.HasValue ? scale.Quantity.Value : 0);
                            clothesScale.DateUpdated = DateTime.UtcNow;
                        }
                        var ttd = new TableToDelete();
                        ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttd.TableName = "OrderScale";
                        ttd.TableKey = "OrderScaleId";
                        ttd.TableValue = scale.OrderScaleId.ToString();
                        ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttd);
                        db.OrderScales.Remove(scale);
                        db.SaveChanges();
                    }

                }
                if (delOrder.OrderSizes.Any())
                {
                    var sizeList = delOrder.OrderSizes.ToList();
                    foreach (var size in sizeList)
                    {
                        var clothesScaleSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleSizeId == size.ClothesSizeId && x.IsActive == true && x.IsDelete == false);
                        if (clothesScaleSize != null)
                        {
                            clothesScaleSize.Quantity = clothesScaleSize.Quantity + (size.Quantity.HasValue ? size.Quantity.Value : 0);
                            clothesScaleSize.DateUpdated = DateTime.UtcNow;
                        }
                        var ttd = new TableToDelete();
                        ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttd.TableName = "OrderSize";
                        ttd.TableKey = "OrderSizeId";
                        ttd.TableValue = size.OrderSizeId.ToString();
                        ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttd);
                        db.OrderSizes.Remove(size);
                        db.SaveChanges();
                    }
                }
                if (delOrder.Boxes.Any())
                {
                    var boxList = delOrder.Boxes.ToList();
                    foreach (var box in boxList)
                    {
                        var ttd = new TableToDelete();
                        ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttd.TableName = "Box";
                        ttd.TableKey = "Id";
                        ttd.TableValue = box.OrderId.ToString();
                        ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttd);
                        db.Boxes.Remove(box);
                    }
                }
                if (delOrder.Bags.Any())
                {
                    var bagList = delOrder.Bags.ToList();
                    foreach (var bag in bagList)
                    {
                        var ttd = new TableToDelete();
                        ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttd.TableName = "Bag";
                        ttd.TableKey = "Id";
                        ttd.TableValue = bag.OrderId.ToString();
                        ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttd);
                        db.Bags.Remove(bag);
                    }
                }
                var ttdo = new TableToDelete();
                ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                ttdo.TableName = "Order";
                ttdo.TableKey = "OrderId";
                ttdo.TableValue = delOrder.OrderId.ToString();
                ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                db.TableToDeletes.Add(ttdo);
                db.Orders.Remove(delOrder);
                Session.Remove("Order");
                db.SaveChanges();
                TempData["PageMessage"] = "The Order was deleted succesfully.";
                return RedirectToAction("Index", new { @id = StatusId });
            }
            TempData["PageMessage"] = "No order found.";
            return RedirectToAction("Index");
        }

        public ActionResult MoveToWareHouse(string Ids)
        {
            if (!string.IsNullOrEmpty(Ids))
            {
                var allIds = Ids.Trim(',').Split(',');
                Guid[] values = allIds.Select(s => Guid.Parse(s)).ToArray();
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "inprocess");
                if (status != null)
                    StatusId = status.OrderStatusId;
                if (StatusId > 0)
                {
                    foreach (var Id in values)
                    {
                        var ord = db.Orders.Find(Id);
                        if (ord != null)
                        {
                            ord.StatusId = StatusId;
                            ord.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                        }
                    }
                    TempData["PageMessage"] = "The orders were succesfully moved to warehouse.";
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult ProcessOrder(Guid Id)
        {
            int UserId = 0;
            int StatusId = 0;
            ViewBag.isCustomer = SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower();
            List<string> Errors = new List<string>();
            Order lastOrder;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "inprocess");
            if (status != null)
                StatusId = status.OrderStatusId;
            ViewBag.LoadNew = true;
            int.TryParse(SiteIdentity.UserId, out UserId);
            lastOrder = db.Orders.Find(Id);
            if (lastOrder != null)
            {
                var account = db.Accounts.Find(lastOrder.AccountId);
                var retModel = new Cart();
                retModel.CartOwner = new CartOwner();
                if (account != null)
                {
                    if (account != null)
                    {
                        if (account.IsActive == true && account.IsDelete == false)
                        {
                            retModel.CartOwner = Data(account, lastOrder.OrderId != Guid.Empty ? lastOrder.OrderId : (Guid?)null);
                        }
                    }
                }
                var compName = db.Companies.FirstOrDefault(x => x.AccountId == account.AccountId && x.IsActive == true && x.IsDelete == false);
                retModel.UserName = compName != null ? compName.Name : account.FirstName + " " + account.LastName;
                retModel.UserId = lastOrder.AccountId;
                retModel.OrdNum = lastOrder.OrderNumber;
                retModel.OrderId = lastOrder.OrderId;
                retModel.Shipping = db.ShipVias.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.ShipViaId, Value = x.Name, IsSelected = false });
                retModel.Terms = db.Terms.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.TermId, Value = x.Name, IsSelected = false });
                //retModel.TotalQty = lastOrder.OriginalQty.HasValue ? lastOrder.OriginalQty.Value : 0;

                var clothes = new List<Cloth>(lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count);
                clothes.AddRange(lastOrder.OrderScales.OrderByDescending(x => x.DateCreated).Select(x => x.ClothesScale.Cloth));
                clothes.AddRange(lastOrder.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                clothes = clothes.Distinct().ToList();
                var list = clothes.GroupBy(x => x.CategoryId);
                var mFitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                var mInseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                foreach (var item in list)
                {
                    var cartItem = new CartCloth();
                    cartItem.GroupName = GroupName(item.Key);
                    //item= item.OrderByDescending(x=>x.)
                    foreach (var cloth in item.ToList())
                    {
                        var contents = new CartContents();
                        //cloth = cloth.OrderScales.OrderByDescending(x => x.DateCreated).ToList();
                        contents.ClothesId = cloth.ClothesId;
                        contents.StyleNumber = cloth.StyleNumber;
                        if (cloth.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                            contents.Image = cloth.ClothesImages.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsActive && !x.IsDelete).ImagePath;
                        else
                            contents.Image = "";

                        var scaleList = lastOrder.OrderScales.Where(x => x.ClothesScale.ClothesId == cloth.ClothesId);
                        int cId = cloth.ClothesId;

                        foreach (var scale in scaleList)
                        {
                            if (scale.Quantity.HasValue ? (scale.Quantity.Value > 0) : false)
                            {
                                var SP = new ContentDetails();
                                var fit = mFitList.FirstOrDefault(x => scale.ClothesScale.FitId.HasValue ? (x.Id == scale.ClothesScale.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => scale.ClothesScale.InseamId.HasValue ? (x.Id == scale.ClothesScale.InseamId.Value) : false);
                                SP.Fit = fit != null ? fit.Value : "";
                                SP.Inseam = inseam != null ? inseam.Value : "";
                                SP.FitId = fit != null ? fit.Id : 0;
                                SP.InseamId = inseam != null ? inseam.Id : 0;
                                SP.Pack = new ClothesScaleClass();
                                SP.Pack.ClothesId = cloth.ClothesId;
                                SP.Pack.ClothesScaleId = scale.ClothesScaleId;
                                SP.Pack.OrderSSId = scale.OrderScaleId;
                                SP.Pack.InvQty = scale.ClothesScale.InvQty.HasValue ? scale.ClothesScale.InvQty.Value : 0;
                                SP.Pack.PurchasedQty = scale.Quantity.HasValue ? scale.Quantity.Value : 0;
                                SP.Pack.QuantSum = scale.ClothesScale.ClothesScaleSizes.Sum(x => x.Quantity);
                                SP.Pack.Name = scale.ClothesScale.Name;
                                SP.Pack.isConfirm = scale.IsConfirmed.HasValue ? scale.IsConfirmed.Value : false;
                                if (SP.Pack.isConfirm)
                                    retModel.TotalQty += scale.ClothesScale.ClothesScaleSizes.Sum(x => x.Quantity).Value * SP.Pack.PurchasedQty.Value;
                                contents.SPs.Add(SP);
                            }
                        }
                        contents.SPs = contents.SPs.OrderBy(x => x.Pack.Name).ToList();
                        var sizeList = lastOrder.OrderSizes.Where(x => x.ClothesScaleSize.ClothesScale.ClothesId == cloth.ClothesId).GroupBy(x => x.ClothesScaleSize.ClothesScale);
                        foreach (var size in sizeList)
                        {
                            var fit = mFitList.FirstOrDefault(x => size.Key.FitId.HasValue ? (x.Id == size.Key.FitId.Value) : false);
                            var inseam = mInseamList.FirstOrDefault(x => size.Key.InseamId.HasValue ? (x.Id == size.Key.InseamId.Value) : false);
                            int FitId = fit != null ? fit.Id : 0;
                            int InseamId = inseam != null ? inseam.Id : 0;
                            if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId))
                            {
                                if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId && x.OpenSizes.Count > 0))
                                {

                                }
                                else
                                {
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                    foreach (var os in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = os.ClothesScaleId;
                                        scaleRow.OrderSSId = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).OrderSizeId : Guid.Empty;
                                        scaleRow.SizeId = os.SizeId;
                                        scaleRow.SizeName = os.Size.Name;
                                        scaleRow.Quantity = os.Quantity;
                                        scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).Quantity : 0;
                                        scaleRow.isConfirm = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.HasValue ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.Value : false) : false;
                                        if (scaleRow.isConfirm)
                                            retModel.TotalQty += scaleRow.PurchasedQuantity != null ? scaleRow.PurchasedQuantity.Value : 0;
                                        var OpenQty = db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity != null ? db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity : 0;
                                        scaleRow.TotalInventory = OpenQty + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * OpenQty));
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    contents.SPs.FirstOrDefault(x => x.FitId == FitId && x.InseamId == InseamId).OpenSizes.AddRange(openSizesForOS);
                                }
                            }
                            else
                            {
                                var SP = new ContentDetails();
                                SP.Fit = fit != null ? fit.Value : "";
                                SP.Inseam = inseam != null ? inseam.Value : "";
                                SP.FitId = fit != null ? fit.Id : 0;
                                SP.InseamId = inseam != null ? inseam.Id : 0;
                                SP.Pack = null;
                                var openSizesForOS = new List<ClothesScaleSizeClass>();
                                var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                foreach (var os in sSQOpenSize)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = os.ClothesScaleId;
                                    scaleRow.OrderSSId = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).OrderSizeId : Guid.Empty;
                                    scaleRow.SizeId = os.SizeId;
                                    scaleRow.SizeName = os.Size.Name;
                                    scaleRow.Quantity = os.Quantity;
                                    scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).Quantity : 0;
                                    scaleRow.isConfirm = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.HasValue ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.Value : false) : false;
                                    if (scaleRow.isConfirm)
                                        retModel.TotalQty += scaleRow.PurchasedQuantity != null ? scaleRow.PurchasedQuantity.Value : 0;
                                    var OpenQty = db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity != null ? db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity : 0;
                                    scaleRow.TotalInventory = OpenQty + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * OpenQty));
                                    openSizesForOS.Add(scaleRow);
                                }
                                SP.OpenSizes.AddRange(openSizesForOS);
                                contents.SPs.Add(SP);
                            }
                        }
                        for (int i = 0; i < contents.SPs.Count(); ++i)
                        {
                            contents.SPs[i].isConfirmed = !(contents.SPs[i].OpenSizes.Any(x => x.isConfirm == false && x.OrderSSId != Guid.Empty));
                            int temp = 0;
                            if (contents.SPs[i].Pack != null)
                            {
                                temp = contents.SPs[i].Pack.PurchasedQty.Value * contents.SPs[i].Pack.QuantSum.Value;
                                contents.SPs[i].isConfirmed = contents.SPs[i].isConfirmed && contents.SPs[i].Pack.isConfirm;
                            }
                            if (contents.SPs[i].OpenSizes.Count == 0)
                            {
                                contents.SPs[i].Quantity = temp;
                                if (contents.SPs[i].Pack != null)
                                {
                                    int sFit = contents.SPs[i].FitId, sInseam = contents.SPs[i].InseamId;
                                    var oScale = db.ClothesScales.FirstOrDefault(x => x.ClothesId == contents.ClothesId && (x.Fit != null ? x.FitId == sFit : true) && (x.InseamId != null ? x.InseamId == sInseam : true) && x.IsOpenSize == true);
                                    if (oScale != null)
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        int clothesScaleId = oScale.ClothesId;
                                        var sSQOpenSize = oScale.ClothesScaleSizes.Where(x => x.IsActive == true && x.IsDelete == false);
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            scaleRow.PurchasedQuantity = 0;
                                            var OpenQty = db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity != null ? db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity : 0;
                                            scaleRow.TotalInventory = OpenQty + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * OpenQty));
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs[i].OpenSizes.AddRange(openSizesForOS);
                                    }
                                }
                            }
                            else
                            {
                                temp += contents.SPs[i].OpenSizes.Sum(x => x.PurchasedQuantity).Value;
                                contents.SPs[i].Quantity = temp;
                            }
                            bool checkfit = contents.SPs[i].FitId > 0;
                            bool checkins = contents.SPs[i].InseamId > 0;
                            contents.SPs[i].ShowPrepack = cloth.ClothesScales.Any(x => (checkfit ? x.FitId == contents.SPs[i].FitId : true) && (checkins ? x.InseamId == contents.SPs[i].InseamId : true) &&
                                x.IsActive == true && x.IsDelete == false && x.IsOpenSize == false && x.InvQty > 0);
                        }
                        cartItem.Contents.Add(contents);
                    }
                    retModel.Clothes.Add(cartItem);
                }
                ViewBag.Bag = lastOrder.Bags.Count;
                ViewBag.Box = lastOrder.Boxes.Count;
                ViewBag.BBCount = lastOrder.Bags.Count + lastOrder.Boxes.Count;
                retModel.Note = lastOrder.Note;
                return View(retModel);
            }
            ViewBag.PageMessage = TempData["PageMessage"];
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ProcessOrder(Cart retModel)
        {
            if (retModel != null)
            {
                var order = db.Orders.Find(retModel.OrderId);
                //var account = db.Accounts.Find(lastOrder.AccountId);
                if (order != null)
                {
                    foreach (var groupItem in retModel.Clothes)
                    {
                        if (groupItem != null)
                        {
                            foreach (var cartItem in groupItem.Contents)
                            {
                                var cloth = db.Clothes.Find(cartItem.ClothesId);
                                if (cloth != null)
                                {
                                    foreach (var SP in cartItem.SPs)
                                    {
                                        if (SP.Pack != null)
                                        {
                                            var clothPack = cloth.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId && x.IsActive == true && x.IsDelete == false);
                                            var ordPack = order.OrderScales.FirstOrDefault(x => x.OrderScaleId == SP.Pack.OrderSSId);
                                            if (clothPack != null && ordPack != null)
                                            {
                                                int nQuant = SP.Pack.PurchasedQty.HasValue ? SP.Pack.PurchasedQty.Value : 0;
                                                int eQuant = ordPack.Quantity.HasValue ? ordPack.Quantity.Value : 0;
                                               
                                                if (nQuant == eQuant)
                                                    ordPack.PackedQty = ordPack.Quantity;
                                                else
                                                {
                                                    ordPack.Quantity = nQuant;
                                                    ordPack.PackedQty = nQuant;
                                                    clothPack.InvQty = (clothPack.InvQty.HasValue ? clothPack.InvQty.Value : 0) + (eQuant - nQuant);
                                                    clothPack.DateUpdated = DateTime.UtcNow;
                                                }
                                                cloth.OriginalQty -= clothPack.ClothesScaleSizes.Sum(x => x.Quantity) * ordPack.Quantity;
                                                ordPack.DateUpdated = DateTime.UtcNow;
                                                db.SaveChanges();                                               
                                            }
                                        }
                                        if (SP.OpenSizes != null)
                                        {
                                            foreach (var os in SP.OpenSizes)
                                            {
                                                var clothSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleId == os.ClothesScaleId && x.ClothesScaleSizeId == os.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false);
                                                var ordSize = order.OrderSizes.FirstOrDefault(x => x.OrderSizeId == os.OrderSSId);
                                                if (clothSize != null && ordSize != null)
                                                {
                                                    int nQuant = os.PurchasedQuantity.HasValue ? os.PurchasedQuantity.Value : 0;
                                                    int eQuant = ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0;
                                                    
                                                    if (nQuant == eQuant)
                                                        ordSize.PackedQty = ordSize.Quantity;
                                                    else
                                                    {
                                                        ordSize.Quantity = nQuant;
                                                        ordSize.PackedQty = nQuant;
                                                        clothSize.Quantity = (clothSize.Quantity.HasValue ? clothSize.Quantity.Value : 0) + (eQuant - nQuant);
                                                        clothSize.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    ordSize.DateUpdated = DateTime.UtcNow;
                                                    cloth.OriginalQty -= ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0;
                                                    db.SaveChanges();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    int StatusId = 0;
                    var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "packed");
                    if (status != null)
                        StatusId = status.OrderStatusId;
                    UpdateOrder(order.OrderId, order.Note, StatusId, true, true);
                    TempData["PageMessage"] = "The order was packed successfully.";
                    return RedirectToAction("Index", new { Id = StatusId });
                }
            }
            return RedirectToAction("ProcessOrder", new { Id = retModel != null ? retModel.OrderId : Guid.Empty });
        }

        [HttpPost]
        public ActionResult UpdateProcessOrder(Cart retModel)
        {
            if (retModel != null)
            {
                var order = db.Orders.Find(retModel.OrderId);
                if (order != null)
                {
                    foreach (var groupItem in retModel.Clothes)
                    {
                        if (groupItem != null)
                        {
                            foreach (var cartItem in groupItem.Contents)
                            {
                                var cloth = db.Clothes.Find(cartItem.ClothesId);
                                if (cloth != null)
                                {
                                    foreach (var SP in cartItem.SPs)
                                    {

                                        if (SP.Pack != null)
                                        {
                                            var clothPack = cloth.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId && x.IsActive == true && x.IsDelete == false);
                                            var ordPack = order.OrderScales.FirstOrDefault(x => x.OrderScaleId == SP.Pack.OrderSSId);
                                            if (clothPack != null && ordPack != null)
                                            {
                                                int nQuant = SP.Pack.PurchasedQty.HasValue ? SP.Pack.PurchasedQty.Value : 0;
                                                int eQuant = ordPack.Quantity.HasValue ? ordPack.Quantity.Value : 0;
                                               
                                                if (nQuant == eQuant)
                                                    ordPack.PackedQty = ordPack.Quantity;
                                                else
                                                {
                                                    ordPack.Quantity = nQuant;
                                                    ordPack.PackedQty = nQuant;
                                                    clothPack.InvQty = (clothPack.InvQty.HasValue ? clothPack.InvQty.Value : 0) + (eQuant - nQuant);
                                                    clothPack.DateUpdated = DateTime.UtcNow;
                                                }
                                                cloth.OriginalQty -= clothPack.ClothesScaleSizes.Sum(x => x.Quantity) * ordPack.Quantity;
                                                ordPack.DateUpdated = DateTime.UtcNow;
                                                db.SaveChanges();                                               
                                            }
                                        }
                                        if (SP.OpenSizes != null)
                                        {
                                            foreach (var os in SP.OpenSizes)
                                            {
                                                var clothSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleId == os.ClothesScaleId && x.ClothesScaleSizeId == os.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false);
                                                var ordSize = order.OrderSizes.FirstOrDefault(x => x.OrderSizeId == os.OrderSSId);
                                                if (clothSize != null && ordSize != null)
                                                {
                                                    int nQuant = os.PurchasedQuantity.HasValue ? os.PurchasedQuantity.Value : 0;
                                                    int eQuant = ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0;
                                                    
                                                    if (nQuant == eQuant)
                                                        ordSize.PackedQty = ordSize.Quantity;
                                                    else
                                                    {
                                                        ordSize.Quantity = nQuant;
                                                        ordSize.PackedQty = nQuant;
                                                        clothSize.Quantity = (clothSize.Quantity.HasValue ? clothSize.Quantity.Value : 0) + (eQuant - nQuant);
                                                        clothSize.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    cloth.OriginalQty -= ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0;
                                                    ordSize.DateUpdated = DateTime.UtcNow;
                                                    db.SaveChanges();                                                   
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    int StatusId = 0;
                    StatusId = order.StatusId;
                    UpdateOrder(order.OrderId, order.Note, StatusId, true, true);
                    return RedirectToAction("ProcessOrder", new { Id = retModel != null ? retModel.OrderId : Guid.Empty });
                }
            }
            return RedirectToAction("ProcessOrder", new { Id = retModel != null ? retModel.OrderId : Guid.Empty });
        }

        [HttpPost]
        public ActionResult BackOrder(Cart retModel)
        {
            if (retModel != null)
            {
                var order = db.Orders.Find(retModel.OrderId);
                if (order != null)
                {
                    int counter = order.Label.HasValue ? order.Label.Value : 0;
                    retModel.Note = !string.IsNullOrEmpty(retModel.Note) ? retModel.Note : order.Note;
                    var packOrder = new Order();
                    packOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks));
                    packOrder.AccountId = order.AccountId;
                    packOrder.EmployeeId = order.EmployeeId;
                    packOrder.AddressId = order.AddressId;
                    packOrder.CommunicationId = order.CommunicationId;
                    packOrder.Discount = order.Discount;
                    packOrder.SubmittedOn = order.SubmittedOn;
                    packOrder.ParentOrderId = order.OrderId;
                    packOrder.OrderNumber = order.OrderNumber.Split('-')[0] + "-" + (++counter);
                    packOrder.Label = counter;
                    packOrder.CreatedOn = DateTime.UtcNow;
                    packOrder.IsDelete = packOrder.IsSentToQuickBook = false;
                    packOrder.DateCreated = packOrder.DateUpdated = DateTime.UtcNow;
                    packOrder.Note = order.Note;
                    packOrder.TagId = order.TagId;
                    db.Orders.Add(packOrder);
                    var backOrder = new Order();
                    backOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks));
                    backOrder.AccountId = order.AccountId;
                    backOrder.EmployeeId = order.EmployeeId;
                    backOrder.AddressId = order.AddressId;
                    backOrder.CommunicationId = order.CommunicationId;
                    backOrder.Discount = order.Discount;
                    backOrder.SubmittedOn = order.SubmittedOn;
                    backOrder.ParentOrderId = order.OrderId;
                    backOrder.OrderNumber = order.OrderNumber.Split('-')[0] + "-" + (++counter);
                    backOrder.Label = counter;
                    backOrder.IsDelete = backOrder.IsSentToQuickBook = false;
                    backOrder.CreatedOn = DateTime.UtcNow;
                    backOrder.DateCreated = packOrder.DateUpdated = DateTime.UtcNow;
                    backOrder.Note = order.Note;
                    backOrder.TagId = order.TagId;
                    db.Orders.Add(backOrder);
                    db.SaveChanges();
                    foreach (var groupItem in retModel.Clothes)
                    {
                        if (groupItem != null)
                        {
                            foreach (var cartItem in groupItem.Contents)
                            {
                                var cloth = db.Clothes.Find(cartItem.ClothesId);
                                if (cloth != null)
                                {
                                    foreach (var SP in cartItem.SPs)
                                    {
                                        if (SP.Pack != null)
                                        {
                                            var clothPack = cloth.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId && x.IsActive == true && x.IsDelete == false);
                                            var ordPack = order.OrderScales.FirstOrDefault(x => x.OrderScaleId == SP.Pack.OrderSSId);
                                            if (clothPack != null && ordPack != null)
                                            {

                                                int nQuant = SP.Pack.PurchasedQty.HasValue ? SP.Pack.PurchasedQty.Value : 0;
                                                int eQuant = ordPack.Quantity.HasValue ? ordPack.Quantity.Value : 0;
                                                if (nQuant <= eQuant)
                                                {
                                                    var newPack = new OrderScale();
                                                    newPack.OrderScaleId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks));
                                                    newPack.IsConfirmed = SP.isConfirmed;
                                                    newPack.ClothesScaleId = ordPack.ClothesScaleId;
                                                    newPack.OrderId = SP.isConfirmed ? packOrder.OrderId : backOrder.OrderId;
                                                    if (nQuant == eQuant)
                                                    {
                                                        newPack.Quantity = nQuant;
                                                        newPack.PackedQty = SP.isConfirmed ? nQuant : 0;
                                                    }
                                                    else
                                                    {
                                                        newPack.Quantity = nQuant;
                                                        newPack.PackedQty = SP.isConfirmed ? nQuant : 0;
                                                        clothPack.InvQty = (clothPack.InvQty.HasValue ? clothPack.InvQty.Value : 0) + (eQuant - nQuant);
                                                        clothPack.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    //ordPack.DateUpdated = DateTime.UtcNow;
                                                    newPack.DateCreated = newPack.DateUpdated = DateTime.UtcNow;
                                                    db.OrderScales.Add(newPack);
                                                    db.SaveChanges();
                                                }
                                            }
                                        }
                                        if (SP.OpenSizes != null)
                                        {
                                            foreach (var os in SP.OpenSizes)
                                            {
                                                var clothSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleId == os.ClothesScaleId && x.ClothesScaleSizeId == os.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false);
                                                var ordSize = order.OrderSizes.FirstOrDefault(x => x.OrderSizeId == os.OrderSSId);
                                                if (clothSize != null && ordSize != null)
                                                {
                                                    int nQuant = os.PurchasedQuantity.HasValue ? os.PurchasedQuantity.Value : 0;
                                                    int eQuant = ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0;
                                                    if (nQuant <= eQuant)
                                                    {
                                                        var newSize = new OrderSize();
                                                        newSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks));
                                                        newSize.IsConfirmed = SP.isConfirmed;
                                                        newSize.ClothesSizeId = ordSize.ClothesSizeId;
                                                        newSize.OrderId = SP.isConfirmed ? packOrder.OrderId : backOrder.OrderId;
                                                        if (nQuant == eQuant)
                                                        {
                                                            newSize.Quantity = nQuant;
                                                            newSize.PackedQty = SP.isConfirmed ? nQuant : 0;
                                                            //ordSize.PackedQty = ordSize.Quantity;
                                                        }
                                                        else
                                                        {
                                                            //ordSize.Quantity = nQuant;
                                                            //ordSize.PackedQty = nQuant;
                                                            newSize.Quantity = nQuant;
                                                            newSize.PackedQty = SP.isConfirmed ? nQuant : 0;
                                                            clothSize.Quantity = (clothSize.Quantity.HasValue ? clothSize.Quantity.Value : 0) + (eQuant - nQuant);
                                                            clothSize.DateUpdated = DateTime.UtcNow;
                                                        }
                                                        //ordSize.DateUpdated = DateTime.UtcNow;
                                                        newSize.DateCreated = newSize.DateUpdated = DateTime.UtcNow;
                                                        db.OrderSizes.Add(newSize);
                                                        db.SaveChanges();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    int StatusId = 0;
                    var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "packed");
                    if (status != null)
                        StatusId = status.OrderStatusId;
                    UpdateOrder(order.OrderId, order.Note, StatusId, true, false);
                    UpdateOrder(packOrder.OrderId, order.Note, StatusId, true, true);
                    StatusId = 0;
                    status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "back order");
                    if (status != null)
                        StatusId = status.OrderStatusId;
                    UpdateOrder(backOrder.OrderId, order.Note, StatusId, false, true);
                    //UpdateOrder(order.OrderId, retModel.Note, StatusId);
                    TempData["PageMessage"] = "The order was packed successfully.";
                    return RedirectToAction("Index", new { Id = StatusId });
                }
            }
            return RedirectToAction("ProcessOrder", new { Id = retModel != null ? retModel.OrderId : Guid.Empty });
        }

        [NonAction]
        public void UpdateOrder(Guid OrderId, string note, int StatusId, bool isPack, bool doCalc)
        {
            var order = db.Orders.Find(OrderId);
            var account = db.Accounts.Find(order.AccountId);
            order.Note = note;
            if (doCalc)
            {
                var priceList = account.CustomerItemPrices.Where(x => x.Price.HasValue).ToList().Select(x => new { Id = x.ClothesId, Price = x.Price });
                var scalePriceList = order.OrderScales.Select(x => x.ClothesScale.Cloth).SelectMany(x => priceList.Where(y => y.Id == x.ClothesId).DefaultIfEmpty(), (x, y) => new { Price = (y != null) ? y.Price : x.Price, Id = x.ClothesId });
                var sizePriceList = order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth).SelectMany(x => priceList.Where(y => y.Id == x.ClothesId).DefaultIfEmpty(), (x, y) => new { Price = (y != null) ? y.Price : x.Price, Id = x.ClothesId });
                var ScaleQty = db.OrderScales.Where(x => x.OrderId == order.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity) * x.Quantity);
                int? QriQty;
                decimal disc = 0.0m;
                var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == order.AccountId);
                if (ci != null)
                    disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                order.Discount = disc;

                if (ScaleQty == null)
                {
                    QriQty = db.OrderSizes.Where(x => x.OrderId == order.OrderId).Sum(x => x.Quantity);
                    order.OriginalQty = QriQty.HasValue ? QriQty.Value : 0;
                }
                else
                {
                    QriQty = db.OrderScales.Where(x => x.OrderId == order.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity) * x.Quantity);
                    order.OriginalQty = QriQty.HasValue ? QriQty.Value : 0;
                    QriQty = db.OrderSizes.Where(x => x.OrderId == order.OrderId).Sum(x => x.Quantity);
                    order.OriginalQty += QriQty.HasValue ? QriQty.Value : 0;
                }
                var scalePrice = order.OrderScales.Sum(x => (x.Quantity) * (x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity)) * (scalePriceList.FirstOrDefault(z => z.Id == x.ClothesScale.ClothesId).Price));
                var sizePrice = order.OrderSizes.Sum(x => x.Quantity * (sizePriceList.FirstOrDefault(y => y.Id == x.ClothesScaleSize.ClothesScale.ClothesId).Price));
                order.GrandTotal = scalePrice + sizePrice;
                order.FinalAmount = order.GrandTotal - (order.GrandTotal * ((order.Discount.HasValue ? order.Discount.Value : 0) / 100)) + (order.ShippingCost.HasValue ? order.ShippingCost.Value : 0); ;
            }
            if (isPack)
            {
                order.PackedOn = DateTime.UtcNow;
                int? PQty = db.OrderSizes.Where(x => x.OrderId == order.OrderId && x.IsConfirmed == true).Sum(x => x.Quantity);
                order.PackedQty = PQty.HasValue ? PQty.Value : 0;
                PQty = db.OrderScales.Where(x => x.OrderId == order.OrderId && x.IsConfirmed == true).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity) * x.Quantity);
                order.PackedQty += PQty.HasValue ? PQty.Value : 0;
                //order.PackedQty = order.OriginalQty;
            }
            
            order.IsSentToQuickBook = false;
            order.StatusId = StatusId;
            order.DateUpdated = DateTime.UtcNow;
            db.SaveChanges();
        }

        [HttpPost]
        public ActionResult AddByStyleNo(int Id, Guid OrderId)
        {
            string result = "-1";
            var cloth = db.Clothes.Find(Id);
            int UserId = 0;
            int StatusId = 0;
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            lastOrder = db.Orders.Find(OrderId);
            if (lastOrder == null)
            {
                lastOrder = new DB.Order();
                lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                lastOrder.OrderNumber = string.Empty;
                lastOrder.AccountId = UserId;
                if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower())
                {
                    var sp = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == UserId);
                    if (sp != null)
                        lastOrder.EmployeeId = sp.SalesPersonId;
                }
                if (!lastOrder.EmployeeId.HasValue)
                    lastOrder.EmployeeId = UserId;
                lastOrder.CreatedOn = DateTime.UtcNow;
                lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                lastOrder.StatusId = StatusId;
                lastOrder.ShippingCost = 0;
                decimal disc = 0.0m;
                if (UserId > 0)
                {
                    var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == UserId);
                    if (ci != null)
                        disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                }
                lastOrder.Discount = disc;
                lastOrder.IsDelete = false;
                lastOrder.IsSentToQuickBook = false;
                db.Orders.Add(lastOrder);
                lastOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                try { db.SaveChanges(); }
                catch { }
            }
            if (cloth != null && lastOrder != null)
            {

                bool isAddded = false;
                var clothScales = cloth.ClothesScales.Where(x => x.IsActive == true && x.IsDelete == false).ToList();
                var scaleIds = clothScales.Where(x => x.IsOpenSize == false && !string.IsNullOrEmpty(x.Name) && x.InvQty > 0 && x.ClothesScaleSizes.Sum(y => y.Quantity) > 0).Select(x => x.ClothesScaleId);

                if (lastOrder.OrderScales.Any(x => scaleIds.Contains(x.ClothesScaleId)))
                    result = "0";
                else
                {
                    var pp = clothScales.FirstOrDefault(x => x.IsOpenSize == false && !string.IsNullOrEmpty(x.Name) && x.InvQty > 0 && x.IsActive == true && x.IsDelete == false);
                    if (pp != null)
                    {
                        var newPack = new OrderScale();
                        newPack.OrderScaleId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        newPack.OrderId = lastOrder.OrderId;
                        newPack.ClothesId = pp.ClothesId;
                        newPack.ClothesScaleId = pp.ClothesScaleId;
                        newPack.Quantity = 1;
                        newPack.PackedQty = 0;
                        newPack.IsConfirmed = false;
                        newPack.DateCreated = newPack.DateUpdated = DateTime.UtcNow;
                        db.OrderScales.Add(newPack);
                        db.SaveChanges();
                    }
                    var os = cloth.ClothesScales.FirstOrDefault(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false);
                    if (os != null)
                    {
                        foreach (var size in os.ClothesScaleSizes)
                        {
                            var newSize = new OrderSize();
                            newSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            newSize.OrderId = lastOrder.OrderId;
                            newSize.ClothesId = os.ClothesId;
                            newSize.ClothesSizeId = size.ClothesScaleSizeId;
                            newSize.Quantity = size.Quantity.HasValue ? (size.Quantity.Value > 0 ? (int?)0 : null) : null;
                            newSize.PackedQty = 0;
                            newSize.IsConfirmed = false;
                            newSize.DateCreated = newSize.DateUpdated = DateTime.UtcNow;
                            db.OrderSizes.Add(newSize);
                            db.SaveChanges();
                        }
                    }
                    result = "1";
                }
                if (lastOrder.OrderScales.Any())
                    lastOrder.OriginalQty = lastOrder.OrderScales.Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                if (lastOrder.OrderSizes.Any())
                    lastOrder.OriginalQty += lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                try { db.SaveChanges(); }
                catch { }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public void DeleteQuantity(string PackId, string SizeIds)
        {
            Guid OrderId = Guid.Empty;
            DB.Order lastOrder = null;
            lastOrder = db.Orders.Find(OrderId);
            if (!string.IsNullOrEmpty(PackId))
            {
                Guid PackOrdId;
                Guid.TryParse(PackId, out PackOrdId);
                var pack = db.OrderScales.Find(PackOrdId);
                if (pack != null)
                {
                    OrderId = pack.OrderId;
                    var ttd = new TableToDelete();
                    ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    ttd.TableName = "OrderScale";
                    ttd.TableKey = "OrderScaleId";
                    ttd.TableValue = pack.OrderScaleId.ToString();
                    ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                    db.TableToDeletes.Add(ttd);
                    db.OrderScales.Remove(pack);
                    db.SaveChanges();
                }
            }
            if (!string.IsNullOrEmpty(SizeIds))
            {
                string[] Ids = SizeIds.Split(',');
                foreach (var Id in Ids)
                {
                    Guid SizeOrdId;
                    Guid.TryParse(Id, out SizeOrdId);
                    OrderSize size = null;

                    size = db.OrderSizes.Find(SizeOrdId);
                    if (size != null)
                    {
                        OrderId = size.OrderId;

                        var ttd = new TableToDelete();
                        ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttd.TableName = "OrderSize";
                        ttd.TableKey = "OrderSizeId";
                        ttd.TableValue = size.OrderSizeId.ToString();
                        ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttd);
                        db.OrderSizes.Remove(size);
                    }
                }
                db.SaveChanges();
            }
            lastOrder = db.Orders.Find(OrderId);
            if (lastOrder != null)
            {
                var count = lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count;
                if (count == 0)
                {

                    var ttdo = new TableToDelete();
                    ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    ttdo.TableName = "Order";
                    ttdo.TableKey = "OrderId";
                    ttdo.TableValue = lastOrder.OrderId.ToString();
                    ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                    db.TableToDeletes.Add(ttdo);
                    db.Orders.Remove(lastOrder);
                    Session.Remove("Order");
                    Session.Remove("EditingOrder");


                }
                else
                {
                    lastOrder.OriginalQty = 0;
                    if (lastOrder.OrderScales.Any())
                        lastOrder.OriginalQty = lastOrder.OrderScales.Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                    if (lastOrder.OrderSizes.Any())
                        lastOrder.OriginalQty += lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);

                    lastOrder.GrandTotal = CalcSum(lastOrder.OrderId);
                    lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * (lastOrder.Discount / 100));
                    lastOrder.FinalAmount += (lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0);
                    db.SaveChanges();
                }
                db.SaveChanges();
            }
            TempData["CartSuccess"] = "abc";

            RedirectToAction("Cart");
        }
        [NonAction]
        public string GroupName(int TypeId)
        {
            string GroupName = string.Empty;
            var cType = db.Categories.Find(TypeId);
            if (cType != null)
            {
                var subCat = db.Categories.Find(cType.ParentId);
                if (subCat != null)
                {
                    var cat = db.Categories.Find(subCat.ParentId);
                    if (cat != null)
                        if (cat.ParentId == 0)
                            GroupName = cat.Name + " ";
                }
                GroupName += cType.Name;
            }
            return GroupName;
        }

        public ActionResult BBDetails(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                var model = new BagsBoxes();
                model.OrderId = order.OrderId;
                model.UserId = order.AccountId;
                model.Status = order.StatusId;
                model.UserName = order.Account.FirstName + " " + order.Account.LastName;
                model.Shipping = string.Empty;
                var Address = db.Addresses.Where(x => x.AccountId == order.AccountId && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress).FirstOrDefault();
                if (Address != null)
                {
                    var list = new List<string>();
                    if (!string.IsNullOrEmpty(Address.Street))
                        list.Add(Address.Street);
                    if (!string.IsNullOrEmpty(Address.Street))
                        list.Add(Address.City);
                    if (!string.IsNullOrEmpty(Address.Street))
                        list.Add(Address.State);
                    if (!string.IsNullOrEmpty(Address.Street))
                        list.Add(Address.Pincode);
                    model.Shipping = string.Join(", ", list);
                }
                model.ShipId = order.ShipViaId.HasValue ? order.ShipViaId.Value : 0;
                if (order.Account.CustomerOptionalInfoes.FirstOrDefault() != null && model.ShipId == 0)
                {
                    var info = order.Account.CustomerOptionalInfoes.FirstOrDefault();
                    model.ShipId = info.ShipViaId.HasValue ? info.ShipViaId.Value : 0;
                    order.ShipViaId = model.ShipId;
                    try { db.SaveChanges(); }
                    catch { }
                }


                model.ShipVias = db.ShipVias.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.ShipViaId, Value = x.Name, IsSelected = false });
                model.BagCount = model.BoxCount = 0;
                if (order.Bags.Any() || order.Boxes.Any())
                {
                    if (order.Bags.Count > 0)
                        model.BagCount = order.Bags.Count;
                    if (order.Boxes.Count > 0)
                        model.BoxCount = order.Boxes.Count;
                }

                var clothes = new List<Cloth>(order.OrderScales.Count + order.OrderSizes.Count);
                clothes.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                clothes.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                clothes = clothes.Distinct().ToList();
                var clothlist = clothes.GroupBy(x => x.CategoryId);
                List<string> StyleList = new List<string>();
                foreach (var item in clothlist)
                {
                    foreach (var cloth in item.ToList())
                        StyleList.Add("#" + cloth.StyleNumber);
                }
                model.StyleNumberList = StyleList;
                return PartialView("PackagingList", model);
            }
            TempData["PageMessage"] = "No order found.";
            return RedirectToAction("Index");
        }

        public ActionResult DetailTable(Guid Id, int? BagCount, int? BoxCount)
        {
            var order = db.Orders.Find(Id);

            if (order != null)
            {
                var model = new BBDetails();
                if (order.Bags.Any() || order.Boxes.Any() || (BagCount.HasValue ? BagCount.Value > 0 : false) || (BoxCount.HasValue ? BoxCount.Value > 0 : false))
                {
                    if (order.Bags.Any())
                        model.Bags.AddRange(order.Bags);

                    if ((BagCount.HasValue ? BagCount.Value > 0 : false))
                    {
                        for (int i = 0; i < BagCount.Value; i++)
                        {
                            var newBag = new Bag();
                            newBag.Id = 0;
                            newBag.Name = "";
                            newBag.Dimension = "";
                            newBag.OrderId = order.OrderId;
                            newBag.Weight = "0";
                            model.Bags.Add(newBag);
                        }
                    }
                    if (order.Boxes.Any())
                        model.Boxes.AddRange(order.Boxes);

                    if ((BoxCount.HasValue ? BoxCount.Value > 0 : false))
                    {
                        for (int i = 0; i < BoxCount.Value; i++)
                        {
                            var newBag = new Box();
                            newBag.Id = 0;
                            newBag.Name = "";
                            newBag.Dimension = "0:0:0";
                            newBag.OrderId = order.OrderId;
                            newBag.Weight = "0";
                            model.Boxes.Add(newBag);
                        }
                    }
                }
                return PartialView("DetailPartial", model);
            }
            TempData["PageMessage"] = "No order found.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult SaveBagBox(BBDetails retModel, Guid OrderId, string IsShip, string Replace, string IsPacked)
        {
            if (retModel != null)
            {
                if (retModel.Boxes != null)
                {
                    int i = 0;
                    foreach (var box in retModel.Boxes)
                    {
                        var dbBox = db.Boxes.Find(box.Id);
                        if (dbBox == null)
                        {
                            dbBox = new Box();
                            dbBox.Id = 0;
                            dbBox.Name = "Box" + (++i);
                            dbBox.OrderId = box.OrderId;
                        }
                        dbBox.Dimension = box.Dimension;
                        dbBox.Weight = box.Weight;
                        if (dbBox.Id == 0)
                            db.Boxes.Add(dbBox);
                        db.SaveChanges();
                        if (OrderId == Guid.Empty)
                            OrderId = box.OrderId;
                    }
                }
                if (retModel.Bags != null)
                {
                    int i = 0;
                    foreach (var bag in retModel.Bags)
                    {
                        var dbBag = db.Bags.Find(bag.Id);
                        if (dbBag == null)
                        {
                            dbBag = new Bag();
                            dbBag.Id = 0;
                            dbBag.Name = "Bag" + (++i);
                            dbBag.OrderId = bag.OrderId;
                            dbBag.Dimension = string.Empty;
                        }
                        dbBag.Weight = bag.Weight;
                        if (dbBag.Id == 0)
                            db.Bags.Add(dbBag);
                        db.SaveChanges();
                        if (OrderId == Guid.Empty)
                            OrderId = bag.OrderId;
                    }
                }
                if (!string.IsNullOrEmpty(IsShip))
                {
                    var order = db.Orders.Find(OrderId);
                    int StatusId = 0;
                    var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "shipped");
                    if (status != null)
                        StatusId = status.OrderStatusId;
                    if (order != null)
                    {
                        order.StatusId = StatusId;
                        order.ShippedOn = DateTime.UtcNow;
                        order.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        TempData["PageMessage"] = "The order was shipped successfully.";
                        return RedirectToAction("Index", new { Id = StatusId });
                    }
                }
                else if (!string.IsNullOrEmpty(Replace))
                    return RedirectToAction("DetailTable", new { Id = OrderId });
                else
                {
                    if (string.IsNullOrEmpty(IsPacked))
                        return RedirectToAction("ProcessOrder", new { Id = OrderId });
                    else
                    {
                        var lastOrder = db.Orders.Find(OrderId);
                        if (lastOrder != null)
                            if (lastOrder.ShipVia != null)
                                if (lastOrder.ShipVia.Name.ToLower().Contains("ups"))
                                    TempData["OpenUPS"] = lastOrder.OrderId;
                        return RedirectToAction("Index", new { Id = (int)OrderMode.Packed });
                    }
                }
            }
            return RedirectToAction("ProcessOrder");
        }

        [HttpPost]
        public ActionResult SaveShipVia(int UserId, int ShipVia, Guid OrderId)
        {
            var customer = db.Accounts.Find(UserId);
            var ship = db.ShipVias.Find(ShipVia);
            var order = db.Orders.Find(OrderId);
            if (customer != null && ship != null && order != null)
            {
                var info = customer.CustomerOptionalInfoes.FirstOrDefault();
                if (info == null)
                {
                    info.CustomerOptionalInfoId = 0;
                    info.AccountId = UserId;
                    info.CustomerType = (int)Platini.Models.CustomerType.Retail;
                }
                info.ShipViaId = ShipVia;
                order.ShipViaId = ShipVia;
                if (info.CustomerOptionalInfoId == 0)
                    db.CustomerOptionalInfoes.Add(info);
                db.SaveChanges();
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteBB(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                if (order.Boxes.Any())
                {
                    var boxList = order.Boxes.ToList();
                    foreach (var box in boxList)
                    {
                        db.Boxes.Remove(box);
                    }
                    db.SaveChanges();
                }
                if (order.Bags.Any())
                {
                    var bagList = order.Bags.ToList();
                    foreach (var bag in bagList)
                    {
                        db.Bags.Remove(bag);
                    }
                    db.SaveChanges();
                }
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteBagBox(int Id, string Type)
        {
            if (!string.IsNullOrEmpty(Type))
            {
                Type = Type.Trim().ToLower();
                if (Type == "bag")
                {
                    var bag = db.Bags.Find(Id);
                    if (bag != null)
                    {
                        db.Bags.Remove(bag);
                        db.SaveChanges();
                        return Json("Success", JsonRequestBehavior.AllowGet);
                    }
                }
                else if (Type == "box")
                {
                    var box = db.Boxes.Find(Id);
                    if (box != null)
                    {
                        db.Boxes.Remove(box);
                        db.SaveChanges();
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult BreakPack(int Id, int fId, int sId, Guid OrderId)
        {
            var order = db.Orders.Find(OrderId);
            var cloth = db.Clothes.Find(Id);
            if (cloth != null && order != null)
            {
                bool checkFit = fId > 0;
                bool checkInseam = sId > 0;
                var pack = cloth.ClothesScales.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize == false && (checkFit ? x.FitId == fId : true) && (checkInseam ? x.InseamId == sId : true) && x.InvQty > 0);
                if (pack != null)
                {
                    if ((pack.InvQty.HasValue ? pack.InvQty.Value : 0) == 1)
                    {
                        return Json("1", JsonRequestBehavior.AllowGet);
                    }
                    checkFit = pack.FitId.HasValue;
                    checkInseam = pack.InseamId.HasValue;
                    var open = db.ClothesScales.Where(x => x.ClothesId == pack.ClothesId && (checkInseam ? x.InseamId == pack.InseamId : true) && (checkFit ? x.FitId == pack.FitId : true) && x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                    if (open == null)
                    {
                        open = new ClothesScale();
                        open.ClothesId = pack.ClothesId;
                        open.FitId = pack.FitId;
                        open.InseamId = pack.InseamId;
                        open.InvQty = 0;
                        open.IsOpenSize = true;
                        open.Name = null;
                        open.IsActive = true;
                        open.IsDelete = false;
                        open.DateCreated = open.DateUpdated = DateTime.UtcNow;
                        db.ClothesScales.Add(open);
                        db.SaveChanges();
                    }
                    foreach (var size in pack.ClothesScaleSizes)
                    {
                        var openSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == open.ClothesScaleId && x.SizeId == size.SizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                        if (openSize == null)
                        {
                            openSize = new ClothesScaleSize();
                            openSize.ClothesScaleId = open.ClothesScaleId;
                            openSize.SizeId = size.SizeId;
                            openSize.Quantity = size.Quantity;
                            openSize.IsActive = true;
                            openSize.IsDelete = false;
                            openSize.DateCreated = openSize.DateUpdated = DateTime.UtcNow;
                            db.ClothesScaleSizes.Add(openSize);
                            db.SaveChanges();
                        }
                        else
                        {
                            openSize.Quantity = openSize.Quantity + size.Quantity;
                            openSize.DateUpdated = DateTime.UtcNow;
                        }
                        db.SaveChanges();
                    }
                    pack.InvQty -= 1;
                    pack.DateUpdated = DateTime.UtcNow;
                    order.IsSentToQuickBook = false;
                    db.SaveChanges();
                    UpdateOrder(order.OrderId, order.Note, order.StatusId, false, true);
                    return Json("success", JsonRequestBehavior.AllowGet);
                }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ConfirmQuantity(int Id, int fId, int sId, Guid OrderId)
        {
            var order = db.Orders.Find(OrderId);
            var cloth = db.Clothes.Find(Id);
            if (cloth != null && order != null)
            {
                bool checkFit = fId > 0;
                bool checkInseam = sId > 0;
                var packs = cloth.ClothesScales.Where(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize == false && (checkFit ? x.FitId == fId : true) && (checkInseam ? x.InseamId == sId : true));
                var oses = cloth.ClothesScales.Where(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize == true && (checkFit ? x.FitId == fId : true) && (checkInseam ? x.InseamId == sId : true));
                foreach (var pack in packs)
                {
                    if (order.OrderScales.Any(x => x.ClothesScaleId == pack.ClothesScaleId))
                    {
                        var ordPack = order.OrderScales.FirstOrDefault(x => x.ClothesScaleId == pack.ClothesScaleId);
                        if (ordPack != null)
                        {
                            ordPack.IsConfirmed = ordPack.IsConfirmed.HasValue ? !ordPack.IsConfirmed.Value : true;
                            ordPack.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                        }
                    }
                }
                foreach (var os in oses)
                {
                    var sizeList = os.ClothesScaleSizes.ToList();
                    foreach (var size in sizeList)
                    {
                        var ordSize = order.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                        if (ordSize != null)
                        {
                            ordSize.IsConfirmed = ordSize.IsConfirmed.HasValue ? !ordSize.IsConfirmed.Value : true;
                            ordSize.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                        }
                    }
                }
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConfirmTotalQuantity(Cart retModel)
        {
            var order = db.Orders.Find(retModel.OrderId);
            if (retModel.Clothes != null)
            {
                #region ConfirmTotalQty
                //foreach (var groupItem in retModel.Clothes)
                //{
                //    if (groupItem != null)
                //    {
                //        foreach (var cartItem in groupItem.Contents)
                //        {
                //            var cloth = db.Clothes.Find(cartItem.ClothesId);
                //            if (cloth != null && order != null)
                //            {
                //                foreach (var SP in cartItem.SPs)
                //                {
                //                    bool checkFit = SP.FitId > 0;
                //                    bool checkInseam = SP.InseamId > 0;
                //                    var packs = cloth.ClothesScales.Where(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize == false && (checkFit ? x.FitId == SP.FitId : true) && (checkInseam ? x.InseamId == SP.InseamId : true));
                //                    var oses = cloth.ClothesScales.Where(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize == true && (checkFit ? x.FitId == SP.FitId : true) && (checkInseam ? x.InseamId == SP.InseamId : true));
                //                    foreach (var pack in packs)
                //                    {
                //                        if (order.OrderScales.Any(x => x.ClothesScaleId == pack.ClothesScaleId))
                //                        {
                //                            var ordPack = order.OrderScales.FirstOrDefault(x => x.ClothesScaleId == pack.ClothesScaleId);
                //                            if (ordPack != null)
                //                            {
                //                                ordPack.IsConfirmed = ordPack.IsConfirmed.HasValue ? !ordPack.IsConfirmed.Value : true;
                //                                ordPack.DateUpdated = DateTime.UtcNow;
                //                                retModel.isConfirmed = ordPack.IsConfirmed.HasValue ? !ordPack.IsConfirmed.Value : true;
                //                                db.SaveChanges();
                //                            }
                //                        }
                //                    }
                //                    foreach (var os in oses)
                //                    {
                //                        var sizeList = os.ClothesScaleSizes.ToList();
                //                        foreach (var size in sizeList)
                //                        {
                //                            var ordSize = order.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                //                            if (ordSize != null)
                //                            {
                //                                ordSize.IsConfirmed = ordSize.IsConfirmed.HasValue ? !ordSize.IsConfirmed.Value : true;
                //                                ordSize.DateUpdated = DateTime.UtcNow;
                //                                retModel.isConfirmed = ordSize.IsConfirmed.HasValue ? !ordSize.IsConfirmed.Value : true;
                //                                db.SaveChanges();
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
                #endregion
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteOrders(string Ids)
        {
            if (!string.IsNullOrEmpty(Ids))
            {
                var allIds = Ids.Trim(',').Split(',');
                Guid[] values = allIds.Select(s => Guid.Parse(s)).ToArray();
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                if (status != null)
                    StatusId = status.OrderStatusId;
                foreach (var Id in values)
                {
                    var delOrder = db.Orders.Find(Id);
                    if (delOrder != null)
                    {
                        if (delOrder.OrderScales.Any())
                        {
                            var scaleList = delOrder.OrderScales.ToList();
                            foreach (var scale in scaleList)
                            {
                                if (delOrder.StatusId != StatusId)
                                {
                                    var clothesScale = db.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == scale.ClothesScaleId && x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false);
                                    if (clothesScale != null)
                                    {
                                        clothesScale.InvQty = clothesScale.InvQty + (scale.Quantity.HasValue ? scale.Quantity.Value : 0);
                                        clothesScale.DateUpdated = DateTime.UtcNow;
                                    }
                                }
                                var ttd = new TableToDelete();
                                ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                ttd.TableName = "OrderScale";
                                ttd.TableKey = "OrderScaleId";
                                ttd.TableValue = scale.OrderScaleId.ToString();
                                ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                db.TableToDeletes.Add(ttd);
                                db.OrderScales.Remove(scale);
                            }
                            db.SaveChanges();
                        }
                        if (delOrder.OrderSizes.Any())
                        {
                            var sizeList = delOrder.OrderSizes.ToList();
                            foreach (var size in sizeList)
                            {
                                if (delOrder.StatusId != StatusId)
                                {
                                    var clothesScaleSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleSizeId == size.ClothesSizeId && x.IsActive == true && x.IsDelete == false);
                                    if (clothesScaleSize != null)
                                    {
                                        clothesScaleSize.Quantity = clothesScaleSize.Quantity + (size.Quantity.HasValue ? size.Quantity.Value : 0);
                                        clothesScaleSize.DateUpdated = DateTime.UtcNow;
                                    }
                                }
                                var ttd = new TableToDelete();
                                ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                ttd.TableName = "OrderSize";
                                ttd.TableKey = "OrderSizeId";
                                ttd.TableValue = size.OrderSizeId.ToString();
                                ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                db.TableToDeletes.Add(ttd);
                                db.OrderSizes.Remove(size);
                            }
                            db.SaveChanges();
                        }
                        if (delOrder.Boxes.Any())
                        {
                            var boxList = delOrder.Boxes.ToList();
                            foreach (var box in boxList)
                            {
                                var ttd = new TableToDelete();
                                ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                ttd.TableName = "Box";
                                ttd.TableKey = "Id";
                                ttd.TableValue = box.OrderId.ToString();
                                ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                db.TableToDeletes.Add(ttd);
                                db.Boxes.Remove(box);
                            }
                        }
                        if (delOrder.Bags.Any())
                        {
                            var bagList = delOrder.Bags.ToList();
                            foreach (var bag in bagList)
                            {
                                var ttd = new TableToDelete();
                                ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                ttd.TableName = "Bag";
                                ttd.TableKey = "Id";
                                ttd.TableValue = bag.OrderId.ToString();
                                ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                db.TableToDeletes.Add(ttd);
                                db.Bags.Remove(bag);
                            }
                        }
                        var ttdo = new TableToDelete();
                        ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttdo.TableName = "Order";
                        ttdo.TableKey = "OrderId";
                        ttdo.TableValue = delOrder.OrderId.ToString();
                        ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttdo);
                        db.Orders.Remove(delOrder);
                        Session.Remove("Order");
                        db.SaveChanges();
                    }
                }
                TempData["PageMessage"] = "The orders were deleted successfully.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult qShip(string Ids, string pwd)
        {
            var ErrorList = new List<string>();
            if (!string.IsNullOrEmpty(Ids) && !string.IsNullOrEmpty(pwd))
            {
                int UserId = 0;
                int.TryParse(SiteIdentity.UserId, out UserId);
                var account = db.Accounts.Find(UserId);
                if (account != null && account.Password != null && account.IsActive == true && account.IsDelete == false)
                {
                    var passstring = Encoding.ASCII.GetString(account.Password);
                    if (passstring == pwd)
                    {
                        var allIds = Ids.Trim(',').Split(',');
                        Guid[] values = allIds.Select(s => Guid.Parse(s)).ToArray();
                        int StatusId = 0;
                        var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "shipped");
                        if (status != null)
                            StatusId = status.OrderStatusId;
                        foreach (var Id in values)
                        {
                            var shipOrder = db.Orders.Find(Id);
                            if (shipOrder != null)
                            {
                                shipOrder.StatusId = StatusId;
                                shipOrder.ShippedOn = DateTime.UtcNow;
                                shipOrder.DateUpdated = DateTime.UtcNow;
                                db.SaveChanges();
                            }
                        }
                        TempData["PageMessage"] = "The orders were successfully shipped.";
                        return RedirectToAction("Index", new { Id = StatusId });
                    }
                }
                ErrorList.Add("Password did not match");
            }
            if (string.IsNullOrEmpty(pwd))
                ErrorList.Add("Please enter your password");
            if (string.IsNullOrEmpty(Ids))
                ErrorList.Add("Please select an order");
            TempData["PageMessage"] = string.Join(". ", ErrorList);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Ship(string Ids)
        {
            var ErrorList = new List<string>();
            if (!string.IsNullOrEmpty(Ids))
            {
                var allIds = Ids.Trim(',').Split(',');
                Guid[] values = allIds.Select(s => Guid.Parse(s)).ToArray();
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "shipped");
                if (status != null)
                    StatusId = status.OrderStatusId;
                foreach (var Id in values)
                {
                    var shipOrder = db.Orders.Find(Id);
                    if (shipOrder != null)
                    {
                        shipOrder.StatusId = StatusId;
                        shipOrder.ShippedOn = DateTime.UtcNow;
                        shipOrder.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }
                TempData["PageMessage"] = "The orders were successfully shipped.";
                return RedirectToAction("Index", new { Id = StatusId });

            }
            if (string.IsNullOrEmpty(Ids))
                ErrorList.Add("Please select an order");
            TempData["PageMessage"] = string.Join(". ", ErrorList);
            return RedirectToAction("Index");
        }

        public ActionResult QBOrder(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                order.IsSentToQuickBook = true;
                if (QuickBookStrings.UseQuickBook())
                    generateInvoice(order.OrderId, order.AccountId);
                order.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "The order was succesfully sent to Quickbooks.";
                return RedirectToAction("Index", new { Id = order.StatusId });
            }
            TempData["PageMessage"] = "Order not found";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                SiteConfiguration.Mode = ModeEnum.Order.ToString();
                Session.Remove("Order");
                Session.Remove("EditingOrder");
                Session["EditingOrder"] = Id;
                return RedirectToAction("Index", "Home", new { @area = "", OrdId = Id });
            }
            TempData["PageMessage"] = "Order not found";
            return RedirectToAction("Index");
        }


        public ActionResult Address(int Id)
        {
            var address = db.Addresses.Find(Id);
            if (address != null)
            {
                var retModel = new AddressModel().InjectClass(address);
                return PartialView("AddressDetail", retModel);
            }
            return PartialView("AddressDetail");
        }

        [HttpPost]
        public ActionResult Address(Address address)
        {
            return View();
        }
        public ActionResult ConfirmBox(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                var retModel = new OrderTotals();
                retModel.OrderId = order.OrderId;
                retModel.UserId = order.AccountId;
                retModel.GT = order.GrandTotal.HasValue ? order.GrandTotal.Value : 0.0m;
                retModel.SC = order.ShippingCost.HasValue ? order.ShippingCost.Value : 0.0m;
                retModel.DT = order.Discount.HasValue ? order.Discount.Value : 0.0m;
                retModel.FA = order.FinalAmount.HasValue ? order.FinalAmount.Value : 0.0m;
                return PartialView(retModel);
            }
            return PartialView();
        }

        public ActionResult UpsDetail(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                var retModel = new UpsValidate();
                retModel.ShipToAddress = new AddressText();
                var addr = order.Address;
                if (addr == null)
                {
                    var account = db.Accounts.Find(order.AccountId);
                    if (account != null)
                        addr = account.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                    if (addr == null)
                        addr = account.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                }
                if (addr != null)
                {
                    string State;
                    string City;
                    string Url = "http://www.webservicex.net/uszip.asmx/GetInfoByZIP?USZip=";
                    Url = string.Format("{0}{1}", Url, addr.Pincode);
                    GetCS(Url, out State, out City);
                    if (!string.IsNullOrEmpty(State) && !string.IsNullOrEmpty(City))
                    {
                        retModel.ShipToAddress.Line1 = City;
                        retModel.ShipToAddress.Line2 = State;
                    }
                    else
                    {
                        retModel.ShipToAddress.Line1 = addr.City;
                        retModel.ShipToAddress.Line2 = addr.State;
                    }
                    retModel.ShipToAddress.AddressId = addr.AddressId;
                    retModel.ShipToAddress.To = addr.Street;

                    retModel.ShipToAddress.ZipCode = addr.Pincode;
                }
                retModel.ShipFrom = ConfigurationManager.AppSettings["ShipFrom"];
                retModel.OrderId = order.OrderId;
                retModel.BoxCount = order.Boxes.Count;
                retModel.COD = retModel.satD = false;
                retModel.Verified = string.Empty;
                return PartialView(retModel);
            }
            return PartialView();
        }

        [HttpPost]
        public ActionResult ValidateUpsAddress(string addr, string city, string state, string zip, Guid orderId)
        {
            string result = "failure";
            if (!string.IsNullOrEmpty(addr) && !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(zip))
            {
                try
                {
                    string UpsUrl = ConfigurationManager.AppSettings["UPSAddressTest"];
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["UseLiveUPS"]) && ConfigurationManager.AppSettings["UseLiveUPS"] == "1")
                        UpsUrl = ConfigurationManager.AppSettings["UPSAddressLive"];
                    var uRequest = new UpsRequest();
                    var accRequest = new AccessRequest(ConfigurationManager.AppSettings["UPSAccessLicenseNumber"], ConfigurationManager.AppSettings["UPSUserId"], ConfigurationManager.AppSettings["UPSPassword"]);
                    var uAddr = new UPSAddress();
                    uAddr.City = city.Trim();
                    uAddr.StateProvinceCode = state.Trim();
                    uAddr.PostalCode = zip.Trim();
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    string response = uRequest.AddressValidateRequest(accRequest, uAddr, UpsUrl);
                    #region ParseResponse
                    if (!string.IsNullOrEmpty(response))
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(response);
                        var nodes = doc.SelectNodes("AddressValidationResponse/AddressValidationResult");
                        var respNodes = doc.SelectNodes("AddressValidationResponse/Response/ResponseStatusDescription");
                        string strResponseStatusDescription = string.Empty;
                        var order = db.Orders.Find(orderId);
                        var customer = db.Accounts.Find(order.AccountId);
                        var shippingAddress = customer.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                        if (shippingAddress != null)
                        {
                            shippingAddress.Street = addr;
                            shippingAddress.City = city;
                            shippingAddress.State = state;
                            shippingAddress.Pincode = zip;
                            shippingAddress.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                        }
                        else
                        {
                            var ShipAddress = new DB.Address();
                            ShipAddress.Street = addr;
                            ShipAddress.City = city;
                            ShipAddress.State = state;
                            ShipAddress.Pincode = zip;
                            ShipAddress.AccountId = order.AccountId;
                            ShipAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
                            ShipAddress.IsActive = true;
                            ShipAddress.IsDelete = false;
                            ShipAddress.DateCreated = DateTime.UtcNow;
                            ShipAddress.DateUpdated = DateTime.UtcNow;
                            db.Addresses.Add(ShipAddress);
                            db.SaveChanges();
                        }
                        if (respNodes != null && respNodes.Count > 0)
                        {
                            if (respNodes.Item(0) != null && respNodes.Item(0).ChildNodes != null && respNodes.Item(0).ChildNodes[0] != null)
                                strResponseStatusDescription = respNodes.Item(0).ChildNodes[0].InnerText;
                        }
                        if (nodes.Count == 1 && !string.IsNullOrEmpty(strResponseStatusDescription) && strResponseStatusDescription.ToLower() == "success")
                        {
                            if (nodes[0].ChildNodes.Item(1).InnerText.ToLower() == "1.0")
                                result = "Success";
                            else
                                result = "Invalid Address Detail";
                        }
                        else
                        {
                            var selectList = new List<SelectedListValues>();
                            string text = string.Empty;
                            foreach (XmlNode crmyNod in nodes)
                            {
                                text = string.Empty;
                                var cNode = crmyNod.ChildNodes.Item(2).ChildNodes;
                                foreach (XmlNode child in cNode)
                                {
                                    if (child.Name.ToLower() == "city")
                                        text = child.InnerText;
                                    else
                                        text += "-" + child.InnerText;
                                }
                                if (crmyNod.ChildNodes.Item(3).Name.ToLower() == "postalcodelowend")
                                {
                                    int Id = 0;
                                    if (int.TryParse(crmyNod.ChildNodes.Item(3).InnerText, out Id) && Id > 0)
                                    {
                                        text += "-" + crmyNod.ChildNodes.Item(3).InnerText;
                                        selectList.Add(new SelectedListValues()
                                        {
                                            Id = Id,
                                            Value = text
                                        });
                                    }
                                }
                                if (crmyNod.ChildNodes.Item(4).Name.ToLower() == "postalcodelowend")
                                {
                                    int Id = 0;
                                    if (int.TryParse(crmyNod.ChildNodes.Item(4).InnerText, out Id) && Id > 0)
                                    {
                                        text += "-" + crmyNod.ChildNodes.Item(4).InnerText;
                                        selectList.Add(new SelectedListValues()
                                        {
                                            Id = Id,
                                            Value = text
                                        });
                                    }
                                }
                            }
                            return Json(selectList, JsonRequestBehavior.AllowGet);
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    System.IO.File.WriteAllText(Server.MapPath("~/UpsErrorLog.txt"), "UPS Error Log : " + DateTime.UtcNow + Environment.NewLine + ex.Message);
                    if (ex.InnerException != null)
                        System.IO.File.AppendAllText(Server.MapPath("~/UpsErrorLog.txt"), ex.InnerException.ToString());
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CityState(string zip)
        {
            string State = "";
            string City = "";
            if (!string.IsNullOrEmpty(zip))
            {
                zip = zip.Trim();
                string Url = "http://www.webservicex.net/uszip.asmx/GetInfoByZIP?USZip=";
                Url = string.Format("{0}{1}", Url, zip);
                GetCS(Url, out State, out City);
            }
            return Json(new { City = City, State = State }, JsonRequestBehavior.AllowGet);
        }

      
        public ActionResult GetRates(Guid OrderId, string zip, bool isCod, bool isSatD)
        {

            var order = db.Orders.Find(OrderId);
            if (order != null && !string.IsNullOrEmpty(zip))
            {
                #region CreateRequest
                string strPayload = string.Empty;
                strPayload = "<?xml version='1.0'?>";
                strPayload += "<AccessRequest xml:lang='en-US'>";
                strPayload += "<AccessLicenseNumber>" + ConfigurationManager.AppSettings["UPSAccessLicenseNumber"] + "</AccessLicenseNumber>";
                strPayload += "<UserId>" + ConfigurationManager.AppSettings["UPSUserId"] + "</UserId>";
                strPayload += "<Password>" + ConfigurationManager.AppSettings["UPSPassword"] + "</Password>";
                strPayload += "</AccessRequest>";
                strPayload += "<?xml version='1.0'?>";
                strPayload += "<RatingServiceSelectionRequest xml:lang='en-US'>";
                strPayload += "<Request>";
                strPayload += "<TransactionReference>";
                strPayload += "<CustomerContext>Rating and Service</CustomerContext>";
                strPayload += "<XpciVersion>1.0001</XpciVersion>";
                strPayload += "</TransactionReference>";
                strPayload += "<RequestAction>Rate</RequestAction>";
                strPayload += "<RequestOption>shop</RequestOption>";
                strPayload += "</Request>";
                strPayload += "<PickupType>";
                strPayload += "<Code>03</Code>";
                strPayload += "</PickupType>";
                strPayload += "<Shipment>";
                strPayload += "<Shipper>";
                strPayload += "<Address>";
                strPayload += "<PostalCode>90723</PostalCode>";
                strPayload += "</Address>";
                strPayload += "</Shipper>";
                strPayload += "<ShipTo>";
                strPayload += "<Address>";
                strPayload += "<PostalCode>" + zip + "</PostalCode>";
                strPayload += "</Address>";
                strPayload += "</ShipTo>";
                strPayload += "<Service>";
                strPayload += "<Code>11</Code>";
                strPayload += "</Service>";
                var boxList = order.Boxes.ToList();
                foreach (var box in boxList)
                {
                    var arr = box.Dimension.Split(':');
                    strPayload += "<Package>";
                    strPayload += "<PackagingType><Code>02</Code><Description>Package</Description>";
                    strPayload += "</PackagingType>";
                    strPayload += "<Dimensions>";
                    strPayload += "<UnitOfMeasurement>";
                    strPayload += "<Code>IN</Code>";
                    strPayload += "</UnitOfMeasurement>";
                    strPayload += "<Length>" + (arr.Length > 0 ? arr[0] : "0") + "</Length>";
                    strPayload += "<Width>" + (arr.Length > 1 ? arr[1] : "0") + "</Width>";
                    strPayload += "<Height>" + (arr.Length > 2 ? arr[2] : "0") + "</Height>";
                    strPayload += "</Dimensions>";
                    strPayload += "<Description>Rate Shopping</Description><PackageWeight>";
                    strPayload += "<UnitOfMeasurement>";
                    strPayload += "<Code>LBS</Code>";
                    strPayload += "</UnitOfMeasurement>";
                    strPayload += "<Weight>" + (!string.IsNullOrEmpty(box.Weight) ? box.Weight : "0") + "</Weight>";
                    strPayload += "</PackageWeight>";
                    strPayload += "</Package>";
                }
                strPayload += "</Shipment>";
                strPayload += "</RatingServiceSelectionRequest>";
                #endregion
                var bytes = Encoding.ASCII.GetBytes(strPayload);
                var client = new WebClient();
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                #region ParseResponse
                try
                {
                    string UpsUrl = ConfigurationManager.AppSettings["UPSRateTest"];
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["UseLiveUPS"]) && ConfigurationManager.AppSettings["UseLiveUPS"] == "1")
                        UpsUrl = ConfigurationManager.AppSettings["UPSRateLive"];
                    var resp = client.UploadData(UpsUrl, "POST", bytes);
                    string xmlResult = Encoding.UTF8.GetString(resp);
                    var doc = new XmlDocument();
                    doc.LoadXml(xmlResult);
                    var nodeList = doc.SelectNodes("RatingServiceSelectionResponse/Response");
                    foreach (XmlNode node in nodeList)
                    {
                        if (!string.IsNullOrEmpty(node.ChildNodes.Item(2).InnerText) && node.ChildNodes.Item(2).InnerText.ToLower() == "failure")
                            return Json(0 , JsonRequestBehavior.AllowGet);
                    }
                    nodeList = doc.SelectNodes("//RatingServiceSelectionResponse/RatedShipment");
                    var selectList = new List<SelectedListValues>();
                    foreach (XmlNode node in nodeList)
                    {
                        string ShipMethod = "";
                        string service = node.ChildNodes.Item(0).InnerText;
                        switch (service)
                        {
                            case "01":
                                ShipMethod = "Ups Next Day Air";
                                break;
                            case "02":
                                ShipMethod = "Ups 2nd day Air";
                                break;
                            case "03":
                                ShipMethod = "Ups Ground";
                                break;
                            case "07":
                                ShipMethod = "Ups Worldwide Express";
                                break;
                            case "08":
                                ShipMethod = "Ups Worldwide Expedited";
                                break;
                            case "11":
                                ShipMethod = "Ups Standard(not available)";
                                break;
                            case "12":
                                ShipMethod = "Ups 3 Days Select";
                                break;
                            case "13":
                                ShipMethod = "Ups Next Day Air Saver";
                                break;
                            case "14":
                                ShipMethod = "Ups Next Day Air Early A.M.";
                                break;
                            case "54":
                                ShipMethod = "Ups Worldwide Express Plus";
                                break;
                            case "59":
                                ShipMethod = "Ups 2nd Day Air A.M.";
                                break;
                            case "65":
                                ShipMethod = "Ups Saver";
                                break;
                            case "82":
                                ShipMethod = "UPS Today Standard";
                                break;
                            case "83":
                                ShipMethod = "UPS Today Dedicated Courier";
                                break;
                            case "84":
                                ShipMethod = "UPS Today Intercity";
                                break;
                            case "85":
                                ShipMethod = "UPS Today Express";
                                break;
                            case "86":
                                ShipMethod = "UPS Today Express Saver";
                                break;
                            default:
                                ShipMethod = string.Empty;
                                break;
                        }
                        string price = node.SelectSingleNode("TotalCharges/MonetaryValue").InnerText;
                        int sId = 0;
                        int.TryParse(service, out sId);
                        if (sId > 0 && !string.IsNullOrEmpty(price) && !string.IsNullOrEmpty(ShipMethod))
                        {
                            if (isSatD)
                            {
                                if (sId == 1 || sId == 2 || sId == 14)
                                    selectList.Add(new SelectedListValues()
                                    {
                                        Id = sId,
                                        Value = "$" + price + "----" + ShipMethod
                                    });
                            }
                            else
                                selectList.Add(new SelectedListValues()
                                {
                                    Id = sId,
                                    Value = "$" + price + "----" + ShipMethod
                                });
                        }
                    }
                    return Json(selectList, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(0 , JsonRequestBehavior.AllowGet);
                }
                #endregion
            }
            return Json(0, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpsShip(Guid Id, string address, int sId, string sText, bool isSatD, bool isCOD)
        {
            var order = db.Orders.Find(Id);
            if (order != null && !string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(sText) && sId > 0)
            {
                var account = db.Accounts.Find(order.AccountId);
                string sPrice = "";
                string sName = "";
                var arr = sText.Split('$');
                if (arr.Length > 1)
                {
                    var arr2 = arr[1].Split('-');
                    if (arr2.Length > 0)
                    {
                        sPrice = arr2[0];
                        sName = arr2.Last();
                    }
                }
                if (account != null && !string.IsNullOrEmpty(sPrice) && !string.IsNullOrEmpty(sName) && !sName.StartsWith("-"))
                {

                    var model = new UpsReview();
                    model.Boxes = order.Boxes.ToList();
                    int count = 10 * order.Boxes.Count();
                    model.Id = order.OrderId;
                    model.AccountId = order.AccountId;
                    model.Name = account.FirstName + " " + account.LastName;
                    model.Phone = string.Empty;
                    if (account.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false) != null)
                        model.Phone = account.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Phone;
                    model.CompanyName = model.Name;
                    if (account.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false) != null)
                        model.CompanyName = account.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name;
                    if (sId < 10)
                        model.sId = "0" + sId;
                    else
                        model.sId = sId.ToString();
                    if (isCOD)
                        model.Price = (Convert.ToDecimal(sPrice) + count).ToString();
                    else
                        model.Price = sPrice;
                    model.ServiceName = sName;
                    model.isCod = isCOD;
                    model.isSatD = isSatD;
                    model.ShipAddress = address;
                    model.ShipFrom = ConfigurationManager.AppSettings["ShipFrom"];
                    model.lNo = ConfigurationManager.AppSettings["UPSAccessLicenseNumber"];
                    model.UN = ConfigurationManager.AppSettings["UPSUserId"];
                    model.PW = ConfigurationManager.AppSettings["UPSPassword"];
                    ViewBag.OrderNumber = order.OrderNumber;
                    ViewBag.OrderId = order.OrderId;
                    Session["UpsModel"] = model;
                    return PartialView(model);
                }
            }
            return PartialView();
        }

        [HttpPost]
        public ActionResult UpsFinal(string OrderNumber)
        {
            var model = new UpsFinal() { isSuccess = false };
            if (Session["UpsModel"] != null)
            {
                var retModel = (UpsReview)Session["UpsModel"];
                var arr = retModel.ShipAddress.Split(',');
                var order = db.Orders.Find(retModel.Id);
                if (order != null)
                {
                    string zipcode = string.Empty;
                    string state = string.Empty;
                    string city = string.Empty;
                    string address = string.Empty;
                    if (arr.Length > 3)
                        zipcode = arr[3];
                    if (arr.Length > 2)
                        state = arr[2];
                    if (arr.Length > 1)
                        city = arr[1];
                    if (arr.Length > 0)
                        address = arr[0];
                    DataTable dt = new DataTable();
                    dt.Columns.Add("Id");
                    dt.Columns.Add("Name");
                    dt.Columns.Add("Dimension");
                    dt.Columns.Add("Weight");
                    dt.Columns.Add("OrderId");
                    foreach (var box in retModel.Boxes)
                    {
                        DataRow dr = dt.NewRow();
                        dr["Id"] = box.Id;
                        dr["Name"] = box.Name;
                        dr["Dimension"] = box.Dimension;
                        dr["Weight"] = box.Weight;
                        dr["OrderId"] = box.OrderId;
                        dt.Rows.Add(dr);
                    }

                    dt.AcceptChanges();
                    string Error = "The COD option is unavailable with the selected service, UPS account type, and/or with the shipments origin";
                    string res = ShipClient.MyFunction(retModel.lNo, retModel.UN, retModel.PW, retModel.sId, address, city, state, zipcode, "02", dt, retModel.CompanyName, retModel.Name, retModel.Phone, retModel.isCod, retModel.isSatD);
                    if (res.Contains(Error))
                    {
                        model.Message = Error;
                        System.IO.File.WriteAllText(Server.MapPath("~/UpsErrorLog.txt"), "UPS Error Log : " + DateTime.UtcNow + Environment.NewLine + res);
                        System.IO.File.AppendAllText(Server.MapPath("~/UpsErrorLog.txt"), res);
                        return PartialView(model);
                    }

                    if (!string.IsNullOrEmpty(res))
                    {
                        if (res.Contains("Success"))
                        {
                            var resArr = res.Split('-');
                            try
                            {
                                if (resArr.Length > 1)
                                {
                                    var resArr2 = resArr[1].Split('@');
                                    if (resArr2.Length > 1)
                                    {
                                        var track = new Track();
                                        track.Id = 0;
                                        track.ShippingCost = decimal.Parse(retModel.Price);
                                        track.ShipViaId = order.ShipViaId.Value;
                                        track.TrackingNumber = resArr2[0];
                                        db.Tracks.Add(track);
                                        db.SaveChanges();

                                        order.StatusId = (int)OrderMode.Shipped;
                                        order.ShippedOn = DateTime.UtcNow;
                                        order.ShippingCost = decimal.Parse(retModel.Price);
                                        order.TrackId = track.Id;
                                        order.DateUpdated = DateTime.UtcNow;
                                        db.SaveChanges();

                                        model.file = resArr2[1];
                                        model.Price = retModel.Price;
                                        model.sNo = resArr2[0];
                                        model.isSuccess = true;
                                        byte[] bytes = Convert.FromBase64String(resArr2[1]);
                                        string path = ConfigurationManager.AppSettings["UPSLabel"];

                                        MemoryStream ms = new MemoryStream(bytes, 0,
                                          bytes.Length);

                                        ms.Write(bytes, 0, bytes.Length);
                                        Image image = Image.FromStream(ms, true);
                                        string fileName = OrderNumber;
                                        if (ImageFormat.Jpeg.Equals(image.RawFormat))
                                            fileName += "." + ImageFormat.Jpeg;
                                        else if (ImageFormat.Gif.Equals(image.RawFormat))
                                            fileName += "." + ImageFormat.Gif;
                                        else if (ImageFormat.Png.Equals(image.RawFormat))
                                            fileName += "." + ImageFormat.Png;
                                        //image.Save(Server.MapPath(path + fileName));
                                        if (System.IO.File.Exists((path) + fileName))
                                            System.IO.File.Delete(HostingEnvironment.MapPath(path) + fileName);
                                        else
                                            System.IO.File.WriteAllBytes(HostingEnvironment.MapPath(path) + fileName, bytes);
                                        model.Message = "The transaction was sucessful.";
                                        return PartialView(model);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.WriteAllText(Server.MapPath("~/UpsErrorLog.txt"), "UPS Error Log : " + DateTime.UtcNow + Environment.NewLine + ex.Message);
                                if(ex.InnerException!=null)
                                    System.IO.File.AppendAllText(Server.MapPath("~/UpsErrorLog.txt"), ex.InnerException.ToString());
                            }
                        }
                    }
                }
            }
            model.Message = "An error occured.";
            return PartialView(model);
        }

        [NonAction]
        public void GetCS(string url, out string State, out string City)
        {
            State = string.Empty;
            City = string.Empty;
            try
            {
                var doc = new XmlDocument();
                doc.Load(url);
                var nList = doc.GetElementsByTagName("Table");
                if (nList != null)
                {
                    if (nList.Count > 0)
                    {
                        var cList = nList[0].ChildNodes;
                        foreach (System.Xml.XmlNode child in cList)
                        {
                            if (child.Name.ToLower() == "city")
                                City = child.InnerText;
                            if (child.Name.ToLower() == "state")
                                State = child.InnerText;
                            if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(State))
                                break;
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        public ActionResult Print(Guid Id, string pType)
        {
            var order = db.Orders.Find(Id);
            if (!string.IsNullOrEmpty(pType) && order != null)
            {
                pType = pType.Trim().ToLower();
                string Message = "";
                string hP = pType == "packlist" ? "hP" : "";
                switch (order.StatusId)
                {
                    case 1:
                    case 2:
                        Message = "Purchase Order";
                        break;
                    case 3:
                    case 6:
                        Message = pType == "packlist" ? "Packing List" : (pType == "printpo" ? "OG Purchase Order" : "Final Order");
                        break;
                    case 4:
                        Message = "Back Order";
                        break;
                    case 7:
                        Message = "Open Order";
                        break;
                    default:
                        Message = "";
                        break;
                }

                //string URL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id + "?Message=" + Message + "&HidePrice=" + hP;
                //byte[] toPdf = PlatiniWebService.CreatePdf(URL);
                //string ContentType = "application/pdf";
                //string FileName = "Cart.pdf";
                //Response.AppendHeader("Content-Disposition", "inline;filename=" + FileName);
                //return File(toPdf, ContentType);
                return RedirectToAction("PrintThisCart", "Home", new { @area = "", @Id = Id, @Message = Message, @HidePrice = hP });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult SendMail(string ids, string Subject, string Message, string sI)
        {
            if (!string.IsNullOrEmpty(ids) && !string.IsNullOrEmpty(sI))
            {
                var emails = ids.Split(',').ToList();
                foreach (var email in emails)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^([\w-\.]+@([\w-]+\.)+[\w-]{2,4})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        emails.Remove(email);
                }
                if (emails.Count > 0)
                {
                    var values = sI.Trim(',').Split(',').Select(x => Guid.Parse(x));
                    foreach (var Id in values)
                    {
                        var order = db.Orders.Find(Id);
                        if (order != null)
                        {
                            string message = "";
                            switch (order.StatusId)
                            {
                                case 1:
                                case 2:
                                    message = "Purchase Order";
                                    break;
                                case 3:
                                case 6:
                                    message = "Final Order";
                                    break;
                                case 4:
                                    message = "Back Order";
                                    break;
                                case 7:
                                    message = "Open Order";
                                    break;
                                default:
                                    message = "";
                                    break;
                            }
                            string URL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id + "?Message=" + message;
                            byte[] toPdf = PlatiniWebService.CreatePdf(URL);
                            string replayMail = ConfigurationManager.AppSettings["SMTPEmail"].ToString();
                            //if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles ==RolesEnum.SalesPerson.ToString())
                            //    replayMail= SiteIdentity.UserName;
                            EmailManager.SendLineSheet(Subject + "( Order " + order.OrderNumber + ")", Message, toPdf, replayMail, emails.ToArray());
                        }
                    }
                }
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("The email could not be sent.  Please try again.", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SendList(Guid Id, string ids)
        {
            if (!string.IsNullOrEmpty(ids))
            {
                var emails = ids.Split(',').ToList();
                foreach (var email in emails)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^([\w-\.]+@([\w-]+\.)+[\w-]{2,4})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        emails.Remove(email);
                }
                if (emails.Count > 0)
                {
                    var order = db.Orders.Find(Id);
                    if (order != null)
                    {
                        string URL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id + "?Message=" + "Packing List" + "&hp=1";
                        byte[] toPdf = PlatiniWebService.CreatePdf(URL);
                        EmailManager.SendLineSheet(" Order Details PDF ( Order " + order.OrderNumber + ")", "", toPdf, ConfigurationManager.AppSettings["SMTPEmail"].ToString(), emails.ToArray());
                    }
                }
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("The email could not be sent.  Please try again.", JsonRequestBehavior.AllowGet);
        }

        #region QB
        string productdesc = string.Empty;

        private void generateInvoice(Guid orderId, int accountId)
        {
            QuickBookStrings.LoadQuickBookStrings(FailureFrom.Invoice.ToString());
            string Type = FailureFrom.Invoice.ToString();
            var oauthValidator = new OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
            ServiceContext contextNew = new ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, IntuitServicesType.QBO, oauthValidator);
            contextNew.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;
            contextNew.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = GetLoc(Type, accountId.ToString());
            DataService service = new DataService(contextNew);
            Order ord = db.Orders.Where(x => x.OrderId == orderId).FirstOrDefault();

            //contextNew.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = Database.GetPath(SiteConfiguration.FailureFrom.Invoice.ToString(), orderId);
            try
            {
                var customer = db.Accounts.Find(accountId);
                var order = db.Orders.Find(orderId);
                decimal TotalCost = 0;
                string ShippingVia = string.Empty;
                string ShippingCost = string.Empty;
                string TrackingNumber = string.Empty;
                string TermName = string.Empty;
                string ShippedOn = order.ShippedOn.HasValue ? order.ShippedOn.Value.ToString() : "";
                string CompanyName = customer.Companies.FirstOrDefault() != null ? (!string.IsNullOrEmpty(customer.Companies.FirstOrDefault().Name) 
                    ? customer.Companies.FirstOrDefault().Name : customer.UserName) : customer.UserName;
                CompanyName = CompanyName.Replace("'", "");
                CompanyName = CompanyName.Replace(":", " ");
                var billingAddress = customer.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                var shippingAddress = customer.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                List<CustomerItemPrice> Prices = new List<CustomerItemPrice>(); ;
                if (customer != null)
                    Prices = customer.CustomerItemPrices.ToList();
                if (customer.CustomerOptionalInfoes.FirstOrDefault() != null)
                {
                    var info = customer.CustomerOptionalInfoes.FirstOrDefault();
                    if (info.ShipViaId.HasValue)
                    {
                        var shipping = db.ShipVias.Find(info.ShipViaId.Value);
                        if (shipping != null)
                            ShippingVia = shipping.Name;
                    }
                    if (info.Term != null)
                        TermName = info.Term.Name;
                }
                if (order.Track != null)
                {
                    ShippingCost = order.Track.ShippingCost.HasValue ? order.Track.ShippingCost.Value.ToString() : "0";
                    TrackingNumber = !string.IsNullOrEmpty(order.Track.TrackingNumber) ? order.Track.TrackingNumber : "0";
                }
                Intuit.Ipp.Data.Invoice invoice = new Invoice();
                List<Intuit.Ipp.Data.Line> objLineList = new List<Line>();
                Customer ObjCustomer = new Customer();
                Intuit.Ipp.Data.Term salesterm = null;

                QueryService<Intuit.Ipp.Data.Term> termQueryService = new QueryService<Intuit.Ipp.Data.Term>(contextNew);
                if (!string.IsNullOrEmpty(TermName))
                    salesterm = termQueryService.ExecuteIdsQuery("Select * From Term Where Name='" + TermName + "'").FirstOrDefault();

                QueryService<Customer> account = new QueryService<Customer>(contextNew);
                if (!String.IsNullOrEmpty(CompanyName))
                {
                    ObjCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompanyName.Trim() + "'").FirstOrDefault();
                    if (ObjCustomer == null)
                    {
                        Customer objAddCustomer = new Customer();
                        objAddCustomer.GivenName = customer.FirstName;
                        objAddCustomer.FamilyName = customer.LastName;
                        objAddCustomer.CompanyName = CompanyName;
                        objAddCustomer.DisplayName = CompanyName;
                        objAddCustomer.PrimaryPhone = new TelephoneNumber() { FreeFormNumber = customer.Communications.FirstOrDefault() != null ? customer.Communications.FirstOrDefault().Phone : "" };
                        objAddCustomer.PrimaryEmailAddr = new EmailAddress() { Address = customer.Email };
                        if (billingAddress != null)
                            objAddCustomer.BillAddr = new PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State };
                        if (shippingAddress != null)
                            objAddCustomer.ShipAddr = new PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State };
                        objAddCustomer.Fax = new TelephoneNumber() { AreaCode = "01", FreeFormNumber = customer.Communications.FirstOrDefault() != null ? customer.Communications.FirstOrDefault().Fax : "" };
                        if (!(salesterm == null))
                            objAddCustomer.SalesTermRef = new ReferenceType() { name = salesterm.Name, Value = salesterm.Id };
                        service.Add(objAddCustomer);
                        ObjCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompanyName.Trim() + "'").FirstOrDefault();
                    }
                }
                if (ObjCustomer != null)
                {
                    Invoice ObjInvoice = new Invoice();
                    string DocNumber = string.Empty;                    
                    DocNumber = order.OrderNumber.ToString();
                    QueryService<Invoice> InvoiceChk = new QueryService<Invoice>(contextNew);
                    ObjInvoice = InvoiceChk.ExecuteIdsQuery("Select * FROM Invoice WHERE DocNumber='" + DocNumber.Trim() + "'").FirstOrDefault();
                    if (ObjInvoice != null)
                    {
                        ObjInvoice.TxnDate = DateTime.UtcNow.Date;
                        var clothes = new List<Cloth>(order.OrderScales.Count + order.OrderSizes.Count);
                        clothes.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                        clothes.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                        clothes = clothes.Distinct().ToList();
                        foreach (var cloth in clothes)
                        {
                            AddProductToQuickBook(cloth.ClothesId);
                            Line invoiceLine = new Line();
                            var productService = new QueryService<Item>(contextNew);
                            var Objstyle = new Item();
                            Objstyle = productService.ExecuteIdsQuery("Select * From Item where Name='" + cloth.StyleNumber + "'").FirstOrDefault();
                            if (Objstyle != null)
                            {
                                invoiceLine.Id = Objstyle.Id;
                                invoiceLine.Description = GetInvoiceNames(cloth, order);
                                int? Quantity = 0; int? QriQty = 0;
                                decimal? Price = 0;
                                QriQty = order.OrderScales.Where(x => x.ClothesScale.ClothesId == cloth.ClothesId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity) * x.Quantity);
                                Quantity = QriQty.HasValue ? QriQty.Value : 0;
                                foreach (var ordSize in cloth.ClothesScales)
                                {
                                    QriQty = order.OrderSizes.Where(x => x.ClothesScaleSize.ClothesScaleId == ordSize.ClothesScaleId).Sum(x => x.Quantity);
                                    Quantity += QriQty.HasValue ? QriQty.Value : 0;
                                }                              
                                if (Prices.Count > 0)
                                    Price = Prices.FirstOrDefault(x => x.ClothesId == cloth.ClothesId) != null ? Prices.FirstOrDefault(x => x.ClothesId == cloth.ClothesId).Price : (cloth.Price != null ? cloth.Price : 0.0m);
                                else
                                    Price = cloth.Price;
                                invoiceLine.Amount = (Price.HasValue ? Price.Value : 0.0m) * (Quantity.HasValue ? Quantity.Value : 0);
                                SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
                                lineSalesItemLineDetail.ItemRef = new ReferenceType()
                                {
                                    name = Objstyle.Name,
                                    Value = Objstyle.Id
                                };
                                TotalCost += Price.HasValue ? Price.Value : 0;
                                lineSalesItemLineDetail.AnyIntuitObject = Price.HasValue ? Price.Value : 0;
                                lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
                                lineSalesItemLineDetail.Qty = Quantity.HasValue ? Quantity.Value : 0;
                                lineSalesItemLineDetail.QtySpecified = true;
                                lineSalesItemLineDetail.TaxCodeRef = new ReferenceType()
                                {
                                    Value = "TAX"
                                };
                                lineSalesItemLineDetail.ServiceDate = DateTime.UtcNow.Date;
                                lineSalesItemLineDetail.ServiceDateSpecified = true;
                                invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;
                                invoiceLine.AmountSpecified = true;
                                invoiceLine.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                                invoiceLine.DetailTypeSpecified = true;
                                objLineList.Add(invoiceLine);
                            }
                        }
                        if (order.Discount.HasValue ? (order.Discount.Value > 0) : false)
                        {
                            DiscountLineDetail obj = new DiscountLineDetail();                            
                            obj.DiscountPercent = order.Discount.Value;
                            obj.PercentBased = true;
                            obj.PercentBasedSpecified = true;
                            obj.DiscountPercentSpecified = true;
                            Line objDis = new Line();
                            objDis.DetailType = LineDetailTypeEnum.DiscountLineDetail;
                            objDis.DetailTypeSpecified = true;
                            objDis.AnyIntuitObject = obj;
                            objLineList.Add(objDis);
                        }
                        if (billingAddress != null)
                        {
                            PhysicalAddress billAddr = new PhysicalAddress();
                            billAddr.Line1 = !string.IsNullOrEmpty(billingAddress.Street) ? billingAddress.Street : "";
                            billAddr.City = !string.IsNullOrEmpty(billingAddress.City) ? billingAddress.City : "";
                            billAddr.Country = "United States";
                            billingAddress.State = !string.IsNullOrEmpty(billingAddress.State) ? billingAddress.State : "";
                            billAddr.PostalCode = !string.IsNullOrEmpty(billingAddress.Pincode) ? billingAddress.Pincode : "";
                            ObjInvoice.BillAddr = billAddr;
                            ObjInvoice.BillEmail = ObjCustomer.PrimaryEmailAddr;
                        }
                        if (shippingAddress != null)
                        {
                            PhysicalAddress billAddr = new PhysicalAddress();
                            billAddr.Line1 = !string.IsNullOrEmpty(shippingAddress.Street) ? shippingAddress.Street : "";
                            billAddr.City = !string.IsNullOrEmpty(shippingAddress.City) ? shippingAddress.City : "";
                            billAddr.Country = "United States";
                            billingAddress.State = !string.IsNullOrEmpty(shippingAddress.State) ? shippingAddress.State : "";
                            billAddr.PostalCode = !string.IsNullOrEmpty(shippingAddress.Pincode) ? shippingAddress.Pincode : "";
                            ObjInvoice.ShipAddr = billAddr;
                        }                       
                        if (salesterm != null)
                            ObjInvoice.SalesTermRef = new ReferenceType() { name = salesterm.Name, Value = salesterm.Id };
                        if (!string.IsNullOrEmpty(order.Note))
                        {
                            if (order.Note.Length > 999)
                                ObjInvoice.CustomerMemo = new MemoRef() { Value = order.Note.Substring(0, 996) + "..." };
                            else
                                ObjInvoice.CustomerMemo = new MemoRef() { Value = order.Note };
                        }
                        if (!string.IsNullOrEmpty(ShippingVia))
                        {
                            ObjInvoice.ShipMethodRef = new ReferenceType() { name = ShippingVia, Value = ShippingVia };
                        }

                        if (!string.IsNullOrEmpty(TrackingNumber))
                        {
                            ObjInvoice.TrackingNum = TrackingNumber;
                        }                       
                        if (!string.IsNullOrEmpty(ShippedOn))
                        {
                            ObjInvoice.ShipDate = Convert.ToDateTime(ShippedOn);
                            ObjInvoice.ShipDateSpecified = true;
                        }
                        if (!string.IsNullOrEmpty(ShippingCost))
                        {
                            SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
                            lineSalesItemLineDetail.ItemRef = new ReferenceType() { Value = "SHIPPING_ITEM_ID" };

                            Line invoiceLineShipCost = new Line();
                            decimal shipCost = !string.IsNullOrEmpty(ShippingCost) ? Convert.ToDecimal(ShippingCost) : 0;
                            invoiceLineShipCost.Amount = Convert.ToDecimal(shipCost);
                            TotalCost += shipCost;
                            invoiceLineShipCost.AmountSpecified = true;
                            invoiceLineShipCost.AnyIntuitObject = lineSalesItemLineDetail;
                            invoiceLineShipCost.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                            invoiceLineShipCost.DetailTypeSpecified = true;
                            objLineList.Add(invoiceLineShipCost);
                        }
                        ObjInvoice.Line = objLineList.ToArray();
                        ObjInvoice.DueDate = DateTime.Now.AddDays(30).Date;
                        ObjInvoice.DueDateSpecified = true;
                        Invoice invoiceAdded = service.Update<Invoice>(ObjInvoice);
                    }
                    else
                    {
                        ObjInvoice = new Invoice();
                        string customerId = ObjCustomer != null ? ObjCustomer.Id : customer.Companies.FirstOrDefault().CompanyId.ToString();
                        ObjInvoice.CustomerRef = new ReferenceType() { Value = ObjCustomer.Id };
                        ObjInvoice.AutoDocNumber = false;
                        ObjInvoice.AutoDocNumberSpecified = true;

                        ObjInvoice.DocNumber = order.OrderNumber.ToString();
                        ObjInvoice.TxnDate = DateTime.UtcNow.Date;
                        ObjInvoice.TxnDateSpecified = true;                        
                        var clothes = new List<Cloth>(order.OrderScales.Count + order.OrderSizes.Count);
                        clothes.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                        clothes.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                        clothes = clothes.Distinct().ToList();
                        foreach (var cloth in clothes)
                        {
                            AddProductToQuickBook(cloth.ClothesId);
                            Line invoiceLine = new Line();
                            var productService = new QueryService<Item>(contextNew);
                            var Objstyle = new Item();
                            Objstyle = productService.ExecuteIdsQuery("Select * From Item where Name='" + cloth.StyleNumber + "'").FirstOrDefault();
                            if (Objstyle != null)
                            {
                                invoiceLine.Id = Objstyle.Id;
                                invoiceLine.Description = GetInvoiceNames(cloth, order);
                                int? Quantity = 0; int? QriQty = 0;
                                decimal? Price = 0;
                                QriQty = order.OrderScales.Where(x => x.ClothesScale.ClothesId == cloth.ClothesId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity) * x.Quantity);
                                Quantity = QriQty.HasValue ? QriQty.Value : 0;
                                foreach (var ordSize in cloth.ClothesScales)
                                {
                                    QriQty = order.OrderSizes.Where(x => x.ClothesScaleSize.ClothesScaleId == ordSize.ClothesScaleId).Sum(x => x.Quantity);
                                    Quantity += QriQty.HasValue ? QriQty.Value : 0;
                                }                                
                                if (Prices.Count > 0)
                                    Price = Prices.FirstOrDefault(x => x.ClothesId == cloth.ClothesId) != null ? Prices.FirstOrDefault(x => x.ClothesId == cloth.ClothesId).Price : (cloth.Price != null ? cloth.Price : 0.0m);
                                else
                                    Price = cloth.Price;
                                invoiceLine.Amount = (Price.HasValue ? Price.Value : 0.0m) * (Quantity.HasValue ? Quantity.Value : 0);
                                SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
                                lineSalesItemLineDetail.ItemRef = new ReferenceType()
                                {
                                    name = Objstyle.Name,
                                    Value = Objstyle.Id
                                };
                                TotalCost += Price.HasValue ? Price.Value : 0;
                                lineSalesItemLineDetail.AnyIntuitObject = Price.HasValue ? Price.Value : 0;
                                lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
                                lineSalesItemLineDetail.Qty = Quantity.HasValue ? Quantity.Value : 0;
                                lineSalesItemLineDetail.QtySpecified = true;                                
                                lineSalesItemLineDetail.TaxCodeRef = new ReferenceType()
                                {
                                    Value = "NON"
                                };
                                lineSalesItemLineDetail.ServiceDate = DateTime.UtcNow.Date;
                                lineSalesItemLineDetail.ServiceDateSpecified = true;
                                invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;
                                invoiceLine.AmountSpecified = true;                                
                                invoiceLine.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                                invoiceLine.DetailTypeSpecified = true;
                                objLineList.Add(invoiceLine);
                            }
                        }
                        if (order.Discount.HasValue ? (order.Discount.Value > 0) : false)
                        {
                            DiscountLineDetail obj = new DiscountLineDetail();
                            obj.DiscountPercent = order.Discount.Value;
                            obj.PercentBased = true;
                            obj.PercentBasedSpecified = true;
                            obj.DiscountPercentSpecified = true;
                            Line objDis = new Line();
                            objDis.DetailType = LineDetailTypeEnum.DiscountLineDetail;
                            objDis.DetailTypeSpecified = true;
                            objDis.AnyIntuitObject = obj;
                            objLineList.Add(objDis);
                        }
                        if (billingAddress != null)
                        {
                            PhysicalAddress billAddr = new PhysicalAddress();
                            billAddr.Line1 = !string.IsNullOrEmpty(billingAddress.Street) ? billingAddress.Street : "";
                            billAddr.City = !string.IsNullOrEmpty(billingAddress.City) ? billingAddress.City : "";
                            billAddr.Country = "United States";
                            billingAddress.State = !string.IsNullOrEmpty(billingAddress.State) ? billingAddress.State : "";
                            billAddr.PostalCode = !string.IsNullOrEmpty(billingAddress.Pincode) ? billingAddress.Pincode : "";
                            ObjInvoice.BillAddr = billAddr;
                            ObjInvoice.BillEmail = ObjCustomer.PrimaryEmailAddr;
                        }
                        if (shippingAddress != null)
                        {
                            PhysicalAddress billAddr = new PhysicalAddress();
                            billAddr.Line1 = !string.IsNullOrEmpty(shippingAddress.Street) ? shippingAddress.Street : "";
                            billAddr.City = !string.IsNullOrEmpty(shippingAddress.City) ? shippingAddress.City : "";
                            billAddr.Country = "United States";
                            billingAddress.State = !string.IsNullOrEmpty(shippingAddress.State) ? shippingAddress.State : "";
                            billAddr.PostalCode = !string.IsNullOrEmpty(shippingAddress.Pincode) ? shippingAddress.Pincode : "";
                            ObjInvoice.ShipAddr = billAddr;
                        }                        
                        if (salesterm != null)
                            ObjInvoice.SalesTermRef = new ReferenceType() { name = salesterm.Name, Value = salesterm.Id };
                        if (!string.IsNullOrEmpty(order.Note))
                        {
                            if (order.Note.Length > 999)
                                ObjInvoice.CustomerMemo = new MemoRef() { Value = order.Note.Substring(0, 996) + "..." };
                            else
                                ObjInvoice.CustomerMemo = new MemoRef() { Value = order.Note };
                        }
                        if (!string.IsNullOrEmpty(ShippingVia))
                        {
                            ObjInvoice.ShipMethodRef = new ReferenceType() { name = ShippingVia, Value = ShippingVia };
                        }

                        if (!string.IsNullOrEmpty(TrackingNumber))
                        {
                            ObjInvoice.TrackingNum = TrackingNumber;
                        }                        
                        if (!string.IsNullOrEmpty(ShippedOn))
                        {
                            ObjInvoice.ShipDate = Convert.ToDateTime(ShippedOn);
                            ObjInvoice.ShipDateSpecified = true;
                        }
                        if (!string.IsNullOrEmpty(ShippingCost))
                        {
                            SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
                            lineSalesItemLineDetail.ItemRef = new ReferenceType() { Value = "SHIPPING_ITEM_ID" };

                            Line invoiceLineShipCost = new Line();
                            decimal shipCost = !string.IsNullOrEmpty(ShippingCost) ? Convert.ToDecimal(ShippingCost) : 0;
                            invoiceLineShipCost.Amount = Convert.ToDecimal(shipCost);
                            TotalCost += shipCost;
                            invoiceLineShipCost.AmountSpecified = true;
                            invoiceLineShipCost.AnyIntuitObject = lineSalesItemLineDetail;
                            invoiceLineShipCost.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                            invoiceLineShipCost.DetailTypeSpecified = true;
                            objLineList.Add(invoiceLineShipCost);
                        }
                        ObjInvoice.TxnTaxDetail = new TxnTaxDetail() { TotalTax = 0 };
                        ObjInvoice.Line = objLineList.ToArray();
                        ObjInvoice.DueDate = DateTime.Now.AddDays(30).Date;
                        ObjInvoice.DueDateSpecified = true;

                        Invoice invoiceAdded = service.Add<Invoice>(ObjInvoice);
                        TotalCost = 0;
                    }
                }
            }

            catch (Exception ex) { }
        }

        public bool AddProductToQuickBook(int ClothesId)
        {
            QuickBookStrings.LoadQuickBookStrings(FailureFrom.Product.ToString());
            string StyleNumber = string.Empty;
            string Type = FailureFrom.Product.ToString();
            var oauthValidator = new OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
            QuickBookFailureRecord existFailure = null;
            if (ClothesId > 0)
                existFailure = db.QuickBookFailureRecords.FirstOrDefault(x => x.FailureFrom.ToLower() == Type.ToLower() && x.FailureFromId == ClothesId);
            try
            {
                var context = new ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, IntuitServicesType.QBO, oauthValidator);
                context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = GetLoc(Type, ClothesId.ToString());
                context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;
                var service = new DataService(context);
                var productService = new QueryService<Item>(context);
                var newProduct = new Item();
                var dbCloth = db.Clothes.Find(ClothesId);
                if (existFailure != null)
                    StyleNumber = existFailure.FailureOriginalValue;
                else if (dbCloth != null)
                {
                    if (!string.IsNullOrEmpty(dbCloth.StyleNumber) && dbCloth.IsActive == true && dbCloth.IsDelete == false)
                        StyleNumber = dbCloth.StyleNumber;
                }
                if (!string.IsNullOrEmpty(StyleNumber) && dbCloth != null)
                {
                    newProduct = productService.ExecuteIdsQuery("Select * From Item where Name='" + StyleNumber + "'").FirstOrDefault();
                    if (newProduct != null) return false;
                    if (newProduct == null)
                    {
                        newProduct = new Item();
                        newProduct.SpecialItem = true;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.Name = StyleNumber;
                        newProduct.FullyQualifiedName = GetNames(dbCloth, 1);
                        newProduct.Active = true;
                        newProduct.PurchaseDesc = GetNames(dbCloth, 2);                        
                        string str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                        string noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();                        
                        newProduct.Description = noHTML;
                        newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                        newProduct.PurchaseCostSpecified = true;                                              
                        newProduct.ActiveSpecified = true;
                        newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                           + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        newProduct.QtyOnHandSpecified = true;
                        //newProduct.Type = ItemTypeEnum.NonInventory;
                        newProduct.TrackQtyOnHand = true;
                        newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                        newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                        newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };                        
                        service.Add<Item>(newProduct);
                    }
                    else
                    {                       
                        newProduct.SpecialItem = true;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.Name = StyleNumber;
                        newProduct.FullyQualifiedName = GetNames(dbCloth, 1);
                        newProduct.Active = true;
                        newProduct.PurchaseDesc = GetNames(dbCloth, 2);                        
                        string str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                        string noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                        newProduct.Description = noHTML;
                        newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                        newProduct.PurchaseCostSpecified = true;                     
                        newProduct.ActiveSpecified = true;
                        newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                          + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        newProduct.QtyOnHandSpecified = true;
                        //newProduct.Type = ItemTypeEnum.NonInventory;
                        newProduct.TrackQtyOnHand = true;
                        newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                        newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                        newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };                       
                        service.Update<Item>(newProduct);

                        var newItem = new Item();
                        newItem = productService.ExecuteIdsQuery("Select * From Item where Name='" + dbCloth.StyleNumber + "'").FirstOrDefault();
                        if (newItem == null)
                        {
                            newItem = new Item();
                            newItem.SpecialItem = true;
                            newItem.UnitPriceSpecified = true;
                            newItem.Name = dbCloth.StyleNumber;
                            newItem.FullyQualifiedName = GetNames(dbCloth, 1);
                            newItem.PurchaseDesc = GetNames(dbCloth, 2);
                            str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                            noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                            newItem.Description = noHTML;
                            newItem.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                            newItem.UnitPriceSpecified = true;
                            newItem.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                            newItem.PurchaseCostSpecified = true;
                            newItem.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                            + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                            newProduct.QtyOnHandSpecified = true;
                            newItem.ActiveSpecified = true;
                            newItem.InvStartDate = DateTime.UtcNow;
                            newItem.InvStartDateSpecified = true;
                            //newItem.Type = ItemTypeEnum.NonInventory;                           
                            newItem.TrackQtyOnHand = true;
                            newItem.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                            newItem.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                            newItem.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };
                            service.Add<Item>(newItem);
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        [NonAction]
        public string GetNames(Cloth dbCloth, int Type)
        {
            string retString = string.Empty;
            Category type = null;
            Category subCat = null;
            Category cat = null;
            type = db.Categories.Find(dbCloth.CategoryId);
            if (type != null)
            {
                subCat = db.Categories.Find(type.ParentId);
                if (subCat != null)
                    cat = db.Categories.Find(subCat.ParentId);
            }
            if (Type == 1)
                retString = (cat != null ? "Category : " + cat.Name + " \n " : "") + (subCat != null ? "SubCategory : " + subCat.Name + " \n " : "") + (type != null ? "Type : " + type.Name : "") + " \n ";
            else if (Type == 2)
            {

                retString = (cat != null ? "Category : " + cat.Name + " \n " : "") + (subCat != null ? "SubCategory : " + subCat.Name + " \n " : "") + (type != null ? "Type : " + type.Name : "") + " \n ";
                retString += (!string.IsNullOrEmpty(dbCloth.Color) ? "Color : " + dbCloth.Color : "");
                //if (dbCloth.FutureDeliveryDate.HasValue ? dbCloth.FutureDeliveryDate.Value != DateTime.MinValue : false)
                //    retString += "Future Product : Yes \n\n ";
                //else
                //    retString += "Future Product : No \n\n ";
                //var fitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                //var inseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                //List<int?> dbFitList = dbCloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                //List<int?> dbInseamList = dbCloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();
                //if (dbFitList.Count == 0)
                //    dbFitList.Add(null);
                //if (dbInseamList.Count == 0)
                //    dbInseamList.Add(null);
                //foreach (var fitid in dbFitList)
                //{
                //    foreach (var inseamid in dbInseamList)
                //    {
                //        var scaleList = dbCloth.ClothesScales;
                //        var prepackList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false);
                //        if (prepackList.Count() > 0)
                //        {
                //            foreach (var pack in prepackList)
                //            {
                //                string SizeQuant = string.Empty;
                //                var fit = fitList.FirstOrDefault(x => x.Id == fitid);
                //                var inseam = fitList.FirstOrDefault(x => x.Id == fitid);
                //                var openSize = scaleList.FirstOrDefault(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true);
                //                if (fit != null)
                //                    SizeQuant = "Fit: " + fit.Value + " \n ";
                //                if(pack.Name!=null)
                //                    SizeQuant += pack.Name.ToUpper().Replace("SCALE", "") + " : " + (pack.InvQty.HasValue ? pack.InvQty.Value : 0) + "  \n ";
                //                if (inseam != null)
                //                    SizeQuant += "Inseam: " + inseam.Value + " ";
                //                if (openSize != null)
                //                {
                //                    List<string> sizeStrings = new List<string>();
                //                    foreach (var size in openSize.ClothesScaleSizes)
                //                    {
                //                        if (size.Size != null)
                //                            sizeStrings.Add(size.Size.Name + " : " + (size.Quantity.HasValue ? size.Quantity.Value : 0));
                //                    }
                //                    if (sizeStrings.Count > 0)
                //                        SizeQuant += "Size: " + string.Join(" ,", sizeStrings);
                //                    SizeQuant += " \n ";
                //                }
                //                retString += SizeQuant;
                //            }
                //        }
                //    }
                //}
            }
            else
            {

            }
            if (!string.IsNullOrEmpty(retString) && retString.Length > 999)
                retString = retString.Substring(0, 996) + "...";
            return retString;
        }

        [NonAction]
        public string GetInvoiceNames(Cloth dbCloth, Order dbOrder)
        {
            string retString = string.Empty;
            Category type = null;
            Category subCat = null;
            Category cat = null;
            type = db.Categories.Find(dbCloth.CategoryId);
            if (type != null)
            {
                subCat = db.Categories.Find(type.ParentId);
                if (subCat != null)
                    cat = db.Categories.Find(subCat.ParentId);
            }
            retString = (cat != null ? "Category : " + cat.Name + " \n " : "") + (subCat != null ? "SubCategory : " + subCat.Name + " \n " : "") + (type != null ? "Type : " + type.Name : "") + " \n ";
            var fitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            var inseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            var MasterOrdScaleList = dbOrder.OrderScales.Select(y => y.ClothesScaleId).Distinct();
            var MasterOrdSizeList = dbOrder.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScaleId).Distinct();
            List<int?> dbFitList = dbCloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
            List<int?> dbInseamList = dbCloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();
            if (dbFitList.Count == 0)
                dbFitList.Add(null);
            if (dbInseamList.Count == 0)
                dbInseamList.Add(null);
            foreach (var fitid in dbFitList)
            {
                foreach (var inseamid in dbInseamList)
                {
                    var scaleList = dbCloth.ClothesScales;
                    var prepackList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false && MasterOrdScaleList.Contains(x.ClothesScaleId));
                    var fit = fitList.FirstOrDefault(x => x.Id == fitid);
                    var inseam = fitList.FirstOrDefault(x => x.Id == fitid);
                    if (prepackList.Count() > 0)
                    {
                        bool shown = false;
                        foreach (var pack in prepackList)
                        {
                            string SizeQuant = string.Empty;

                            var ordPack = dbOrder.OrderScales.FirstOrDefault(x => x.ClothesScaleId == pack.ClothesScaleId);
                            var openSize = scaleList.FirstOrDefault(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true && MasterOrdSizeList.Contains(x.ClothesScaleId));
                            if (fit != null)
                                SizeQuant = "Fit: " + fit.Value + " \n ";
                            SizeQuant += pack.Name.ToUpper().Replace("SCALE", "") + " : " + (ordPack.Quantity.HasValue ? ordPack.Quantity.Value : 0) + "  \n ";
                            if (inseam != null)
                                SizeQuant += "Inseam: " + inseam.Value + " ";
                            if (openSize != null && !shown)
                            {
                                List<string> sizeStrings = new List<string>();
                                foreach (var size in openSize.ClothesScaleSizes)
                                {
                                    var ordSize = dbOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                                    sizeStrings.Add(size.Size.Name + " : " + (ordSize != null ? (ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0) : 0));
                                }
                                if (sizeStrings.Count > 0)
                                    SizeQuant += "Size: " + string.Join(" ,", sizeStrings);
                                SizeQuant += " \n ";
                                shown = true;
                            }
                            retString += SizeQuant;
                        }
                    }
                    else
                    {
                        var openSize = scaleList.FirstOrDefault(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true && MasterOrdSizeList.Contains(x.ClothesScaleId));
                        if (openSize != null)
                        {
                            string SizeQuant = string.Empty;
                            if (fit != null)
                                SizeQuant = "Fit: " + fit.Value + " \n ";
                            if (inseam != null)
                                SizeQuant += "Inseam: " + inseam.Value + " ";
                            if (openSize != null)
                            {
                                List<string> sizeStrings = new List<string>();
                                foreach (var size in openSize.ClothesScaleSizes)
                                {
                                    var ordSize = dbOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                                    sizeStrings.Add(size.Size.Name + " : " + (ordSize != null ? (ordSize.Quantity.HasValue ? ordSize.Quantity.Value : 0) : 0));
                                }
                                if (sizeStrings.Count > 0)
                                    SizeQuant += "Size: " + string.Join(" ,", sizeStrings);
                                SizeQuant += " \n ";
                            }
                            retString += SizeQuant;
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(retString) && retString.Length > 999)
                retString = retString.Substring(0, 996) + "...";
            return retString;
        }

        [NonAction]
        public string GetLoc(string type, string Id)
        {
            var di = new System.IO.DirectoryInfo(Server.MapPath("~/Library/Uploads/" + type + "/" + Id));
            di.Create();
            return di.FullName;
        }
        #endregion

        public ActionResult PrintView(string orderId, int accountId, string status, bool isPackingPrint)
        {
            string mypath = ConfigurationManager.AppSettings["CartUrl"].ToString();
            string mypath1 = string.Format("{0}Receipt=1&OrderID={1}&UAccountid={2}&printon=1", mypath, orderId, accountId);

            var result = (from ord in db.Orders
                          join tr in db.Tracks on ord.TrackId equals tr.Id
                          where ord.OrderNumber == orderId && ord.IsDelete == false
                          select tr).SingleOrDefault();

            mypath1 = string.Format("{0}&TrackingNo={1}", mypath1, result.TrackingNumber);
            mypath1 = string.Format("{0}&ShipCost={1}", mypath1, result.ShippingCost);

            Platini.DB.Account acc = db.Accounts.Where(x => x.AccountId == accountId).SingleOrDefault();

            if (acc.RoleId == 3 || isPackingPrint)
            {
                mypath1 = string.Format("{0}&HidePrice=1", mypath1);
            }

            if (status == "InProcess" || status == "New")
            {
                mypath1 = string.Format("{0}&PrintView=Purchase Order", mypath1);
            }
            else if (status == "Back Order")
            {
                mypath1 = string.Format("{0}&PrintView=Back Order", mypath1);
            }
            else if (status == "Packed" || status == "Shipped")
            {
                if (isPackingPrint)
                    mypath1 = string.Format("{0}&PrintView=Packing List", mypath1);
                else
                    mypath1 = string.Format("{0}&PrintView=Final Order", mypath1);
            }
            else if (status == "Pending")
            {
                mypath1 = string.Format("{0}&PrintView=Open Order", mypath1);
            }

            string strScreenShot = GetScreenShot(mypath1, orderId);
            string url = ConfigurationManager.AppSettings["baseurl"].ToString() + strScreenShot;
            url = url.Replace("~\\", "");
            url = url.Replace("\\", "/");
            Response.Redirect(url);
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        private string GetScreenShot(string screenUrl, string orderId)
        {
            string pdfUrl = PlatiniWebService.GetPdf(screenUrl);
            string pdfPath = DownloadFile(pdfUrl, orderId);
            string[] myarr = pdfUrl.Split('/');
            string path = string.Format("Screenshot\\{0}", myarr[4]);
            bool result = PlatiniWebService.RemovePdf(path);
            return pdfPath;
        }

        private string DownloadFile(string urlAddress, string orderid)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
            request.Timeout = 10000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream s = response.GetResponseStream();

            //Write to disk
            string filename = string.Format("PlatiniJeans{0}_{1}", orderid, DateTime.Now);
            filename = filename.Replace("-", String.Empty);
            string pdfpath = string.Format("~/Screenshot/{0}.pdf", filename);
            FileStream fs = new FileStream(Server.MapPath(pdfpath), FileMode.Create);

            //Byte[] read = new Byte()[];
            //int count = s.Read(read,0,read.Length);
            //while(count > 0)
            //{
            //    fs.Write(read,0,count);
            //    count = s.Read(read, 0, read.Length);
            //}

            //fs.Close();
            //s.Close();
            //response.Close();

            return pdfpath;
        }

        public ActionResult SavePrint(string orderId)
        {
            Order ord = db.Orders.Where(x => x.OrderNumber == orderId).SingleOrDefault();

            string mypath = ConfigurationManager.AppSettings["CartUrl"].ToString();
            string mypath1 = string.Format("{0}Receipt=1&OrderID={1}&UAccountid={2}&printon=1&HidePrice=1&PrintView=Packing List", mypath, orderId, ord.AccountId);

            string strScreenShot = GetScreenShot(mypath1, orderId);
            string url = ConfigurationManager.AppSettings["baseurl"].ToString() + strScreenShot;
            url = url.Replace("~\\", "");
            url = url.Replace("\\", "/");
            Response.Write("<script>");
            Response.Write("window.open('" + url + "','_blank')");
            Response.Write("</script>");
            //Response.Redirect(url);

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }
        [NonAction]
        public decimal? CalcSum(Guid OrderId)
        {
            var order = db.Orders.Find(OrderId);
            var account = db.Accounts.Find(order.AccountId);
            bool isRetail = account.CustomerOptionalInfoes.FirstOrDefault() != null ? account.CustomerOptionalInfoes.FirstOrDefault().CustomerType == (int)Platini.Models.CustomerType.Retail : true;
            var priceList = account.CustomerItemPrices.Where(x => x.Price.HasValue).ToList().Select(x => new { Id = x.ClothesId, Price = x.Price });
            var scalePriceList = order.OrderScales.Select(x => x.ClothesScale.Cloth).SelectMany(x => priceList.Where(y => y.Id == x.ClothesId).DefaultIfEmpty(), (x, y) => new { Price = (y != null) ? y.Price : (isRetail ? x.MSRP : x.Price), Id = x.ClothesId });
            var sizePriceList = order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth).SelectMany(x => priceList.Where(y => y.Id == x.ClothesId).DefaultIfEmpty(), (x, y) => new { Price = (y != null) ? y.Price : (isRetail ? x.MSRP : x.Price), Id = x.ClothesId });
            var scalePrice = order.OrderScales.Sum(x => (x.Quantity) * (x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity)) * (scalePriceList.FirstOrDefault(z => z.Id == x.ClothesScale.ClothesId).Price));
            var sizePrice = order.OrderSizes.Sum(x => x.Quantity * (sizePriceList.FirstOrDefault(y => y.Id == x.ClothesScaleSize.ClothesScale.ClothesId).Price));
            return scalePrice + sizePrice;
        }
        [NonAction]
        public CartOwner Data(Platini.DB.Account customer, Guid? printOrder)
        {
            var model = new CartOwner() { CommunicationId = 0, TermId = 0, ShipVia = 0 };
            model.UserId = customer.AccountId;
            model.Buyer = customer.FirstName + " " + customer.LastName;
            model.Email = customer.Email;
            model.CompanyName = string.Empty;
            if (customer.Companies.FirstOrDefault() != null)
                model.CompanyName = customer.Companies.FirstOrDefault().Name;
            model.BillingAddress = new AddressText() { AddressId = 0 };
            model.ShippingAddress = new AddressText() { AddressId = 0 };
            if (customer.Addresses.Any(x => x.IsActive == true && x.IsDelete == false))
            {
                var Address = customer.Addresses.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.AddressTypeId == (int)AddressTypeEnum.BillingAddress);
                if (Address != null)
                {
                    model.BillingAddress.Line1 = Address.Street;
                    model.BillingAddress.AddressId = Address.AddressId;
                    model.BillingAddress.Line2 = string.Format("{0}, {1}, {2}", Address.City, Address.State, Address.Pincode);
                }
                if (Session["Order"] != null || Session["EditingOrder"] != null || printOrder.HasValue)
                {
                    Guid lastOrder = Guid.Empty;
                    if (Session["Order"] != null)
                        lastOrder = (Guid)Session["Order"];
                    if (Session["EditingOrder"] != null)
                        lastOrder = (Guid)Session["EditingOrder"];
                    if (printOrder.HasValue)
                        lastOrder = printOrder.Value;
                    var order = db.Orders.FirstOrDefault(x => x.OrderId == lastOrder);
                    if (order != null)
                    {
                        if (order.AddressId.HasValue)
                            Address = db.Addresses.FirstOrDefault(x => x.AddressId == order.AddressId.Value);
                        if (order.TermId.HasValue)
                            model.TermId = order.TermId.Value;
                        if (order.ShipViaId.HasValue)
                            model.ShipVia = order.ShipViaId.Value;
                    }
                }
                Address = null;
                Address = customer.Addresses.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress);
                if (Address != null)
                {
                    model.ShippingAddress.AddressId = Address.AddressId;
                    model.ShippingAddress.Line1 = Address.Street;
                    model.ShippingAddress.Line2 = string.Format("{0}, {1}", Address.City, Address.State);
                    model.ShippingAddress.To = Address.ShipTo;
                    model.ShippingAddress.ZipCode = Address.Pincode;
                }
            }
            model.Fax = model.Phone = string.Empty;
            if (customer.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false) != null)
            {
                var phone = customer.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false);
                model.CommunicationId = phone.CommunicationId;
                model.Fax = phone.Fax;
                model.Phone = phone.Phone;
            }
            model.isRetail = customer.RoleId == (int)RolesEnum.Customer || customer.RoleId == (int)RolesEnum.User;
            if (customer.CustomerOptionalInfoes.FirstOrDefault() != null)
            {
                var info = customer.CustomerOptionalInfoes.FirstOrDefault();
                if (model.TermId == 0)
                    model.TermId = info.TermId.HasValue ? info.TermId.Value : 0;
                if (model.ShipVia == 0)
                    model.ShipVia = info.ShipViaId.HasValue ? info.ShipViaId.Value : 0;
                model.Discount = info.Discount.HasValue ? info.Discount.Value : 0.0m;
                model.isRetail = info.CustomerType == (int)Platini.Models.CustomerType.Retail;
            }
            var salesPerson = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == model.UserId);
            model.SalesPerson = "Default";
            if (salesPerson != null)
            {
                model.SalesPersonId = salesPerson.SalesPersonId;
                model.SalesPerson = db.Accounts.FirstOrDefault(x => x.AccountId == model.SalesPersonId).Email;
            }
            return model;
        }

        public List<SelectListItem> ClothesList()
        {
            List<SelectListItem> ClothesList = new List<SelectListItem>();
            var clothes = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false);
            foreach (var cl in clothes)
            {
                ClothesList.Add(new SelectListItem { Text = cl.StyleNumber.ToString(), Value = cl.ClothesId.ToString() });
            }
            return ClothesList;
        }
        #region Recode1
        //public ActionResult ProcessOrder2(string orderId)
        //{
        //    ProcessModel pm = new ProcessModel();
        //    List<ProcessOrderModel> pomList = new List<ProcessOrderModel>();
        //    Order ord = db.Orders.Where(o => o.OrderNumber == orderId && o.IsDelete == false).SingleOrDefault();
        //    List<OrderScale> orderScaleList = new List<OrderScale>();
        //    List<OrderSize> orderSizeList = new List<OrderSize>();
        //    if (ord != null)
        //    {
        //        foreach (OrderScale dbOS in ord.OrderScales)
        //        {
        //            OrderScale os = new OrderScale();
        //            os.InjectClass(dbOS);
        //            orderScaleList.Add(os);
        //        }

        //        foreach (OrderSize dbOs in ord.OrderSizes)
        //        {
        //            OrderSize os = new OrderSize();
        //            os.InjectClass(dbOs);
        //            orderSizeList.Add(os);
        //        }

        //        foreach (OrderScale ors in orderScaleList)
        //        {
        //            ProcessOrderModel pom = new ProcessOrderModel();
        //            var query = (from os in db.OrderScales
        //                         join cs in db.ClothesScales on os.ClothesScaleId equals cs.ClothesScaleId
        //                         join clth in db.Clothes on cs.ClothesId equals clth.ClothesId
        //                         //join ci in db.ClothesImages on clth.ClothesId equals ci.ClothesId
        //                         join cat in db.Categories on clth.CategoryId equals cat.CategoryId
        //                         where os.OrderScaleId == ors.OrderScaleId
        //                         select new { cs, clth, cat }).FirstOrDefault();

        //            if (query != null)
        //            {
        //                pom.StyleNumber = query.clth.StyleNumber;
        //                pom.CategoryName = query.cat.Name;
        //                ClothesImage ci = db.ClothesImages.Where(c => c.ClothesId == query.clth.ClothesId).FirstOrDefault();
        //                if (ci != null)
        //                {
        //                    pom.ImagePath = ci.ImagePath;
        //                }
        //                if (ors.Quantity.HasValue)
        //                    pom.Quantity = ors.Quantity.Value;
        //                else
        //                    pom.Quantity = 0;
        //                pom.Scale = "Prepack";
        //                if (query.cs.FitId != null)
        //                {
        //                    Fit fit = db.Fits.Where(f => f.FitId == query.cs.FitId.Value).SingleOrDefault();
        //                    pom.Scales = fit.Name;
        //                }
        //                else
        //                {
        //                    pom.Scales = "";
        //                }
        //                if (query.cs.InseamId != null)
        //                {
        //                    Inseam ins = db.Inseams.Where(i => i.InseamId == query.cs.InseamId.Value).SingleOrDefault();
        //                    pom.Inseam = ins.Name;
        //                }
        //                else
        //                {
        //                    pom.Inseam = "";
        //                }
        //                pom.SizeList = new List<SizeQty>();

        //                var q = db.Sizes.Where(s => s.SizeGroupId == query.clth.SizeGroupId).ToList();
        //                foreach (Size sz in q)
        //                {
        //                    SizeQty sq = new SizeQty();
        //                    sq.SizeId = sz.SizeId;
        //                    sq.SizeName = sz.Name;
        //                    sq.Quantity = 1;
        //                    pom.SizeList.Add(sq);
        //                }
        //                pomList.Add(pom);
        //            }
        //        }

        //        foreach (OrderSize osz in orderSizeList)
        //        {
        //            ProcessOrderModel pom = new ProcessOrderModel();
        //            var query = (from os in db.OrderSizes
        //                         join css in db.ClothesScaleSizes on os.ClothesSizeId equals css.ClothesScaleSizeId
        //                         join cs in db.ClothesScales on css.ClothesScaleId equals cs.ClothesScaleId
        //                         join clth in db.Clothes on cs.ClothesId equals clth.ClothesId
        //                         //join ci in db.ClothesImages on clth.ClothesId equals ci.ClothesId
        //                         join cat in db.Categories on clth.CategoryId equals cat.CategoryId
        //                         join sz in db.Sizes on css.SizeId equals sz.SizeId
        //                         where os.OrderSizeId == osz.OrderSizeId
        //                         select new { cs, css, clth, cat, sz }).FirstOrDefault();

        //            if (query != null)
        //            {
        //                pom.StyleNumber = query.clth.StyleNumber;
        //                pom.CategoryName = query.cat.Name;
        //                ClothesImage ci = db.ClothesImages.Where(c => c.ClothesId == query.clth.ClothesId).FirstOrDefault();
        //                if (ci != null)
        //                {
        //                    pom.ImagePath = ci.ImagePath;
        //                }
        //                if (osz.Quantity.HasValue)
        //                    pom.Quantity = osz.Quantity.Value;
        //                else
        //                    pom.Quantity = 0;
        //                pom.Scale = "Open Size";
        //                if (query.cs.FitId != null)
        //                {
        //                    Fit fit = db.Fits.Where(f => f.FitId == query.cs.FitId.Value).SingleOrDefault();
        //                    pom.Scales = fit.Name;
        //                }
        //                else
        //                {
        //                    pom.Scales = "";
        //                }
        //                if (query.cs.InseamId != null)
        //                {
        //                    Inseam ins = db.Inseams.Where(i => i.InseamId == query.cs.InseamId.Value).SingleOrDefault();
        //                    pom.Inseam = ins.Name;
        //                }
        //                else
        //                {
        //                    pom.Inseam = "";
        //                }
        //                pom.SizeList = new List<SizeQty>();

        //                SizeQty sq = new SizeQty();
        //                sq.SizeId = query.sz.SizeId;
        //                sq.SizeName = query.sz.Name;
        //                sq.Quantity = 1;

        //                pom.SizeList.Add(sq);
        //                pomList.Add(pom);
        //            }
        //        }
        //        pm.POMs = new List<ProcessOrderModel>();
        //        pm.POMs = pomList;
        //        pm.AccountId = ord.AccountId;
        //    }
        //    pm.OrderId = orderId;

        //    return View(pm);
        //}

        //public ActionResult GetPackingInfo(string orderId)
        //{
        //    PackingInfo pi = new PackingInfo();
        //    //Order ord = db.Orders.Where(o => o.OrderNumber == orderId).SingleOrDefault();
        //    var result = (from ord in db.Orders
        //                  join acc in db.Accounts on ord.AccountId equals acc.AccountId
        //                  where ord.OrderNumber == orderId
        //                  select new { ord, acc }).SingleOrDefault();

        //    pi.OrderId = orderId;
        //    pi.CustomerName = result.acc.FirstName + " " + result.acc.LastName;

        //    var addr = db.Addresses.Where(a => a.AccountId == result.acc.AccountId && a.AddressTypeId == 2).SingleOrDefault();

        //    if (addr != null)
        //    {
        //        pi.ShippingAddress = addr.Street + " , " + addr.City + ", " + addr.State + ", " + addr.Country;
        //    }

        //    pi.ShipViaList = new List<DropDownList>();
        //    var shipViaList = db.ShipVias.Where(x => x.IsActive == true && x.IsDeleted == false).ToList();
        //    foreach (var item in shipViaList)
        //    {
        //        DropDownList ddl = new DropDownList();
        //        ddl.Id = item.Id;
        //        ddl.Name = item.Name;
        //        pi.ShipViaList.Add(ddl);
        //    }
        //    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(pi);

        //    return Json(jsonData, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult PackSubmit(int orderId, string numberOfBoxes, string numberOfBags, string[] boxes, string[] bags)
        //{
        //    if (!String.IsNullOrEmpty(numberOfBoxes))
        //    {
        //        int bxs = Convert.ToInt32(numberOfBoxes);
        //        if (!String.IsNullOrEmpty(numberOfBags))
        //        {
        //            int bgs = Convert.ToInt32(numberOfBags);
        //            for (int i = 0; i < boxes.Length; i++)
        //            {
        //                string[] str = boxes[i].Split(':');
        //                //int weight = Convert.ToInt32(str[3]);
        //                int j = i + 1;
        //                Box bx = new Box();
        //                bx.OrderId = orderId;
        //                bx.Name = "Box" + j;
        //                bx.TotalBox = bxs;
        //                bx.Weight = str[3];
        //                bx.Dimension = str[0] + ":" + str[1] + ":" + str[2];
        //                db.Boxes.Add(bx);
        //                db.SaveChanges();
        //            }


        //            for (int i = 0; i < bags.Length; i++)
        //            {
        //                int j = i + 1;
        //                Bag bg = new Bag();
        //                bg.OrderId = orderId;
        //                bg.Name = "Bag" + j;
        //                bg.TotalBags = bgs;
        //                bg.Weight = bags[i];
        //                db.Bags.Add(bg);
        //                db.SaveChanges();
        //            }
        //        }
        //        else
        //        {

        //            for (int i = 0; i < boxes.Length; i++)
        //            {
        //                string[] str = boxes[i].Split(':');
        //                //int weight = Convert.ToInt32(str[3]);
        //                int j = i + 1;
        //                Box bx = new Box();
        //                bx.OrderId = orderId;
        //                bx.Name = "Box" + j;
        //                bx.TotalBox = bxs;
        //                bx.Weight = str[3];
        //                bx.Dimension = str[0] + ":" + str[1] + ":" + str[2];
        //                db.Boxes.Add(bx);
        //                db.SaveChanges();
        //            }
        //        }
        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }
        //    return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult SaveClose(int orderId, string note)
        //{
        //    List<Box> boxes = db.Boxes.Where(x => x.OrderId == orderId).ToList();
        //    List<Bag> bags = db.Bags.Where(x => x.OrderId == orderId).ToList();

        //    Order ord = db.Orders.Where(x => x.OrderNumber == orderId && x.ConfirmStatus == true).SingleOrDefault();

        //    if (ord != null)
        //    {
        //        if (!String.IsNullOrEmpty(note))
        //        {
        //            ord.Note = note;
        //            db.SaveChanges();
        //        }

        //        OrderStatu orderStatus = db.OrderStatus.Where(x => x.Status == "Packed").SingleOrDefault();
        //        if (orderStatus != null)
        //        {
        //            if (ord.ParentOrderId == null)
        //            {
        //                ord.StatusId = orderStatus.OrderStatusId;
        //                ord.PackedOn = DateTime.Now;
        //                ord.Label = orderId;
        //                db.SaveChanges();
        //            }
        //            else
        //            {
        //                ord.StatusId = orderStatus.OrderStatusId;
        //                ord.PackedOn = DateTime.Now;
        //                db.SaveChanges();
        //            }
        //        }

        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }

        //    return Json(new { Response = "Failure"}, JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult SendToQB(int orderId, int accountId)
        //{
        //    var res1 = db.Boxes.Where(x => x.OrderId == orderId).ToList();
        //    var res2 = db.Bags.Where(x => x.OrderId == orderId).ToList();
        //    var ord = db.Orders.Where(x => x.OrderNumber == orderId).FirstOrDefault();

        //    if (res1.Count > 0 || res2.Count > 0)
        //    {
        //        Order order = db.Orders.Where(x => x.OrderNumber == orderId && x.IsDelete == false).SingleOrDefault();
        //        if (order != null)
        //        {
        //            order.StatusId = 5;
        //            order.ShippedOn = DateTime.Now;
        //            db.SaveChanges();
        //        }
        //        generateInvoice(order.OrderId, accountId);
        //    }
        //    return Json(string.Empty, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult Submit(string orderId)
        //{
        //    Order ord = db.Orders.Where(x => x.OrderNumber == orderId && x.IsDelete == false).SingleOrDefault();
        //    if (ord != null)
        //    {
        //        ord.OriginalQty = 0;
        //        ord.SubmittedOn = DateTime.Now;
        //        ord.StatusId = 1;
        //        db.SaveChanges();

        //        OrderScale os = db.OrderScales.Where(x => x.OrderId == ord.OrderId).SingleOrDefault();
        //        if (os != null)
        //        {
        //            ClothesScale cs = db.ClothesScales.Where(x => x.ClothesScaleId == os.ClothesScale.ClothesScaleId).SingleOrDefault();
        //            cs.InvQty = cs.InvQty - os.Quantity;
        //            db.SaveChanges();
        //        }

        //        OrderSize osz = db.OrderSizes.Where(x => x.OrderId == ord.OrderId).SingleOrDefault();
        //        if (osz != null)
        //        {
        //            ClothesScaleSize csz = db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == osz.ClothesScaleSize.ClothesScaleSizeId).SingleOrDefault();
        //            csz.Quantity = csz.Quantity - osz.Quantity;
        //            db.SaveChanges();
        //        }
        //        //
        //        //TODO : from elanvital

        //        //SendOrderEmailToCustomer();
        //    }
        //    return Json(string.Empty, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult SendEmail(string emails, string subject, string message, string[] ids)
        //{
        //    if (ids != null && !String.IsNullOrEmpty(emails))
        //    {
        //        var mypath = ConfigurationManager.AppSettings["CartUrl"].ToString();
        //        foreach (string a in ids)
        //        {
        //            string mypath1 = string.Format("{0}Receipt=1&OrderID={1}&UAccountid={2}&printon=1", mypath, a, db.Orders.Where(x => x.OrderNumber == a).SingleOrDefault().AccountId);
        //            string strScreenShot = GetScreenShot(mypath1, a);
        //            string strEmail = ConfigurationManager.AppSettings["MailFrom"];
        //            EmailManager.SendEmail(subject, message, Server.MapPath(strScreenShot), strEmail, emails, false, string.Empty);
        //            FileInfo fi = new FileInfo(Server.MapPath(strScreenShot));
        //            if (fi.Exists)
        //            {
        //                System.IO.File.Delete(Server.MapPath(strScreenShot));
        //            }
        //        }
        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }
        //    return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        //}
        //public JsonResult GetOrderList(string status, int? Id)
        //{
        //    string jsonData = string.Empty;
        //    if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == RolesEnum.SalesPerson.ToString())
        //    {
        //        Id = Convert.ToInt32(SiteIdentity.UserId);
        //    }

        //    if (!String.IsNullOrEmpty(status))
        //    {
        //        if (!Id.HasValue)
        //        {
        //            if (!(status.ToLower() == "all"))
        //            {
        //                List<DisplayOrder> dOrderList = db.Accounts.Where(acc => acc.IsDelete == false && acc.IsActive == true).SelectMany(acc => db.Companies.Where(cmp => cmp.AccountId == acc.AccountId && cmp.IsActive == true && cmp.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (acc, cmp) => new
        //                                                {
        //                                                    ab = acc,
        //                                                    bc = cmp
        //                                                })
        //                                                .SelectMany(g => db.Communications.Where(comm => comm.AccountId == g.ab.AccountId && comm.IsActive == true && comm.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (g, comm) => new
        //                                                {
        //                                                    cd = g.ab,
        //                                                    de = g.bc,
        //                                                    ef = comm
        //                                                })
        //                                                .SelectMany(h => db.Addresses.Where(addr => addr.AccountId == h.cd.AccountId && addr.IsActive == true && addr.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (h, addr) => new
        //                                                {
        //                                                    fg = h.cd,
        //                                                    gh = h.de,
        //                                                    hi = h.ef,
        //                                                    ij = addr
        //                                                })
        //                                                .SelectMany(i => db.Orders.Where(ord => ord.AccountId == i.fg.AccountId && ord.IsDelete == false).Take(1),
        //                                                (i, ord) => new
        //                                                {
        //                                                    jk = i.fg,
        //                                                    kl = i.gh,
        //                                                    lm = i.hi,
        //                                                    mn = i.ij,
        //                                                    no = ord
        //                                                }).Where(k => k.no.IsDelete == false)
        //                                                .SelectMany(j => db.OrderStatus.Where(st => st.OrderStatusId == j.no.StatusId && st.Status == status),
        //                                                (j, st) => new DisplayOrder
        //                                                {
        //                                                    OrderId = j.no.OrderNumber,
        //                                                    FirstName = j.jk.FirstName,
        //                                                    LastName = j.jk.LastName,
        //                                                    //Address = string.Format("{0}, {1}, {2}",j.mn.Street, j.mn.City, j.mn.State),
        //                                                    Address = j.mn.Street + " , " + j.mn.City + " , " + j.mn.State,
        //                                                    Email = j.jk.Email,
        //                                                    Phone = j.lm.Phone,
        //                                                    Date = j.no.PackedOn,
        //                                                    CompanyName = j.kl.Name,
        //                                                    OriginalQuantity = j.no.OriginalQty,
        //                                                    PackedQuantity = j.no.PackedQty,
        //                                                    GrandTotal = j.no.GrandTotal,
        //                                                    Discount = j.no.Discount,
        //                                                    FinalAmount = j.no.FinalAmount
        //                                                }).ToList();

        //                jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dOrderList);
        //            }
        //            else
        //            {
        //                List<DisplayOrder> dOrderList = db.Accounts.Where(acc => acc.IsDelete == false && acc.IsActive == true).SelectMany(acc => db.Companies.Where(cmp => cmp.AccountId == acc.AccountId && cmp.IsActive == true && cmp.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (acc, cmp) => new
        //                                                {
        //                                                    ab = acc,
        //                                                    bc = cmp
        //                                                })
        //                                                .SelectMany(g => db.Communications.Where(comm => comm.AccountId == g.ab.AccountId && comm.IsActive == true && comm.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (g, comm) => new
        //                                                {
        //                                                    cd = g.ab,
        //                                                    de = g.bc,
        //                                                    ef = comm
        //                                                })
        //                                                .SelectMany(h => db.Addresses.Where(addr => addr.AccountId == h.cd.AccountId && addr.IsActive == true && addr.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (h, addr) => new
        //                                                {
        //                                                    fg = h.cd,
        //                                                    gh = h.de,
        //                                                    hi = h.ef,
        //                                                    ij = addr
        //                                                })
        //                                                .SelectMany(i => db.Orders.Where(ord => ord.AccountId == i.fg.AccountId && ord.IsDelete == false).Take(1),
        //                                                (i, ord) => new
        //                                                {
        //                                                    jk = i.fg,
        //                                                    kl = i.gh,
        //                                                    lm = i.hi,
        //                                                    mn = i.ij,
        //                                                    no = ord
        //                                                }).Where(k => k.no.IsDelete == false)
        //                                                .SelectMany(j => db.OrderStatus.Where(st => st.OrderStatusId == j.no.StatusId),
        //                                                (j, st) => new DisplayOrder
        //                                                {
        //                                                    OrderId = j.no.OrderNumber,
        //                                                    FirstName = j.jk.FirstName,
        //                                                    LastName = j.jk.LastName,
        //                                                    //Address = string.Format("{0}, {1}, {2}",j.mn.Street, j.mn.City, j.mn.State),
        //                                                    Address = j.mn.Street + " , " + j.mn.City + " , " + j.mn.State,
        //                                                    Email = j.jk.Email,
        //                                                    Phone = j.lm.Phone,
        //                                                    Date = j.no.PackedOn,
        //                                                    CompanyName = j.kl.Name,
        //                                                    OriginalQuantity = j.no.OriginalQty,
        //                                                    PackedQuantity = j.no.PackedQty,
        //                                                    GrandTotal = j.no.GrandTotal,
        //                                                    Discount = j.no.Discount,
        //                                                    FinalAmount = j.no.FinalAmount
        //                                                }).ToList();

        //                jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dOrderList);
        //            }
        //        }
        //        else
        //        {
        //            if (!(status.ToLower() == "all"))
        //            {
        //                List<DisplayOrder> dOrderList = db.Accounts.Where(acc => acc.IsDelete == false && acc.IsActive == true && acc.AccountId == Id.Value).SelectMany(acc => db.Companies.Where(cmp => cmp.AccountId == acc.AccountId && cmp.IsActive == true && cmp.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (acc, cmp) => new
        //                                                {
        //                                                    ab = acc,
        //                                                    bc = cmp
        //                                                })
        //                                                .SelectMany(g => db.Communications.Where(comm => comm.AccountId == g.ab.AccountId && comm.IsActive == true && comm.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (g, comm) => new
        //                                                {
        //                                                    cd = g.ab,
        //                                                    de = g.bc,
        //                                                    ef = comm
        //                                                })
        //                                                .SelectMany(h => db.Addresses.Where(addr => addr.AccountId == h.cd.AccountId && addr.IsActive == true && addr.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                                (h, addr) => new
        //                                                {
        //                                                    fg = h.cd,
        //                                                    gh = h.de,
        //                                                    hi = h.ef,
        //                                                    ij = addr
        //                                                })
        //                                                .SelectMany(i => db.Orders.Where(ord => ord.AccountId == i.fg.AccountId && ord.IsDelete == false).Take(1),
        //                                                (i, ord) => new
        //                                                {
        //                                                    jk = i.fg,
        //                                                    kl = i.gh,
        //                                                    lm = i.hi,
        //                                                    mn = i.ij,
        //                                                    no = ord
        //                                                }).Where(k => k.no.IsDelete == false)
        //                                                .SelectMany(j => db.OrderStatus.Where(st => st.OrderStatusId == j.no.StatusId && st.Status == status),
        //                                                (j, st) => new DisplayOrder
        //                                                {
        //                                                    OrderId = j.no.OrderNumber,
        //                                                    FirstName = j.jk.FirstName,
        //                                                    LastName = j.jk.LastName,
        //                                                    //Address = string.Format("{0}, {1}, {2}",j.mn.Street, j.mn.City, j.mn.State),
        //                                                    Address = j.mn.Street + " , " + j.mn.City + " , " + j.mn.State,
        //                                                    Email = j.jk.Email,
        //                                                    Phone = j.lm.Phone,
        //                                                    Date = j.no.PackedOn,
        //                                                    CompanyName = j.kl.Name,
        //                                                    OriginalQuantity = j.no.OriginalQty,
        //                                                    PackedQuantity = j.no.PackedQty,
        //                                                    GrandTotal = j.no.GrandTotal,
        //                                                    Discount = j.no.Discount,
        //                                                    FinalAmount = j.no.FinalAmount
        //                                                }).ToList();

        //                jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dOrderList);
        //            }
        //            else
        //            {
        //                List<DisplayOrder> dOrderList = db.Accounts.Where(acc => acc.IsDelete == false && acc.IsActive == true && acc.AccountId == Id.Value).SelectMany(acc => db.Companies.Where(cmp => cmp.AccountId == acc.AccountId && cmp.IsActive == true && cmp.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                               (acc, cmp) => new
        //                                               {
        //                                                   ab = acc,
        //                                                   bc = cmp
        //                                               })
        //                                               .SelectMany(g => db.Communications.Where(comm => comm.AccountId == g.ab.AccountId && comm.IsActive == true && comm.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                               (g, comm) => new
        //                                               {
        //                                                   cd = g.ab,
        //                                                   de = g.bc,
        //                                                   ef = comm
        //                                               })
        //                                               .SelectMany(h => db.Addresses.Where(addr => addr.AccountId == h.cd.AccountId && addr.IsActive == true && addr.IsDelete == false).Take(1).DefaultIfEmpty(),
        //                                               (h, addr) => new
        //                                               {
        //                                                   fg = h.cd,
        //                                                   gh = h.de,
        //                                                   hi = h.ef,
        //                                                   ij = addr
        //                                               })
        //                                               .SelectMany(i => db.Orders.Where(ord => ord.AccountId == i.fg.AccountId && ord.IsDelete == false).Take(1),
        //                                               (i, ord) => new
        //                                               {
        //                                                   jk = i.fg,
        //                                                   kl = i.gh,
        //                                                   lm = i.hi,
        //                                                   mn = i.ij,
        //                                                   no = ord
        //                                               }).Where(k => k.no.IsDelete == false)
        //                                               .SelectMany(j => db.OrderStatus.Where(st => st.OrderStatusId == j.no.StatusId),
        //                                               (j, st) => new DisplayOrder
        //                                               {
        //                                                   OrderId = j.no.OrderNumber,
        //                                                   FirstName = j.jk.FirstName,
        //                                                   LastName = j.jk.LastName,
        //                                                   //Address = string.Format("{0}, {1}, {2}",j.mn.Street, j.mn.City, j.mn.State),
        //                                                   Address = j.mn.Street + " , " + j.mn.City + " , " + j.mn.State,
        //                                                   Email = j.jk.Email,
        //                                                   Phone = j.lm.Phone,
        //                                                   Date = j.no.PackedOn,
        //                                                   CompanyName = j.kl.Name,
        //                                                   OriginalQuantity = j.no.OriginalQty,
        //                                                   PackedQuantity = j.no.PackedQty,
        //                                                   GrandTotal = j.no.GrandTotal,
        //                                                   Discount = j.no.Discount,
        //                                                   FinalAmount = j.no.FinalAmount
        //                                               }).ToList();

        //                jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dOrderList);
        //            }
        //        }
        //    }
        //    return Json(jsonData, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult MoveToWareHouse(string[] ids)
        //{
        //    if (ids != null)
        //    {
        //        foreach (string a in ids)
        //        {
        //            Order ord = db.Orders.Where(x => x.OrderNumber == a && x.IsDelete == false).SingleOrDefault();
        //            if (ord != null)
        //            {
        //                ord.StatusId = db.OrderStatus.Where(x => x.Status == "InProcess").SingleOrDefault().OrderStatusId;
        //                ord.DateUpdated = DateTime.Now;
        //                db.SaveChanges();
        //            }
        //        }
        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }

        //    return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        //}


        //public ActionResult QuickShip(string[] ids)
        //{
        //    if (ids != null)
        //    {
        //        foreach (string a in ids)
        //        {
        //            Order ord = db.Orders.Where(x => x.OrderNumber == a && x.IsDelete == false).SingleOrDefault();
        //            if (ord != null)
        //            {
        //                ord.StatusId = db.OrderStatus.Where(x => x.Status == "Shipped").SingleOrDefault().OrderStatusId;
        //                ord.ShippedOn = DateTime.Now;
        //                ord.DateUpdated = DateTime.Now;
        //                db.SaveChanges();
        //            }
        //        }
        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }
        //    return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult DeleteOrder(string[] ids)
        //{
        //    if (ids != null)
        //    {
        //        foreach (string a in ids)
        //        {
        //            Order ord = db.Orders.Where(x => x.OrderNumber == a && x.IsDelete == false).SingleOrDefault();
        //            if (ord != null)
        //            {
        //                ord.IsDelete = true;
        //                ord.DateUpdated = DateTime.Now;
        //                db.SaveChanges();
        //            }
        //        }
        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }
        //    return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult Ship(string orderId)
        //{
        //    Order ord = db.Orders.Where(x => x.OrderNumber == orderId && x.IsDelete == false).SingleOrDefault();
        //    if (ord != null)
        //    {
        //        ord.StatusId = db.OrderStatus.Where(x => x.Status == "Shipped").SingleOrDefault().OrderStatusId;
        //        ord.ShippedOn = DateTime.Now;
        //        ord.DateUpdated = DateTime.Now;
        //        db.SaveChanges();
        //        return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
        //    }
        //    return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        //} 
        #endregion
    }
}

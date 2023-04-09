using Platini.DB;
//using Platini.Areas.Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System.Data;

namespace Platini.Areas.Common.Controllers
{
    public class FailureController : Controller
    {
        #region Intuit Library
        private Entities db = new Entities();
        static OAuthRequestValidator oauthValidator = new OAuthRequestValidator(System.Configuration.ConfigurationManager.AppSettings["accessToken"].ToString(), System.Configuration.ConfigurationManager.AppSettings["accessTokenSecret"].ToString(), System.Configuration.ConfigurationManager.AppSettings["consumerKey"].ToString(), System.Configuration.ConfigurationManager.AppSettings["consumerSecret"].ToString());
        static string appToken = System.Configuration.ConfigurationManager.AppSettings["appToken"].ToString();
        static string companyID = System.Configuration.ConfigurationManager.AppSettings["companyID"].ToString();
        static ServiceContext Objcontext = new ServiceContext(appToken, companyID, IntuitServicesType.QBO, oauthValidator);
        DataService service = new DataService(Objcontext);
        #endregion

        #region Strings
        String Prepack = string.Empty;
        String Size = string.Empty;
        String Inseam1 = string.Empty;
        String Fit = string.Empty;
        String Display = string.Empty;
        string str = string.Empty;
        #endregion


        public ActionResult Index()
        {
            List<QuickBookFailureRecord> result = db.QuickBookFailureRecords.ToList();
            return View(result);
        }

        //public ActionResult ErrorLog(int Id)
        //{
        //    var Failure= db.QuickBookFailureRecords.Find(Id);
        //    if(Failure!=null)
        //    {
        //        ViewBag.ErrorList = Failure.ErrorDetails;
        //        return PartialView("ErrorList");
        //    }
        //    TempData["PageMessage"] = "No order found.";
        //    return RedirectToAction("Index");            
        //}

        public JsonResult Retry(string failureFrom, int id)
        {
            if (failureFrom.ToLower() == "customer")
            {
                addEditDeleteCustomerToQuickBook(id);
            }
            else if (failureFrom.ToLower() == "product")
            {
                addEditDeleteProductToQuickBook(id);
            }
            else
            {
                generateInvoiceToQuickBook(id);
            }

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RetryAll(string[] arr)
        {
            if (arr.Any())
            {
                foreach (string str in arr)
                {
                    string[] st = str.Split('-');
                    string failureFrom = st[0];
                    int id = int.Parse(st[1]);
                    if (failureFrom.ToLower() == "customer")
                    {
                        addEditDeleteCustomerToQuickBook(id);
                    }
                    else if (failureFrom.ToLower() == "product")
                    {
                        addEditDeleteProductToQuickBook(id);
                    }
                    else
                    {
                        generateInvoiceToQuickBook(id);
                    }
                }
            }

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }


        private void addEditDeleteCustomerToQuickBook(int custId)
        {
            var query = from acc in db.Accounts
                        join coi in db.CustomerOptionalInfoes on acc.AccountId equals coi.AccountId
                        join trm in db.Terms on coi.TermId equals trm.TermId
                        join qfr in db.QuickBookFailureRecords on acc.AccountId equals qfr.FailureFromId
                        join cmp in db.Companies on acc.AccountId equals cmp.AccountId
                        join comm in db.Communications on acc.AccountId equals comm.AccountId
                        join addr in db.Addresses on acc.AccountId equals addr.AccountId
                        where acc.AccountId == custId
                        select new { acc, coi, trm, qfr, cmp, comm, addr };


            Customer objCustomer = new Customer();
            var dbcustomerOptionalInfo = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == custId);
            QueryService<Intuit.Ipp.Data.Term> termQueryService = new QueryService<Intuit.Ipp.Data.Term>(Objcontext);
            Intuit.Ipp.Data.Term term = new Intuit.Ipp.Data.Term();
            QueryService<Customer> account = new QueryService<Customer>(Objcontext);

            if (query.Any() && query.Where(x => x.acc.IsActive == true && x.acc.IsDelete == false).Any())
            {
                objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + query.FirstOrDefault().qfr.FailureOriginalValue.ToString().Trim() + "'").FirstOrDefault();

                if (query.FirstOrDefault().trm.Name != null)
                {
                    term = termQueryService.ExecuteIdsQuery("Select * From Term Where Name='" + query.FirstOrDefault().trm.Name.ToString().Trim() + "'").FirstOrDefault();
                }

                if (objCustomer != null)
                {
                    objCustomer.GivenName = Convert.ToString(query.FirstOrDefault().acc.FirstName);
                    objCustomer.FamilyName = Convert.ToString(query.FirstOrDefault().acc.LastName);
                    objCustomer.CompanyName = Convert.ToString(query.FirstOrDefault().cmp.Name);
                    objCustomer.DisplayName = Convert.ToString(query.FirstOrDefault().cmp.Name);
                    objCustomer.PrimaryPhone = new TelephoneNumber { FreeFormNumber = Convert.ToString(query.FirstOrDefault().comm.Phone) };
                    objCustomer.PrimaryEmailAddr = new EmailAddress { Address = query.FirstOrDefault().acc.Email.ToString() };
                    objCustomer.BillAddr = new PhysicalAddress
                    {
                        City = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.City),
                        Line1 = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.Street),
                        PostalCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.Pincode),
                        CountrySubDivisionCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.State),
                        Country = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.Country)
                    };
                    objCustomer.ShipAddr = new PhysicalAddress
                    {
                        City = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.City),
                        Line1 = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.Street),
                        PostalCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.Pincode),
                        CountrySubDivisionCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.State),
                        Country = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.Country)
                    };
                    objCustomer.Fax = new TelephoneNumber
                    {
                        AreaCode = "01",
                        FreeFormNumber = Convert.ToString(query.FirstOrDefault().comm.Fax)
                    };
                    if (term != null)
                    {
                        objCustomer.SalesTermRef = new ReferenceType() { name = term.Name, Value = term.Id };
                    }
                    objCustomer.ResaleNum = dbcustomerOptionalInfo.BusinessReseller;
                    service.Update(objCustomer);
                }
                else
                {
                    objCustomer = new Customer();
                    objCustomer.GivenName = Convert.ToString(query.FirstOrDefault().acc.FirstName);
                    objCustomer.FamilyName = Convert.ToString(query.FirstOrDefault().acc.LastName);
                    objCustomer.CompanyName = Convert.ToString(query.FirstOrDefault().cmp.Name);
                    objCustomer.DisplayName = Convert.ToString(query.FirstOrDefault().cmp.Name);
                    objCustomer.PrimaryPhone = new TelephoneNumber { FreeFormNumber = Convert.ToString(query.FirstOrDefault().comm.Phone) };
                    objCustomer.PrimaryEmailAddr = new EmailAddress { Address = query.FirstOrDefault().acc.Email.ToString() };
                    objCustomer.BillAddr = new PhysicalAddress
                    {
                        City = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.City),
                        Line1 = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.Street),
                        PostalCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.Pincode),
                        CountrySubDivisionCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.State),
                        Country = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 1).FirstOrDefault().addr.Country)
                    };
                    objCustomer.ShipAddr = new PhysicalAddress
                    {
                        City = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.City),
                        Line1 = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.Street),
                        PostalCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.Pincode),
                        CountrySubDivisionCode = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.State),
                        Country = Convert.ToString(query.Where(x => x.addr.AddressTypeId == 2).FirstOrDefault().addr.Country)
                    };
                    objCustomer.Fax = new TelephoneNumber
                    {
                        AreaCode = "01",
                        FreeFormNumber = Convert.ToString(query.FirstOrDefault().comm.Fax)
                    };
                    if (term != null)
                    {
                        objCustomer.SalesTermRef = new ReferenceType() { name = term.Name, Value = term.Id };
                    }
                    objCustomer.ResaleNum = dbcustomerOptionalInfo.BusinessReseller;
                    service.Add(objCustomer);
                }
            }
            else
            {
                objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + Convert.ToString(query.FirstOrDefault().cmp.Name).Trim() + "'").FirstOrDefault();
                if ((objCustomer != null))
                {
                    objCustomer.ActiveSpecified = true;
                    objCustomer.Active = false;
                    service.Update(objCustomer);
                }
            }
        }

        private void addEditDeleteProductToQuickBook(int prodId)
        {
            var query = from cloth in db.Clothes
                        join category in db.Categories on cloth.CategoryId equals category.CategoryId
                        join qfr in db.QuickBookFailureRecords on cloth.ClothesId equals qfr.FailureFromId
                        where cloth.ClothesId == prodId
                        select new { cloth, category, qfr };

            QueryService<Item> account = new QueryService<Item>(Objcontext);
            Item objItem = account.ExecuteIdsQuery("Select * From Item where Name='" + Convert.ToString(query.FirstOrDefault().qfr.FailureOriginalValue).Trim() + "'").FirstOrDefault<Item>();

            if (query.Any() && query.Where(x => x.cloth.IsActive == true).Any())
            {
                if (objItem == null)
                {
                    Item objstyle = new Item();
                    objstyle.SpecialItem = true;
                    objstyle.QtyOnHand = 0M;
                    objstyle.QtyOnHandSpecified = true;
                    objstyle.UnitPriceSpecified = true;
                    objstyle.Name = Convert.ToString(query.FirstOrDefault().cloth.StyleNumber);
                    objstyle.FullyQualifiedName = query.FirstOrDefault().category.ParentId + ":" + query.FirstOrDefault().cloth.CategoryId + ":" + query.FirstOrDefault().cloth.StyleNumber;
                    objstyle.Active = true;
                    objstyle.UnitPrice = Convert.ToDecimal(query.FirstOrDefault().cloth.Price);
                    objstyle.UnitPriceSpecified = true;
                    objstyle.PurchaseCost = Convert.ToDecimal(query.FirstOrDefault().cloth.ProductCost);
                    objstyle.PurchaseCostSpecified = true;
                    objstyle.ActiveSpecified = true;
                    objstyle.IncomeAccountRef = new ReferenceType() { name = System.Configuration.ConfigurationManager.AppSettings["quickProductCategory"].ToString() };
                    service.Add<Item>(objstyle);
                    updateQuickBookProduct(Convert.ToString(query.FirstOrDefault().cloth.StyleNumber), prodId, Convert.ToString(query.FirstOrDefault().category.ParentId), Convert.ToString(query.FirstOrDefault().cloth.CategoryId));
                }
                else
                    updateQuickBookProduct(Convert.ToString(query.FirstOrDefault().qfr.FailureOriginalValue).Trim(), prodId, Convert.ToString(query.FirstOrDefault().category.ParentId), Convert.ToString(query.FirstOrDefault().cloth.CategoryId));
            }
            else
            {
                Item objdelete = account.ExecuteIdsQuery("Select * From Item where Name='" + Convert.ToString(query.FirstOrDefault().cloth.StyleNumber).Trim() + "'").FirstOrDefault<Item>();
                if (objdelete != null)
                {
                    objdelete.Active = false;
                    objdelete.ActiveSpecified = true;
                    service.Update<Item>(objdelete);
                }
            }

        }

        string productdesc = string.Empty;

        private void generateInvoiceToQuickBook(int Id)
        {
            //QuickBookItems qb = new QuickBookItems();
            var query = db.Orders.Where(x => x.OrderNumber == Id.ToString()).FirstOrDefault();

            #region MyRegion
            var custQuery = from acc in db.Accounts
                            join cmp in db.Companies on acc.AccountId equals cmp.AccountId
                            join coi in db.CustomerOptionalInfoes on acc.AccountId equals coi.AccountId
                            join trm in db.Terms on coi.TermId equals trm.TermId
                            where acc.AccountId == query.AccountId
                            select new { acc.Email, cmp.Name, TermName = trm.Name };
            string CompnyName = Convert.ToString(custQuery.SingleOrDefault().Name);
            //# Region 'object daleraction"
            Invoice invoice = new Invoice();
            List<Line> objLineList = new List<Line>();
            Customer ObjCustomer = new Customer();
            QueryService<Customer> account = new QueryService<Customer>(Objcontext);

            #endregion

            QueryService<Intuit.Ipp.Data.Term> termQueryService = new QueryService<Intuit.Ipp.Data.Term>(Objcontext);
            Intuit.Ipp.Data.Term salesterm = new Intuit.Ipp.Data.Term();
            string ShippingVia = string.Empty;
            if (custQuery.SingleOrDefault().TermName != null && !string.IsNullOrEmpty(Convert.ToString(custQuery.SingleOrDefault().TermName)))
            {
                salesterm = termQueryService.ExecuteIdsQuery("Select * From Term Where Name='" + custQuery.SingleOrDefault().TermName + "'").FirstOrDefault();
                ShippingVia = Convert.ToString(custQuery.SingleOrDefault().TermName);
            }

            #region Add Customer
            if (string.IsNullOrEmpty(CompnyName) == false)
            {
                ObjCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompnyName.Trim() + "'").FirstOrDefault();

                if (ObjCustomer == null)
                {

                    string strQuery = string.Format("select a.*,t.Name from Accounts a left join CustomerOptionalInfoes coi on a.Id = coi.AccountId  left join Terms t on a.TermId=t.ID   where AccountID={1}", query.AccountId);
                    DataTable dt = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();
                    if ((dt != null) && dt.Rows.Count > 0)
                    {
                        dynamic objAddCustomer = new Customer();
                        objAddCustomer.GivenName = Convert.ToString(dt.Rows[0]["FirstName"]);
                        objAddCustomer.FamilyName = Convert.ToString(dt.Rows[0]["LastName"]);
                        objAddCustomer.CompanyName = Convert.ToString(dt.Rows[0]["CompanyName"]);
                        objAddCustomer.DisplayName = Convert.ToString(dt.Rows[0]["CompanyName"]);
                        objAddCustomer.PrimaryPhone = new TelephoneNumber { FreeFormNumber = Convert.ToString(dt.Rows[0]["Phone"]) };
                        if ((dt.Rows[0]["Email1"] == DBNull.Value) && !string.IsNullOrEmpty(Convert.ToString(dt.Rows[0]["Email1"])))
                        {
                            objAddCustomer.PrimaryEmailAddr = new EmailAddress { Address = Convert.ToString(dt.Rows[0]["Email"]) + "," + Convert.ToString(dt.Rows[0]["Email1"]) };
                        }
                        else
                        {
                            objAddCustomer.PrimaryEmailAddr = new EmailAddress { Address = Convert.ToString(dt.Rows[0]["Email"]) };
                        }
                        objAddCustomer.BillAddr = new PhysicalAddress
                        {
                            City = Convert.ToString(dt.Rows[0]["BillingCity"]),
                            Line1 = Convert.ToString(dt.Rows[0]["BillingAddress"]),
                            PostalCode = Convert.ToString(dt.Rows[0]["BillingZip"]),
                            CountrySubDivisionCode = Convert.ToString(dt.Rows[0]["BillingState"]),
                            Country = Convert.ToString(dt.Rows[0]["BillingCountry"])
                        };
                        objAddCustomer.ShipAddr = new PhysicalAddress
                        {
                            City = Convert.ToString(dt.Rows[0]["ShippingCity"]),
                            Line1 = Convert.ToString(dt.Rows[0]["ShippingAddress"]),
                            PostalCode = Convert.ToString(dt.Rows[0]["ShippingZip"]),
                            CountrySubDivisionCode = Convert.ToString(dt.Rows[0]["ShippingState"]),
                            Country = Convert.ToString(dt.Rows[0]["ShippingCountry"])
                        };
                        objAddCustomer.Fax = new TelephoneNumber
                        {
                            AreaCode = "01",
                            FreeFormNumber = Convert.ToString(dt.Rows[0]["Fax"])
                        };
                        if ((salesterm != null))
                        {
                            objAddCustomer.SalesTermRef = new ReferenceType
                            {
                                name = salesterm.Name,
                                Value = salesterm.Id
                            };
                        }
                        service.Add(objAddCustomer);
                    }
                }
                ObjCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompnyName.Trim() + "'").FirstOrDefault();
            #endregion

                #region Create Invoice
                if ((ObjCustomer != null))
                {
                    invoice.CustomerRef = new ReferenceType { Value = ObjCustomer.Id };

                    string strQuery = string.Format("select  i.Name as Inseam,sc.Name as ScaleType,oi.OrderItemID,a.ShippingAddress,a.ShippingCity,a.ShippingState,a.ShippingZip,a.BillingAddress,ois.ScaleText,ois.Quantity, a.BillingCity,a.BillingState,a.BillingZip, c.StyleNumber,c.Price,o.SubmittedOn,o.Address1,o.ParentOrderid,o.Label,o.GrandTotal,o.Discount,o.FinalAmount,os.SizeId,os.Quantity,oi.AccountID,oi.ClothesID,oi.QTY from Orders o left join OrderItems oi on o.OrderID=oi.OrderID left join OrderItem_Size os on os.OrderItemsId=oi.OrderItemID  left join Clothes c on oi.ClothesID=c.ClothesID left join OrderItem_Scale ois on ois.orderItemid=oi.OrderItemID  left join Accounts a on oi.AccountID=a.AccountID  left join Scale_Type sc on sc.ScaleTypeId=oi.ScaleTypeId  left join dbo.Inseam i on i.InseamId=oi.InseamId  where o.OrderID={1} Order By OrderItemId", Id);
                    DataTable dtProduct = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();
                    //DocNumber - QBO Only, otherwise use DocNumber
                    invoice.AutoDocNumber = false;
                    invoice.AutoDocNumberSpecified = true;
                    //invoice.DocNumber = Convert.ToString(ViewState("OrderId"))
                    if ((dtProduct.Rows[0]["ParentOrderid"] != null) && Convert.ToString(dtProduct.Rows[0]["ParentOrderid"]) != "")
                        invoice.DocNumber = Convert.ToString(dtProduct.Rows[0]["ParentOrderid"] + "-" + dtProduct.Rows[0]["Label"]);
                    else
                        invoice.DocNumber = Convert.ToString(Id);
                    //TxnDate
                    invoice.TxnDate = DateTime.Now.Date;
                    invoice.TxnDateSpecified = true;
                    if ((dtProduct.Rows[0]["ParentOrderid"] != null) && Convert.ToString(dtProduct.Rows[0]["ParentOrderid"]) != string.Empty)
                        invoice.TrackingNum = Convert.ToString(dtProduct.Rows[0]["ParentOrderid"] + "-" + dtProduct.Rows[0]["Label"]);
                    else
                        invoice.TrackingNum = Convert.ToString(Id);
                    //#Region "Create sales item line"  
                    if (query.Note != null)
                        invoice.CustomerMemo = new MemoRef() { Value = Convert.ToString(query.Note) };

                    if ((salesterm != null))
                    {
                        invoice.SalesTermRef = new ReferenceType
                        {
                            name = salesterm.Name,
                            Value = salesterm.Id
                        };
                    }

                    DataTable dtOrder = default(DataTable);
                    strQuery = string.Format("Select ShippingCost,ShippingVia,TrackingNo,ShippedOn from Orders where Orderid='{1}'", Id);
                    dtOrder = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();

                    ShippingVia = Convert.ToString(dtOrder.Rows[0]["ShippingVia"]);
                    if (string.IsNullOrEmpty(ShippingVia))
                    {
                        ShippingVia = "0";
                    }
                    string ShippingCost = Convert.ToString(dtOrder.Rows[0]["ShippingCost"]);
                    if (string.IsNullOrEmpty(ShippingCost))
                    {
                        ShippingCost = "0";
                    }
                    string TrackingNo = Convert.ToString(dtOrder.Rows[0]["TrackingNo"]);

                    string ShippedOn = Convert.ToString(dtOrder.Rows[0]["ShippedOn"]);

                    int orderItemId = 1;

                    foreach (DataRow drProd in dtProduct.Rows)
                    {
                        if (orderItemId != Convert.ToInt32(drProd["orderItemid"]))
                        {
                            Line invoiceLine = new Line();
                            orderItemId = Convert.ToInt32(drProd["orderItemid"]);

                            DataTable dtDiscount = new DataTable();
                            strQuery = string.Format("select ClothesId,Price from CustomerItemPrice where ClothesID={1} and AccountId={2} ", drProd["ClothesID"], drProd["AccountID"]);
                            dtOrder = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();

                            //#Region "Style number"
                            strQuery = string.Format("select StyleNumber from Clothes where ClothesID={1}", drProd["ClothesID"]);
                            DataTable dtClothes = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();
                            //#End Region
                            Item Objstyle = new Item();
                            QueryService<Item> product = new QueryService<Item>(Objcontext);
                            Objstyle = product.ExecuteIdsQuery("Select * From Item where Name='" + Convert.ToString(dtClothes.Rows[0]["StyleNumber"]) + "'").FirstOrDefault();

                            if ((dtClothes.Rows[0]["StyleNumber"] != null) && Objstyle == null)
                            {
                                strQuery = string.Format("select c.ProductCost,c.Category,(select s.SubCategory_Name from SubCategory s where s.SubCategory_Id=m.SubCategory_Id ) as subcategory, c.[type] from Clothes c left join Menu m on m.MenuID = c.CategoryId  where c.StyleNumber={1}", dtClothes.Rows[0]["StyleNumber"]);
                                DataTable dtProDetails = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();

                                addProductToQuickBook(Convert.ToInt32(drProd["ClothesID"]), Convert.ToString(dtClothes.Rows[0]["StyleNumber"]), Convert.ToString(drProd["Price"]), Convert.ToString(dtProDetails.Rows[0]["subcategory"]), Convert.ToString(dtProDetails.Rows[0]["type"]), Convert.ToString(dtProDetails.Rows[0]["ProductCost"]));
                                Objstyle = product.ExecuteIdsQuery("Select * From Item where Name='" + Convert.ToString(dtClothes.Rows[0]["StyleNumber"]) + "'").FirstOrDefault();
                            }

                            //#Region "Open size"
                            strQuery = string.Format("select OrderItem_Size.Quantity,Size.Size from OrderItem_Size left join Size on Size.SizeId=OrderItem_Size.SizeId where OrderItemsId={1}", drProd["orderItemid"]);
                            DataTable dtSize = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();
                            //#End Region

                            string Prepack = string.Empty;
                            //Prepack
                            if (!object.ReferenceEquals(drProd["ScaleText"], DBNull.Value))
                            {
                                Prepack = "Scale" + drProd["ScaleText"] + ":" + drProd["Quantity"];
                            }
                            //Size
                            string size = string.Empty;
                            if (dtSize.Rows.Count > 0 && dtSize != null)
                            {
                                foreach (DataRow item in dtSize.Rows)
                                {
                                    if (item["Quantity"] == null || item["Quantity"] == DBNull.Value)
                                    {
                                        size += item["Size"] + " :  0" + ", ";
                                    }
                                    else
                                    {
                                        size += item["Size"] + " : " + item["Quantity"] + ", ";
                                    }
                                }
                                size = Convert.ToString("Size : ") + size;
                                size = size.TrimEnd(new char[] { ',', ' ' });
                            }
                            //Inseam
                            string inseam = string.Empty;
                            if (drProd["Inseam"] != DBNull.Value && drProd["Inseam"] != string.Empty)
                            {
                                inseam = "Inseam : " + drProd["Inseam"];
                            }
                            //Fit
                            string Fit = string.Empty;
                            if (drProd["ScaleType"] != DBNull.Value && drProd["ScaleType"] != string.Empty)
                            {
                                Fit = "Fit : " + drProd["ScaleType"];
                            }
                            //Line Description
                            if (Objstyle != null)
                            {
                                invoiceLine.Id = Objstyle.Id;
                            }
                            strQuery = string.Format("select c.ProductCost,c.Category,(select s.SubCategory_Name from SubCategory s where s.SubCategory_Id=m.SubCategory_Id ) as subcategory, c.[type] from Clothes c left join Menu m on m.MenuID = c.CategoryId  where c.StyleNumber={1}", dtClothes.Rows[0]["StyleNumber"]);

                            DataTable dtProDetails1 = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();

                            if (!string.IsNullOrEmpty(Convert.ToString(dtProDetails1.Rows[0]["type"])))
                            {
                                productdesc = "Category : " + Convert.ToString(dtProDetails1.Rows[0]["category"]).Trim() + " , " + "SubCategory : " + Convert.ToString(dtProDetails1.Rows[0]["subcategory"]) + " , " + "Type : " + Convert.ToString(dtProDetails1.Rows[0]["type"]) + "\n";
                            }

                            if (string.IsNullOrEmpty(inseam) && string.IsNullOrEmpty(Fit))
                            {
                                invoiceLine.Description = productdesc + Convert.ToString(Prepack + "\n" + size);
                            }
                            else
                            {
                                invoiceLine.Description = productdesc + Convert.ToString((Convert.ToString((Convert.ToString(Fit + "\n" + Prepack) + "\n") + inseam) + "             ") + size);
                            }

                            //Line Amount  
                            if (dtDiscount.Rows.Count > 0)
                            {
                                invoiceLine.Amount = Convert.ToDecimal(dtDiscount.Rows[0]["Price"]) * Convert.ToDecimal(drProd["Qty"]);
                                SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
                                //Line Sales Item Line Detail - ItemRef
                                if ((Objstyle != null))
                                {
                                    lineSalesItemLineDetail.ItemRef = new ReferenceType
                                    {
                                        name = Objstyle.Name,
                                        Value = Objstyle.Id
                                    };
                                }
                                lineSalesItemLineDetail.AnyIntuitObject = Convert.ToDecimal(dtDiscount.Rows[0]["Price"]);
                                lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
                                lineSalesItemLineDetail.Qty = Convert.ToInt32(drProd["QTY"]);
                                lineSalesItemLineDetail.QtySpecified = true;
                                lineSalesItemLineDetail.TaxCodeRef = new ReferenceType { Value = "TAX" };
                                lineSalesItemLineDetail.ServiceDate = DateTime.Now.Date;
                                lineSalesItemLineDetail.ServiceDateSpecified = true;
                                invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;
                            }
                            else
                            {
                                invoiceLine.Amount = Convert.ToDecimal(drProd["Price"]) * Convert.ToDecimal(drProd["Qty"]);
                                SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
                                //Line Sales Item Line Detail - ItemRef
                                if ((Objstyle != null))
                                {
                                    lineSalesItemLineDetail.ItemRef = new ReferenceType
                                    {
                                        name = Objstyle.Name,
                                        Value = Objstyle.Id
                                    };
                                }
                                lineSalesItemLineDetail.AnyIntuitObject = Convert.ToDecimal(drProd["Price"]);
                                lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
                                lineSalesItemLineDetail.Qty = Convert.ToInt32(drProd["QTY"]);
                                lineSalesItemLineDetail.QtySpecified = true;
                                lineSalesItemLineDetail.TaxCodeRef = new ReferenceType { Value = "TAX" };
                                lineSalesItemLineDetail.ServiceDate = DateTime.Now.Date;
                                lineSalesItemLineDetail.ServiceDateSpecified = true;
                                //Assign Sales Item Line Detail to Line Item
                                invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;
                            }
                            invoiceLine.AmountSpecified = true;
                            //Line Detail Type
                            invoiceLine.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                            invoiceLine.DetailTypeSpecified = true;
                            updateQuickBookProduct(Convert.ToString(dtClothes.Rows[0]["StyleNumber"]), Convert.ToInt32(drProd["ClothesID"]), "", "");
                            objLineList.Add(invoiceLine);
                        }
                    }

                    if ((dtProduct.Rows[0]["Discount"] != DBNull.Value) && Convert.ToString(dtProduct.Rows[0]["Discount"]) != "")
                    {
                        DiscountLineDetail obj = new DiscountLineDetail();
                        obj.DiscountPercent = Convert.ToInt32(dtProduct.Rows[0]["Discount"]);
                        obj.PercentBased = true;
                        obj.PercentBasedSpecified = true;
                        obj.DiscountPercentSpecified = true;
                        Line objDis = new Line();
                        objDis.DetailType = LineDetailTypeEnum.DiscountLineDetail;
                        objDis.DetailTypeSpecified = true;
                        objDis.AnyIntuitObject = obj;
                        objLineList.Add(objDis);
                    }

                    PhysicalAddress billAddr = new PhysicalAddress();
                    billAddr.Line1 = Convert.ToString(dtProduct.Rows[0]["BillingAddress"]);
                    billAddr.City = Convert.ToString(dtProduct.Rows[0]["BillingCity"]);
                    billAddr.Country = "United States";
                    billAddr.PostalCode = Convert.ToString(dtProduct.Rows[0]["BillingZip"]);
                    invoice.BillAddr = billAddr;
                    invoice.BillEmail = ObjCustomer.PrimaryEmailAddr;
                    PhysicalAddress shipAddr = new PhysicalAddress();
                    shipAddr.Line1 = Convert.ToString(dtProduct.Rows[0]["ShippingAddress"]);
                    shipAddr.City = Convert.ToString(dtProduct.Rows[0]["ShippingCity"]);
                    shipAddr.Country = "United States";
                    shipAddr.PostalCode = Convert.ToString(dtProduct.Rows[0]["ShippingZip"]);
                    invoice.ShipAddr = shipAddr;

                    if ((salesterm != null))
                    {
                        invoice.SalesTermRef = new ReferenceType
                        {
                            name = salesterm.Name,
                            Value = salesterm.Id
                        };
                    }

                    if (!string.IsNullOrEmpty(ShippingVia))
                    {
                        invoice.ShipMethodRef = new ReferenceType
                        {
                            name = ShippingVia,
                            Value = ShippingVia
                        };
                    }
                    if (!string.IsNullOrEmpty(TrackingNo))
                    {
                        invoice.TrackingNum = TrackingNo;
                    }
                    if (!string.IsNullOrEmpty(ShippedOn))
                    {
                        invoice.ShipDate = Convert.ToDateTime(ShippedOn);
                        invoice.ShipDateSpecified = true;
                    }

                    if (!string.IsNullOrEmpty(ShippingCost))
                    {
                        dynamic lineSalesItemLineDetail = new SalesItemLineDetail();
                        lineSalesItemLineDetail.ItemRef = new ReferenceType { Value = "SHIPPING_ITEM_ID" };
                        Line invoiceLineShipCost = new Line();
                        invoiceLineShipCost.Amount = Convert.ToDecimal(ShippingCost);
                        invoiceLineShipCost.AmountSpecified = true;
                        invoiceLineShipCost.AnyIntuitObject = lineSalesItemLineDetail;
                        invoiceLineShipCost.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                        invoiceLineShipCost.DetailTypeSpecified = true;
                        objLineList.Add(invoiceLineShipCost);
                    }
                    invoice.Line = objLineList.ToArray();

                    invoice.DueDate = DateTime.Now.AddDays(30).Date;
                    invoice.DueDateSpecified = true;
                    Invoice invoiceAdded = service.Add<Invoice>(invoice);
                }
                #endregion
            }
        }

        public void addProductToQuickBook(int clothesId, string StyleNumber, string price, string Subcategory, string type, string ProductCost)
        {
            QueryService<Item> account = new QueryService<Item>(Objcontext);

            Item objstyle = new Item();
            objstyle.SpecialItem = true;
            objstyle.UnitPriceSpecified = true;
            objstyle.Name = StyleNumber;
            objstyle.Active = true;
            objstyle.UnitPrice = Convert.ToDecimal(price);
            objstyle.UnitPriceSpecified = true;
            objstyle.PurchaseCost = Convert.ToDecimal(ProductCost);
            objstyle.PurchaseCostSpecified = true;
            objstyle.ActiveSpecified = true;
            objstyle.IncomeAccountRef = new ReferenceType
            {
                name = System.Configuration.ConfigurationManager.AppSettings["quickProductCategory"].ToString()
            };
            service.Add<Item>(objstyle);
            updateQuickBookProduct(StyleNumber, clothesId, Subcategory, type);
        }

        private void updateQuickBookProduct(string styleNumber, int clothesId, string subCategory, string type)
        {
            Display = "";
            Prepack = "";
            var query = db.Clothes.Where(x => x.ClothesId == clothesId);
            if (query.Any())
            {
                DateTime dAtOnceDate = new DateTime(1900, 1, 1);
                string strDate = string.Empty;
                if (query.FirstOrDefault().FutureDeliveryDate == null)
                {
                }
                else
                {
                    strDate = Convert.ToString(query.FirstOrDefault().FutureDeliveryDate);
                    DateTime.TryParse(Convert.ToString(query.FirstOrDefault().FutureDeliveryDate), out dAtOnceDate);
                }

                if (dAtOnceDate.Year != 1900 && strDate != string.Empty)
                {
                    Display += "Color : " + Convert.ToString(query.FirstOrDefault().Color) + "\n" + "Future Product : Yes" + "\n\n";
                }
                else
                {
                    Display += "Color : " + Convert.ToString(query.FirstOrDefault().Color) + "\n" + "Future Product : No" + "\n\n";
                }
            }

            //
            var ScaleDt = from scl in db.ClothesScales
                          join ins in db.Inseams on scl.InseamId equals ins.InseamId
                          join sclsz in db.ClothesScaleSizes on scl.ClothesScaleId equals sclsz.ClothesScaleId
                          join fit in db.Fits on scl.FitId equals fit.FitId
                          where scl.ClothesId == clothesId
                          select new { scl, ins, sclsz, fit };

            var OpenSizeDt = from sclsz in db.ClothesScaleSizes
                             join scl in db.ClothesScales on sclsz.ClothesScaleId equals scl.ClothesScaleId
                             join sz in db.Sizes on sclsz.SizeId equals sz.SizeId
                             where scl.ClothesId == clothesId
                             select new { sclsz, scl, sz };

            var OpenDt = from scl in db.ClothesScales
                         join ins in db.Inseams on scl.InseamId equals ins.InseamId
                         join fit in db.Fits on scl.FitId equals fit.FitId
                         where scl.ClothesId == clothesId
                         select new { scl, ins, fit };

            if (ScaleDt.Any())
            {
                foreach (var dr1 in ScaleDt.ToList())
                {
                    if (Convert.ToInt32(dr1.ins.InseamId) > 0 && Convert.ToInt32(dr1.fit.FitId) > 0)
                    {
                        Prepack = "Scale " + Convert.ToString(dr1.fit.Name) + ": " + Convert.ToString(dr1.scl.InvQty) + "\n";
                        foreach (var drOpen in OpenDt.ToList())
                        {
                            if ((Convert.ToInt32(dr1.ins.InseamId) == Convert.ToInt32(drOpen.ins.InseamId)) && (Convert.ToInt32(dr1.fit.FitId) == Convert.ToInt32(drOpen.fit.FitId)))
                            {
                                //OpensizeDt = db.SelectSQL("select *,(select size from size where size.sizeid=scale_size.sizeid)SizeName from Scale_size where ScaleId ='" + Convert.ToInt32(drOpen["ScaleId"]) + "'");
                                var OpensizeDt = from sclsz in db.ClothesScaleSizes
                                                 join size in db.Sizes on sclsz.SizeId equals size.SizeId
                                                 where sclsz.ClothesScaleId == drOpen.scl.ClothesScaleId
                                                 select new { sclsz, size };


                                if (OpensizeDt.Any())
                                {
                                    foreach (var dr2 in OpensizeDt.ToList())
                                    {
                                        if (Convert.ToInt32(dr2.sclsz.Quantity) > 0)
                                        {
                                            Size += Convert.ToString(dr2.size.Name) + ": " + Convert.ToInt32(dr2.sclsz.Quantity) + ", ";
                                        }
                                        else
                                        {
                                            Size += Convert.ToString(dr2.size.Name) + ": 0, ";
                                        }
                                    }

                                    string Sze = Size.TrimEnd(' ');
                                    Sze = Sze.TrimEnd(',');

                                    Size = "";
                                    Display += (Convert.ToString("\n" + "Fit : " + Convert.ToString(dr1.scl.Name) + "\n" + Prepack + "\n" + "Inseam : " + Convert.ToString(dr1.ins.Name) + "        Size: ") + Sze) + "\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dr3 in ScaleDt.ToList())
                        {
                            Prepack += "Scale " + Convert.ToString(dr3.fit.Name) + ": " + Convert.ToString(dr3.scl.InvQty) + "\n";
                        }
                        foreach (var dr2 in OpenSizeDt.ToList())
                        {
                            if (Convert.ToInt32(dr2.sclsz.Quantity) > 0)
                            {
                                Size += Convert.ToString(dr2.sz.Name) + ": " + Convert.ToInt32(dr2.sclsz.Quantity) + ", ";
                            }
                            else
                            {
                                Size += Convert.ToString(dr2.sz.Name) + ": 0, ";
                            }
                        }
                        string Sze = Size.TrimEnd(' ');
                        Sze = Sze.TrimEnd(',');

                        Display += (Convert.ToString(Prepack + "\n" + "Size: ") + Sze) + "\n";

                        Size = "";

                        break; // TODO: might not be correct. Was : Exit For
                    }
                }
            }
            else
            {
                foreach (var drOpen in OpenDt.ToList())
                {
                    if ((Convert.ToInt32(drOpen.ins.InseamId) > 0) && (Convert.ToInt32(drOpen.fit.FitId) > 0))
                    {
                        //OpensizeDt = db.SelectSQL("select *,(select size from size where size.sizeid=scale_size.sizeid)SizeName from Scale_size where ScaleId ='" + Convert.ToInt32(drOpen["ScaleId"]) + "'");
                        var OpensizeDt = from sclsz in db.ClothesScaleSizes
                                         join size in db.Sizes on sclsz.SizeId equals size.SizeId
                                         where sclsz.ClothesScaleId == drOpen.scl.ClothesScaleId
                                         select new { sclsz, size };

                        if (OpensizeDt.Any())
                        {
                            foreach (var dr2 in OpensizeDt.ToList())
                            {
                                if (Convert.ToInt32(dr2.sclsz.Quantity) > 0)
                                {
                                    Size += Convert.ToString(dr2.size.Name) + ": " + Convert.ToInt32(dr2.sclsz.Quantity) + ", ";
                                }
                                else
                                {
                                    Size += Convert.ToString(dr2.size.Name) + ": 0, ";
                                }
                            }

                            string Sze = Size.TrimEnd(' ');
                            Sze = Sze.TrimEnd(',');

                            Size = "";
                            Display += (Convert.ToString("\n" + "Fit : " + Convert.ToString(drOpen.scl.Name) + "\n" + Prepack + "\n" + "Inseam : " + Convert.ToString(drOpen.ins.Name) + "        Size: ") + Sze) + "\n";
                        }
                    }
                    else
                    {
                        foreach (var dr2 in OpenSizeDt.ToList())
                        {
                            if (Convert.ToInt32(dr2.sclsz.Quantity) > 0)
                            {
                                Size += Convert.ToString(dr2.sz.Name) + ": " + Convert.ToInt32(dr2.sclsz.Quantity) + ", ";
                            }
                            else
                            {
                                Size += Convert.ToString(dr2.sz.Name) + ": 0, ";
                            }
                        }
                        string Sze = Size.TrimEnd(' ');
                        Sze = Sze.TrimEnd(',');

                        Display += (Convert.ToString(Prepack + "\n" + "Size: ") + Sze) + "\n";

                        Size = "";
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }
            }


            try
            {
                QueryService<Item> account = new QueryService<Item>(Objcontext);
                Item objItem = account.ExecuteIdsQuery((Convert.ToString("Select * From Item where Name='") + styleNumber.Trim()) + "'").FirstOrDefault();
                if (objItem != null)
                {
                    if (objItem.Name != Convert.ToString(query.FirstOrDefault().StyleNumber))
                    {
                        objItem.Name = Convert.ToString(query.FirstOrDefault().StyleNumber);
                    }
                    objItem.UnitPrice = Convert.ToDecimal(query.FirstOrDefault().Price);
                    objItem.UnitPriceSpecified = true;
                    objItem.PurchaseCost = Convert.ToDecimal(query.FirstOrDefault().ProductCost);
                    objItem.PurchaseCostSpecified = true;
                    if (type.Trim() != "")
                        productdesc = "SubCategory : " + subCategory.Trim() + "\nType : " + type.Trim() + "\n";
                    string Desc = productdesc + Display;
                    if (Desc.Length < 1000)
                    {
                        objItem.PurchaseDesc = Desc;
                    }
                    else
                    {
                        string str12 = Desc.Substring(0, 996);
                        str12 = str12 + "...";
                        objItem.PurchaseDesc = str12;
                    }
                    service.Update<Item>(objItem);
                    Display = "";
                    Prepack = "";
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

    }
}
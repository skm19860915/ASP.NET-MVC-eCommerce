using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.DB;
using Platini.Models;
using MvcPaging;
using System.Threading;
using Intuit.Ipp.Security;
using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace Platini.Areas.Common.Controllers
{
    public class ClothesController : Controller
    {
        private Entities db = new Entities();
        private bool activeInactive = false;
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

            var clothes = db.Clothes.Where(i => i.IsDelete == false).OrderBy(x => x.CategoryId).ThenBy(x => x.SortOrder).ToList();
            string searchText = searchString;
            if (!ReferenceEquals(searchText, null))
                clothes = clothes.Where(e => e.StyleNumber.ToLower().Contains(searchText.ToLower())).ToList();

            Type sortByPropType = typeof(Cloth).GetProperty(sortColumn).PropertyType;
            List<Cloth> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(Cloth), sortByPropType })
                                        .Invoke(clothes, new object[] { clothes, sortColumn, sortOrder }) as List<Cloth>;

            List<ClothesClass> retList = new List<ClothesClass>().InjectFrom(sortedList);
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(retList.ToPagedList(currentPageIndex, SiteConfiguration.defaultpageSize));
        }

        public ActionResult Step1(int? id)
        {
            var cloth = new StepOneClass();
            cloth.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.Brands = db.Brands.Where(x => x.IsActive == true && x.IsDeleted == false).OrderBy(x => x.SortOrder).ToList();
            cloth.IsActive = true;
            var dbCloth = db.Clothes.Find(id);
            if (dbCloth != null)
            {
                cloth.InjectClass(dbCloth);
                cloth.ClothesDescription = HttpUtility.HtmlDecode(dbCloth.ClothesDescription);
                if (dbCloth.Clearance > 0)
                {
                    cloth.Clearance = true;
                }
                else
                {
                    cloth.Clearance = false;
                }
                cloth.CategoryId = dbCloth.CategoryId;
                cloth.BrandId = dbCloth.BrandId;
                cloth.SId = db.Categories.Where(x => x.CategoryId == cloth.CategoryId).FirstOrDefault().ParentId;
                cloth.CId = db.Categories.Where(x => x.CategoryId == cloth.SId).FirstOrDefault().ParentId;
                cloth.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.CId).ToList().OrderBy(x => x.SortOrder).ToList();
                cloth.CategoryTypes = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.SId).ToList().OrderBy(x => x.SortOrder).ToList();
                cloth.SizeGroups = db.SizeGroups.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryId == dbCloth.CategoryId).ToList();
                var clothesImages = db.ClothesImages.Where(i => i.ClothesId == id && i.IsDelete == false);
                var clothesImage = clothesImages.Where(i => i.SortOrder == 1 && i.IsActive == true && i.IsDelete == false).OrderByDescending(x => x.DateUpdated).FirstOrDefault();
                if (clothesImage != null)
                    cloth.Image1 = clothesImage.ImagePath;
                clothesImage = clothesImages.Where(i => i.SortOrder == 2 && i.IsActive == true && i.IsDelete == false).OrderByDescending(x => x.DateUpdated).FirstOrDefault();
                if (clothesImage != null)
                    cloth.Image2 = clothesImage.ImagePath;
                clothesImage = clothesImages.Where(i => i.SortOrder == 3 && i.IsActive == true && i.IsDelete == false).OrderByDescending(x => x.DateUpdated).FirstOrDefault();
                if (clothesImage != null)
                    cloth.Image3 = clothesImage.ImagePath;
                clothesImage = clothesImages.Where(i => i.SortOrder == 4 && i.IsActive == true && i.IsDelete == false).OrderByDescending(x => x.DateUpdated).FirstOrDefault();
                if (clothesImage != null)
                    cloth.Image4 = clothesImage.ImagePath;
                clothesImage = clothesImages.Where(i => i.SortOrder == 5 && i.IsActive == true && i.IsDelete == false).OrderByDescending(x => x.DateUpdated).FirstOrDefault();
                if (clothesImage != null)
                    cloth.Image5 = clothesImage.ImagePath;
                cloth.IsActive = dbCloth.IsActive.HasValue ? dbCloth.IsActive.Value : false;

                var quantities = db.ClothesScales.Where(x => x.ClothesId == id).ToList();
                var fitIds = quantities.Where(x => x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                var inseamIds = quantities.Where(x => x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();

                cloth.ClothAttribute.FitList = db.Fits.Where(x => x.IsActive == true)
                    .Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = fitIds.Contains(x.FitId) }).ToList();
                cloth.ClothAttribute.InseamList = db.Inseams.Where(x => x.IsActive == true)
                    .Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = inseamIds.Contains(x.InseamId) }).ToList();
            }
            else
            {
                cloth.ClothAttribute.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                cloth.ClothAttribute.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            }
            return View(cloth);
        }
       
        [HttpPost]
        public ActionResult Step1(StepOneClass cloth)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Qty");
            int PreSize = 0;
            if (ModelState.IsValid)
            {
                var chkExist = db.Clothes.Any(x => x.StyleNumber == cloth.StyleNumber && x.IsDelete == false) && db.Clothes.Where(x => x.StyleNumber == cloth.StyleNumber && x.IsDelete == false).All(x => x.ClothesId != cloth.ClothesId);
                if (!chkExist)
                {
                    Cloth dbCloth = db.Clothes.Find(cloth.ClothesId);
                    if (dbCloth != null)
                    {
                        PreSize = dbCloth.SizeGroupId;
                        bool activateChanged = dbCloth.IsActive.HasValue ? dbCloth.IsActive.Value : false;                        
                        dbCloth.InjectClass(cloth);
                        dbCloth.CategoryId = cloth.CategoryId;
                        //if (!activateChanged && cloth.IsActive == true)
                        //    dbCloth.DateChanged = DateTime.UtcNow;
                        dbCloth.IsActive = cloth.IsActive;
                        activeInactive = cloth.IsActive.HasValue ? cloth.IsActive.Value : false;
                        if (cloth.Clearance)
                        {
                            dbCloth.Clearance = 1;
                            dbCloth.SortOrder = int.MaxValue;
                        }
                        else
                        {
                            dbCloth.Clearance = 0;
                        }
                        dbCloth.IsSent = null;
                        dbCloth.BrandId = cloth.BrandId;
                        dbCloth.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                    else
                    {
                        dbCloth = new Cloth();
                        dbCloth.InjectClass(cloth);
                        dbCloth.CategoryId = cloth.CategoryId;
                        dbCloth.IsActive = cloth.IsActive;
                        dbCloth.IsDelete = false;
                        dbCloth.DateCreated = DateTime.UtcNow;
                        dbCloth.DateUpdated = DateTime.UtcNow;
                        if (cloth.Clearance)
                        {
                            dbCloth.Clearance = 1;
                            dbCloth.SortOrder = int.MaxValue;
                        }
                        else
                        {
                            dbCloth.Clearance = 0;
                        }
                        dbCloth.IsSent = null;
                        dbCloth.BrandId = cloth.BrandId;
                        dbCloth.DateCreated = DateTime.UtcNow;
                        dbCloth.DateUpdated = DateTime.UtcNow;
                        dbCloth.DateChanged = DateTime.UtcNow;
                        db.Clothes.Add(dbCloth);
                        db.SaveChanges();
                    }

                    HttpPostedFileBase upPic = Request.Files["file1"];
                    if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                    {
                        SaveUpdateClothesImage(upPic, 1, dbCloth.ClothesId, dbCloth.StyleNumber);
                    }
                    upPic = Request.Files["file2"];
                    if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                    {
                        SaveUpdateClothesImage(upPic, 2, dbCloth.ClothesId, dbCloth.StyleNumber);
                    }
                    upPic = Request.Files["file3"];
                    if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                    {
                        SaveUpdateClothesImage(upPic, 3, dbCloth.ClothesId, dbCloth.StyleNumber);
                    }
                    upPic = Request.Files["file4"];
                    if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                    {
                        SaveUpdateClothesImage(upPic, 4, dbCloth.ClothesId, dbCloth.StyleNumber);
                    }
                    upPic = Request.Files["file5"];
                    if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                    {
                        SaveUpdateClothesImage(upPic, 5, dbCloth.ClothesId, dbCloth.StyleNumber);
                    }
                    SetStyleNumber(dbCloth.ClothesId);
                    Session["selectedFitIds"] = cloth.ClothAttribute.FitList.Where(x => x.IsSelected == true).Select(x => (int?)x.Id).ToList();
                    Session["selectedInseamIds"] = cloth.ClothAttribute.InseamList.Where(x => x.IsSelected == true).Select(x => (int?)x.Id).ToList();

                    return RedirectToAction("Step2", new { id = dbCloth.ClothesId, presize = PreSize });
                }
                else
                {
                    ViewBag.PageMessage = "A cloth with this name already exists.";
                }
            }

            cloth.ClothAttribute.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            cloth.ClothAttribute.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            cloth.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.CId).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.CategoryTypes = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.SId).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.SizeGroups = db.SizeGroups.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryId == cloth.CategoryId).ToList();
            cloth.Brands = db.Brands.Where(x => x.IsActive == true && x.IsDeleted == false).OrderBy(x => x.SortOrder).ToList();
            return View("Step1", cloth);
        }

        public bool SaveUpdateClothesImage(HttpPostedFileBase upPic, int SortOrder, int ClothesId, string StyleNumber)
        {
            string fileName;
            System.Drawing.Imaging.ImageFormat format;
            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
            {
                var clothesImage = db.ClothesImages.Where(i => i.ClothesId == ClothesId && i.SortOrder == SortOrder).FirstOrDefault();
                if (clothesImage != null)
                {
                    string imagePath = clothesImage.ImagePath;
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + imagePath));
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/WebThumb/" + imagePath));
                }
                else
                {
                    clothesImage = new ClothesImage();
                    clothesImage.DateCreated = DateTime.UtcNow;
                }
                clothesImage.ImageName = upPic.FileName.Split('.').FirstOrDefault();
                clothesImage.ImagePath = fileName;
                clothesImage.ClothesId = ClothesId;
                clothesImage.SortOrder = SortOrder;
                clothesImage.IsActive = true;
                clothesImage.IsDelete = false;
                clothesImage.DateUpdated = DateTime.UtcNow;
                db.ClothesImages.Add(clothesImage);
                db.SaveChanges();
                System.Drawing.Image.FromStream(upPic.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                var arrWeb = ImgHelper.ResizeImage(upPic.InputStream, 500, 750, "White", 90, format);
                var arrMob = ImgHelper.ResizeImage(upPic.InputStream, 150, 225, "White", 90, format);
                using (var ms = new System.IO.MemoryStream(arrWeb))
                {
                    System.Drawing.Image.FromStream(ms).Save(Server.MapPath("~/Library/Uploads/WebThumb/" + fileName), format);
                }
                using (var ms = new MemoryStream(arrMob))
                {
                    System.Drawing.Image.FromStream(ms).Save(Server.MapPath("~/Library/Uploads/MobileThumb/" + fileName), format);
                }
                return true;
            }
            return false;
        }

        public bool SaveUpdateClothesImage2(HttpPostedFileBase upPic, int SortOrder, int ClothesId)
        {
            string fileName;
            System.Drawing.Imaging.ImageFormat format;
            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
            {
                var clothesImage = db.ClothesImages.Where(i => i.ClothesId == ClothesId && i.SortOrder == SortOrder).FirstOrDefault();
                if (clothesImage != null)
                {
                    string imagePath = clothesImage.ImagePath;
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + imagePath));
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/WebThumb/" + imagePath));
                }
                else
                {
                    clothesImage = new ClothesImage();
                    clothesImage.DateCreated = DateTime.UtcNow;
                }
                clothesImage.ImageName = upPic.FileName.Split('.').FirstOrDefault();
                clothesImage.ImagePath = fileName;
                clothesImage.ClothesId = ClothesId;
                clothesImage.SortOrder = SortOrder;
                clothesImage.IsActive = true;
                clothesImage.IsDelete = false;
                clothesImage.DateUpdated = DateTime.UtcNow;
                db.ClothesImages.Add(clothesImage);
                db.SaveChanges();
                System.Drawing.Image.FromStream(upPic.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                var arrWeb = ImgHelper.ResizeImage(upPic.InputStream, 500, 750, "White", 90, format);
                var arrMob = ImgHelper.ResizeImage(upPic.InputStream, 150, 225, "White", 90, format);
                using (var ms = new System.IO.MemoryStream(arrWeb))
                {
                    System.Drawing.Image.FromStream(ms).Save(Server.MapPath("~/Library/Uploads/WebThumb/" + fileName), format);
                }
                using (var ms = new MemoryStream(arrMob))
                {
                    System.Drawing.Image.FromStream(ms).Save(Server.MapPath("~/Library/Uploads/MobileThumb/" + fileName), format);
                }
                return true;
            }
            return false;
        }

        public ActionResult Step2(int id, int presize)
        {
            var model = new StepTwoClass();

            model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();

            if (db.Clothes.Where(x => x.ClothesId == id).Any())
            {
                var cloth = db.Clothes.Find(id);
                model.ClothesId = id;
                model.StyleNumber = cloth.StyleNumber;
                model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                var sizeGroupId = db.Clothes.Where(x => x.ClothesId == id).Select(x => x.SizeGroupId).Single();

                if (Session["selectedFitIds"] == null && Session["selectedInseamIds"] == null)
                {
                    return RedirectToAction("Step1", new { id = id });
                }

                List<int?> dbFitList = cloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                List<int?> dbInseamList = cloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();

                List<int?> FitList = new List<int?>();
                if (Session["selectedFitIds"] != null)
                {
                    FitList = Session["selectedFitIds"] as List<int?>;
                }

                List<int?> InseamList = new List<int?>();
                if (Session["selectedInseamIds"] != null)
                {
                    InseamList = Session["selectedInseamIds"] as List<int?>;
                }
                bool modify = false;
                if (dbFitList.Except(FitList).ToList().Any() || FitList.Except(dbFitList).ToList().Any() || dbInseamList.Except(InseamList).Any() || InseamList.Except(dbInseamList).Any())
                    modify = true;
                if (FitList.Count() == 0 && InseamList.Count() == 0)
                    modify = true;
                if (FitList.Count() == 0)
                    FitList.Add(null);
                if (InseamList.Count() == 0)
                    InseamList.Add(null);
                foreach (var fitid in FitList)
                {
                    foreach (var inseamid in InseamList)
                    {
                        var scaleList = db.ClothesScales.Where(x => x.ClothesId == id).ToList();
                        var prepackList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false);
                        var opensizeList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true);
                        if (opensizeList.Count() > 0)
                        {
                            foreach (var scale in opensizeList)
                            {                                
                                var openSizesForOS = new List<ClothesScaleSizeClass>();
                                var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                foreach (var item in sSQOpenSize)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = item.ClothesScaleId;
                                    scaleRow.SizeId = item.SizeId;
                                    scaleRow.SizeName = item.Size.Name;
                                    scaleRow.Quantity = item.Quantity;                                   
                                    openSizesForOS.Add(scaleRow);
                                }
                                if (sizeGroupId != presize && presize > 0)
                                {
                                    openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var openSizes = db.Sizes.Where(x => x.SizeGroupId == sizeGroupId && x.IsActive == true).Select(x => new ClothesScaleSizeClass { SizeId = x.SizeId, ClothesScaleId = 0, ClothesScaleSizeId = 0, Quantity = 0, SizeName = x.Name }).ToList();                                   
                                    foreach (var item in openSizes)
                                    {
                                        var findSize = sSQOpenSize.Where(x => x.Size.Name == item.SizeName).FirstOrDefault();
                                        if (findSize != null)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = findSize.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = findSize.ClothesScaleId;
                                            scaleRow.SizeId = findSize.SizeId;
                                            scaleRow.SizeName = findSize.Size.Name;
                                            scaleRow.Quantity = findSize.Quantity.HasValue ? findSize.Quantity.Value : 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        else
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = item.ClothesScaleId;
                                            scaleRow.SizeId = item.SizeId;
                                            scaleRow.SizeName = item.SizeName;
                                            scaleRow.Quantity = item.Quantity.HasValue ? item.Quantity.Value : 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                    }
                                }                            
                                var availableOpenSizeItem = new ClothesScaleClass();
                                availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizesForOS);
                                availableOpenSizeItem.InseamId = scale.InseamId;
                                availableOpenSizeItem.FitId = scale.FitId;
                                availableOpenSizeItem.IsOpenSize = true;
                                availableOpenSizeItem.Name = scale.Name;
                                availableOpenSizeItem.InvQty = scale.InvQty;
                                if (fitid.HasValue)
                                    availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                if (inseamid.HasValue)
                                    availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                availableOpenSizeItem.ClothesId = scale.ClothesId;
                                availableOpenSizeItem.ClothesScaleId = scale.ClothesScaleId;
                                model.AvailableOpenSizes.Add(availableOpenSizeItem);
                            }
                        }
                        else if (modify || prepackList.Any())
                        {
                            var openSizes = db.Sizes.Where(x => x.SizeGroupId == sizeGroupId && x.IsActive == true).Select(x => new ClothesScaleSizeClass { SizeId = x.SizeId, ClothesScaleId = 0, ClothesScaleSizeId = 0, Quantity = 0, SizeName = x.Name }).ToList();
                            var availableOpenSizeItem = new ClothesScaleClass();
                            availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizes);
                            if (fitid.HasValue)
                                availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                            if (inseamid.HasValue)
                                availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                            availableOpenSizeItem.InseamId = inseamid;
                            availableOpenSizeItem.FitId = fitid;
                            availableOpenSizeItem.IsOpenSize = true;
                            availableOpenSizeItem.InvQty = 0;
                            availableOpenSizeItem.ClothesId = id;
                            availableOpenSizeItem.ClothesScaleId = 0;
                            model.AvailableOpenSizes.Add(availableOpenSizeItem);
                        }

                        if (prepackList.Count() > 0)
                        {
                            foreach (var scale in prepackList)
                            {                               
                                var openSizesForPP = new List<ClothesScaleSizeClass>();
                                var sSQPrePacks = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                foreach (var item in sSQPrePacks)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = item.ClothesScaleId;
                                    scaleRow.SizeId = item.SizeId;
                                    scaleRow.SizeName = item.Size.Name;
                                    scaleRow.Quantity = item.Quantity;                                   
                                    openSizesForPP.Add(scaleRow);
                                }
                                if (sizeGroupId != presize && presize > 0)
                                {
                                    openSizesForPP = new List<ClothesScaleSizeClass>();
                                    var openSizes = db.Sizes.Where(x => x.SizeGroupId == sizeGroupId && x.IsActive == true).Select(x => new ClothesScaleSizeClass { SizeId = x.SizeId, ClothesScaleId = 0, ClothesScaleSizeId = 0, Quantity = 0, SizeName = x.Name }).ToList();
                                    foreach (var item in openSizes)
                                    {
                                        var findSize = sSQPrePacks.Where(x => x.Size.Name == item.SizeName).FirstOrDefault();
                                        if (findSize != null)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = findSize.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = findSize.ClothesScaleId;
                                            scaleRow.SizeId = findSize.SizeId;
                                            scaleRow.SizeName = findSize.Size.Name;
                                            scaleRow.Quantity = findSize.Quantity.HasValue ? findSize.Quantity.Value : 0;
                                            openSizesForPP.Add(scaleRow);
                                        }
                                        else
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = item.ClothesScaleId;
                                            scaleRow.SizeId = item.SizeId;
                                            scaleRow.SizeName = item.SizeName;
                                            scaleRow.Quantity = item.Quantity.HasValue ? item.Quantity.Value : 0;
                                            openSizesForPP.Add(scaleRow);
                                        }
                                    }
                                }

                                var availablePrePacksItem = new ClothesScaleClass();
                                availablePrePacksItem.ClothesScaleSizeClass.AddRange(openSizesForPP);
                                availablePrePacksItem.InseamId = scale.InseamId;
                                availablePrePacksItem.FitId = scale.FitId;
                                availablePrePacksItem.IsOpenSize = false;
                                if (fitid.HasValue)
                                    availablePrePacksItem.selectedFitId = fitid.Value;
                                if (inseamid.HasValue)
                                    availablePrePacksItem.selectedInseamId = inseamid.Value;
                                availablePrePacksItem.Name = scale.Name;
                                availablePrePacksItem.InvQty = scale.InvQty;
                                availablePrePacksItem.ClothesId = scale.ClothesId;
                                availablePrePacksItem.ClothesScaleId = scale.ClothesScaleId;
                                model.AvailablePrePacks.Add(availablePrePacksItem);
                            }
                        }
                        else if (modify)
                        {
                            var prePacksSizes = db.Sizes.Where(x => x.SizeGroupId == sizeGroupId && x.IsActive == true).Select(x => new ClothesScaleSizeClass { SizeId = x.SizeId, ClothesScaleId = 0, ClothesScaleSizeId = 0, Quantity = 0, SizeName = x.Name }).ToList();
                            var availablePrePacksItem = new ClothesScaleClass();
                            availablePrePacksItem.ClothesScaleSizeClass.AddRange(prePacksSizes);
                            if (fitid.HasValue)
                                availablePrePacksItem.selectedFitId = fitid.Value;
                            if (inseamid.HasValue)
                                availablePrePacksItem.selectedInseamId = inseamid.Value;
                            availablePrePacksItem.InseamId = inseamid;
                            availablePrePacksItem.FitId = fitid;
                            availablePrePacksItem.IsOpenSize = false;
                            availablePrePacksItem.InvQty = 0;
                            availablePrePacksItem.ClothesId = id;
                            availablePrePacksItem.ClothesScaleId = 0;
                            model.AvailablePrePacks.Add(availablePrePacksItem);
                        }

                    }
                }
            }
            return View(model);
        }

        public ActionResult MassUpload(string Ids)
        {
            var cloth = new StepOneClass();
            if (!string.IsNullOrEmpty(Ids))
            {
                cloth.Ids = Ids;
                cloth.Qty = Ids.Split(',').Count();
                int[] values = cloth.Ids.Split(',').Select(s => int.Parse(s)).ToArray();
                if (values.FirstOrDefault() > 0)
                {
                    var dbCloth = db.Clothes.Find(values.FirstOrDefault());
                    if (dbCloth != null)
                    {
                        cloth.InjectClass(dbCloth);
                        cloth.CategoryId = dbCloth.CategoryId;
                        cloth.BrandId = dbCloth.BrandId;
                        cloth.ClothesDescription = dbCloth.ClothesDescription;
                        cloth.SId = db.Categories.Where(x => x.CategoryId == cloth.CategoryId).FirstOrDefault().ParentId;
                        cloth.CId = db.Categories.Where(x => x.CategoryId == cloth.SId).FirstOrDefault().ParentId;
                        cloth.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.CId).ToList().OrderBy(x => x.SortOrder).ToList();
                        cloth.CategoryTypes = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.SId).ToList().OrderBy(x => x.SortOrder).ToList();
                        cloth.SizeGroups = db.SizeGroups.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryId == dbCloth.CategoryId).ToList();
                    }
                }
            }
            cloth.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.Brands = db.Brands.Where(x => x.IsActive == true && x.IsDeleted == false).OrderBy(x => x.SortOrder).ToList();
            cloth.IsActive = true;
            cloth.ClothAttribute.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            cloth.ClothAttribute.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            cloth.Quantites = new List<int>();
            cloth.Quantites.AddRange(Enumerable.Range(1, 20));
            return View(cloth);
        }

        [HttpPost]
        public ActionResult MassUpload(StepOneClass cloth)
        {
            string value = string.Empty;
            ModelState.Remove("Id");
            ModelState.Remove("StyleNumber");
            if (ModelState.IsValid)
            {
                var list = new List<int>();
                if (!string.IsNullOrEmpty(cloth.Ids))
                {
                    int[] values = cloth.Ids.Split(',').Select(s => int.Parse(s)).ToArray();

                    foreach (var id in values)
                    {
                        Cloth dbCloth = db.Clothes.Find(id);
                        if (dbCloth != null)
                        {
                            dbCloth.Price = cloth.Price.HasValue ? cloth.Price.Value : dbCloth.Price;
                            dbCloth.ProductCost = cloth.ProductCost.HasValue ? cloth.ProductCost.Value : dbCloth.ProductCost;
                            dbCloth.MSRP = cloth.MSRP.HasValue ? cloth.MSRP.Value : dbCloth.MSRP;
                            dbCloth.FutureDeliveryDate = cloth.FutureDeliveryDate.HasValue ? cloth.FutureDeliveryDate.Value : dbCloth.FutureDeliveryDate;
                            dbCloth.CategoryId = cloth.CategoryId;
                            dbCloth.Tags = cloth.Tags;
                            dbCloth.ClothesDescription = cloth.ClothesDescription;
                            //dbCloth.IsActive = cloth.IsActive;
                            dbCloth.Clearance = null;
                            dbCloth.IsSent = null;
                            dbCloth.BrandId = cloth.BrandId;
                            dbCloth.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                        }
                    }
                    list = values.ToList();
                    Session["uploads"] = list;
                }
                else
                {
                    for (int i = 0; i < cloth.Qty; i++)
                    {
                        var dbCloth = new Cloth();
                        dbCloth.InjectClass(cloth);
                        string StyleNumber = DateTime.Now.Ticks.ToString();
                        dbCloth.StyleNumber = StyleNumber;
                        dbCloth.OldStyleNumber = StyleNumber;
                        dbCloth.CategoryId = cloth.CategoryId;
                        dbCloth.ClothesDescription = cloth.ClothesDescription;
                        dbCloth.IsActive = true;
                        dbCloth.SortOrder = 0;
                        dbCloth.IsDelete = false;
                        dbCloth.DateCreated = DateTime.UtcNow;
                        dbCloth.DateUpdated = DateTime.UtcNow;
                        dbCloth.Clearance = 0;
                        dbCloth.IsSent = null;
                        dbCloth.BrandId = cloth.BrandId;
                        dbCloth.DateCreated = DateTime.UtcNow;
                        dbCloth.DateUpdated = DateTime.UtcNow;
                        dbCloth.DateChanged = DateTime.UtcNow;
                        db.Clothes.Add(dbCloth);
                        db.SaveChanges();
                        Thread.Sleep(5);
                        list.Add(dbCloth.ClothesId);
                    }
                    if (cloth.FutureDeliveryDate != null && cloth.FutureDeliveryDate >= DateTime.UtcNow)
                        value = "1";
                    Session["uploads"] = list;
                }

                Session["selectedFitIds"] = cloth.ClothAttribute.FitList.Where(x => x.IsSelected == true).Select(x => (int?)x.Id).ToList();
                Session["selectedInseamIds"] = cloth.ClothAttribute.InseamList.Where(x => x.IsSelected == true).Select(x => (int?)x.Id).ToList();


                return RedirectToAction("MassUpload2", new { id = list.FirstOrDefault(), future = value });
            }

            cloth.ClothAttribute.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            cloth.ClothAttribute.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            cloth.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.CId).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.CategoryTypes = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == cloth.SId).ToList().OrderBy(x => x.SortOrder).ToList();
            cloth.SizeGroups = db.SizeGroups.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryId == cloth.CategoryId).ToList();
            cloth.Brands = db.Brands.Where(x => x.IsActive == true && x.IsDeleted == false).OrderBy(x => x.SortOrder).ToList();
            cloth.Quantites = new List<int>();
            cloth.Quantites.AddRange(Enumerable.Range(1, 20));
            return View("MassUpload", cloth);
        }


        public ActionResult MassUpload2(int id, string future)
        {
            var model = new StepTwoClass();
            model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            if (Session["uploads"] != null && (Session["selectedFitIds"] != null || Session["selectedInseamIds"] != null))
            {
                if (db.Clothes.Where(x => x.ClothesId == id).Any())
                {
                    var cloth = db.Clothes.Find(id);
                    model.ClothesId = id;
                    model.StyleNumber = cloth.StyleNumber;
                    model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                    var sizeGroupId = db.Clothes.Where(x => x.ClothesId == id).Select(x => x.SizeGroupId).Single();


                    List<int?> dbFitList = cloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                    List<int?> dbInseamList = cloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();

                    List<int?> FitList = new List<int?>();
                    if (Session["selectedFitIds"] != null)
                    {
                        FitList = Session["selectedFitIds"] as List<int?>;
                    }

                    List<int?> InseamList = new List<int?>();
                    if (Session["selectedInseamIds"] != null)
                    {
                        InseamList = Session["selectedInseamIds"] as List<int?>;
                    }
                    bool modify = false;
                    if (dbFitList.Except(FitList).ToList().Any() || FitList.Except(dbFitList).ToList().Any() || dbInseamList.Except(InseamList).Any() || InseamList.Except(dbInseamList).Any())
                        modify = true;
                    if (FitList.Count() == 0 && InseamList.Count() == 0)
                        modify = true;
                    if (FitList.Count() == 0)
                        FitList.Add(null);
                    if (InseamList.Count() == 0)
                        InseamList.Add(null);
                    foreach (var fitid in FitList)
                    {
                        foreach (var inseamid in InseamList)
                        {
                            var scaleList = db.ClothesScales.Where(x => x.ClothesId == id).ToList();
                            var prepackList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false);
                            var opensizeList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true);
                            if (opensizeList.Count() > 0)
                            {
                                foreach (var scale in opensizeList)
                                {
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                    foreach (var item in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = item.ClothesScaleId;
                                        scaleRow.SizeId = item.SizeId;
                                        scaleRow.SizeName = item.Size.Name;
                                        scaleRow.Quantity = item.Quantity;
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    var availableOpenSizeItem = new ClothesScaleClass();
                                    availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizesForOS);
                                    availableOpenSizeItem.InseamId = scale.InseamId;
                                    availableOpenSizeItem.FitId = scale.FitId;
                                    availableOpenSizeItem.IsOpenSize = true;
                                    availableOpenSizeItem.Name = scale.Name;
                                    availableOpenSizeItem.InvQty = scale.InvQty;
                                    if (fitid.HasValue)
                                        availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                    if (inseamid.HasValue)
                                        availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                    availableOpenSizeItem.ClothesId = scale.ClothesId;
                                    availableOpenSizeItem.ClothesScaleId = scale.ClothesScaleId;
                                    model.AvailableOpenSizes.Add(availableOpenSizeItem);
                                }
                            }
                            else if (modify || prepackList.Any())
                            {
                                var openSizes = db.Sizes.Where(x => x.SizeGroupId == sizeGroupId && x.IsActive == true).Select(x => new ClothesScaleSizeClass { SizeId = x.SizeId, ClothesScaleId = 0, ClothesScaleSizeId = 0, Quantity = 0, SizeName = x.Name }).ToList();
                                var availableOpenSizeItem = new ClothesScaleClass();
                                availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizes);
                                if (fitid.HasValue)
                                    availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                if (inseamid.HasValue)
                                    availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                availableOpenSizeItem.InseamId = inseamid;
                                availableOpenSizeItem.FitId = fitid;
                                availableOpenSizeItem.IsOpenSize = true;
                                availableOpenSizeItem.InvQty = 0;
                                availableOpenSizeItem.ClothesId = id;
                                availableOpenSizeItem.ClothesScaleId = 0;
                                model.AvailableOpenSizes.Add(availableOpenSizeItem);
                            }

                            if (prepackList.Count() > 0)
                            {
                                foreach (var scale in prepackList)
                                {
                                    var openSizesForPP = new List<ClothesScaleSizeClass>();
                                    var sSQPrePacks = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                    foreach (var item in sSQPrePacks)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = item.ClothesScaleId;
                                        scaleRow.SizeId = item.SizeId;
                                        scaleRow.SizeName = item.Size.Name;
                                        scaleRow.Quantity = item.Quantity;
                                        openSizesForPP.Add(scaleRow);
                                    }
                                    var availablePrePacksItem = new ClothesScaleClass();
                                    availablePrePacksItem.ClothesScaleSizeClass.AddRange(openSizesForPP);
                                    availablePrePacksItem.InseamId = scale.InseamId;
                                    availablePrePacksItem.FitId = scale.FitId;
                                    availablePrePacksItem.IsOpenSize = false;
                                    if (fitid.HasValue)
                                        availablePrePacksItem.selectedFitId = fitid.Value;
                                    if (inseamid.HasValue)
                                        availablePrePacksItem.selectedInseamId = inseamid.Value;
                                    availablePrePacksItem.Name = scale.Name;
                                    availablePrePacksItem.InvQty = scale.InvQty;
                                    availablePrePacksItem.ClothesId = scale.ClothesId;
                                    availablePrePacksItem.ClothesScaleId = scale.ClothesScaleId;
                                    model.AvailablePrePacks.Add(availablePrePacksItem);
                                }
                            }
                            else if (modify)
                            {
                                var prePacksSizes = db.Sizes.Where(x => x.SizeGroupId == sizeGroupId && x.IsActive == true).Select(x => new ClothesScaleSizeClass { SizeId = x.SizeId, ClothesScaleId = 0, ClothesScaleSizeId = 0, Quantity = 0, SizeName = x.Name }).ToList();
                                var availablePrePacksItem = new ClothesScaleClass();
                                availablePrePacksItem.ClothesScaleSizeClass.AddRange(prePacksSizes);
                                if (fitid.HasValue)
                                    availablePrePacksItem.selectedFitId = fitid.Value;
                                if (inseamid.HasValue)
                                    availablePrePacksItem.selectedInseamId = inseamid.Value;
                                availablePrePacksItem.InseamId = inseamid;
                                availablePrePacksItem.FitId = fitid;
                                availablePrePacksItem.IsOpenSize = false;
                                availablePrePacksItem.InvQty = 0;
                                availablePrePacksItem.ClothesId = id;
                                availablePrePacksItem.ClothesScaleId = 0;
                                model.AvailablePrePacks.Add(availablePrePacksItem);
                            }

                        }
                    }
                }
                var list = (List<int>)Session["uploads"];
                model.Ids = string.Join(",", list);
                model.isFuture = future;
                return View("MassUpload2", model);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult MassUpload2(StepTwoClass model)
        {
            string value = string.Empty;
            int[] values = model.Ids.Split(',').Select(s => int.Parse(s)).ToArray();
            foreach (var id in values)
            {
                foreach (var item in model.AvailableOpenSizes)
                {
                    ClothesScale dbClothesScale = new ClothesScale();
                    dbClothesScale.Name = item.Name;
                    dbClothesScale.IsOpenSize = true;
                    dbClothesScale.ClothesId = id;
                    dbClothesScale.InseamId = item.InseamId;
                    dbClothesScale.FitId = item.FitId;
                    dbClothesScale.InvQty = item.InvQty;
                    dbClothesScale.IsActive = true;
                    dbClothesScale.IsDelete = false;
                    dbClothesScale.DateCreated = DateTime.UtcNow;
                    db.ClothesScales.Add(dbClothesScale);
                    dbClothesScale.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    foreach (var element in item.ClothesScaleSizeClass)
                    {
                        ClothesScaleSize dbClothesScaleSize = new ClothesScaleSize();
                        dbClothesScaleSize.ClothesScaleId = dbClothesScale.ClothesScaleId;
                        dbClothesScaleSize.SizeId = element.SizeId;
                        dbClothesScaleSize.Quantity = element.Quantity;
                        dbClothesScaleSize.DateUpdated = DateTime.UtcNow;
                        dbClothesScaleSize.IsActive = true;
                        dbClothesScaleSize.IsDelete = false;
                        dbClothesScaleSize.DateCreated = DateTime.UtcNow;
                        db.ClothesScaleSizes.Add(dbClothesScaleSize);
                        dbClothesScaleSize.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }

                foreach (var item in model.AvailablePrePacks)
                {
                    ClothesScale dbClothesScale = new ClothesScale();
                    dbClothesScale.Name = item.Name;
                    dbClothesScale.IsOpenSize = false;
                    dbClothesScale.ClothesId = id;
                    dbClothesScale.InseamId = item.InseamId;
                    dbClothesScale.FitId = item.FitId;
                    dbClothesScale.InvQty = item.InvQty;
                    if (item.selectedFitId > 0)
                        dbClothesScale.FitId = item.selectedFitId;
                    if (item.selectedInseamId > 0)
                        dbClothesScale.InseamId = item.selectedInseamId;
                    if (dbClothesScale.FitId <= 0)
                        dbClothesScale.FitId = null;
                    if (dbClothesScale.InseamId <= 0)
                        dbClothesScale.InseamId = null;

                    dbClothesScale.IsActive = true;
                    dbClothesScale.IsDelete = false;
                    dbClothesScale.DateCreated = DateTime.UtcNow;
                    db.ClothesScales.Add(dbClothesScale);
                    dbClothesScale.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();

                    foreach (var element in item.ClothesScaleSizeClass)
                    {
                        ClothesScaleSize dbClothesScaleSize = new ClothesScaleSize();
                        dbClothesScaleSize.ClothesScaleId = dbClothesScale.ClothesScaleId;
                        dbClothesScaleSize.SizeId = element.SizeId;
                        dbClothesScaleSize.Quantity = element.Quantity;
                        dbClothesScaleSize.IsActive = true;
                        dbClothesScaleSize.IsDelete = false;
                        dbClothesScaleSize.DateCreated = DateTime.UtcNow;
                        db.ClothesScaleSizes.Add(dbClothesScaleSize);
                        dbClothesScaleSize.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }
            }
            if (QuickBookStrings.UseQuickBook())
            {
                foreach (var id in values)
                {
                    AddProductToQuickBook(id);
                }
            }
            Session["selectedFitIds"] = null;
            Session["selectedInseamIds"] = null;
            if (Request["hNew"] != null && Request["hNew"].ToString() == "1")
                return RedirectToAction("Index", "Clothes");
            else
                return RedirectToAction("LineSheet", "Home", new { ids = model.Ids, future = model.isFuture, @area = "" });
        }

        [HttpPost]
        public ActionResult Step2(StepTwoClass model)
        {
            foreach (var item in model.AvailableOpenSizes)
            {
                ClothesScale dbClothesScale = db.ClothesScales.Find(item.ClothesScaleId);
                if (dbClothesScale == null)
                    dbClothesScale = new ClothesScale();
                dbClothesScale.Name = item.Name;
                dbClothesScale.IsOpenSize = true;
                dbClothesScale.ClothesId = item.ClothesId;
                dbClothesScale.InseamId = item.InseamId;
                dbClothesScale.FitId = item.FitId;
                dbClothesScale.InvQty = item.InvQty;
                if (dbClothesScale.ClothesScaleId == 0)
                {
                    dbClothesScale.IsActive = true;
                    dbClothesScale.IsDelete = false;
                    dbClothesScale.DateCreated = DateTime.UtcNow;
                    db.ClothesScales.Add(dbClothesScale);
                }
                dbClothesScale.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                foreach (var element in item.ClothesScaleSizeClass)
                {
                    ClothesScaleSize dbClothesScaleSize = db.ClothesScaleSizes.Find(element.ClothesScaleSizeId);
                    if (dbClothesScaleSize == null)
                        dbClothesScaleSize = new ClothesScaleSize();
                    dbClothesScaleSize.ClothesScaleId = dbClothesScale.ClothesScaleId;
                    dbClothesScaleSize.SizeId = element.SizeId;
                    dbClothesScaleSize.Quantity = element.Quantity;
                    dbClothesScaleSize.DateUpdated = DateTime.UtcNow;
                    if (dbClothesScaleSize.ClothesScaleSizeId == 0)
                    {
                        dbClothesScaleSize.IsActive = true;
                        dbClothesScaleSize.IsDelete = false;
                        dbClothesScaleSize.DateCreated = DateTime.UtcNow;
                        db.ClothesScaleSizes.Add(dbClothesScaleSize);
                    }
                    dbClothesScaleSize.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }
            }

            foreach (var item in model.AvailablePrePacks)
            {
                ClothesScale dbClothesScale = db.ClothesScales.Find(item.ClothesScaleId);
                if (dbClothesScale == null)
                    dbClothesScale = new ClothesScale();
                dbClothesScale.Name = item.Name;
                dbClothesScale.IsOpenSize = false;
                dbClothesScale.ClothesId = item.ClothesId;
                dbClothesScale.InseamId = item.InseamId;
                dbClothesScale.FitId = item.FitId;
                dbClothesScale.InvQty = item.InvQty;
                if (item.selectedFitId > 0)
                    dbClothesScale.FitId = item.selectedFitId;
                if (item.selectedInseamId > 0)
                    dbClothesScale.InseamId = item.selectedInseamId;
                if (dbClothesScale.FitId <= 0)
                    dbClothesScale.FitId = null;
                if (dbClothesScale.InseamId <= 0)
                    dbClothesScale.InseamId = null;
                if (dbClothesScale.ClothesScaleId == 0)
                {
                    dbClothesScale.IsActive = true;
                    dbClothesScale.IsDelete = false;
                    dbClothesScale.DateCreated = DateTime.UtcNow;
                    db.ClothesScales.Add(dbClothesScale);
                }
                dbClothesScale.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();

                foreach (var element in item.ClothesScaleSizeClass)
                {
                    ClothesScaleSize dbClothesScaleSize = db.ClothesScaleSizes.Find(element.ClothesScaleSizeId);
                    if (dbClothesScaleSize == null)
                        dbClothesScaleSize = new ClothesScaleSize();
                    dbClothesScaleSize.ClothesScaleId = dbClothesScale.ClothesScaleId;
                    dbClothesScaleSize.SizeId = element.SizeId;
                    dbClothesScaleSize.Quantity = element.Quantity;
                    if (dbClothesScaleSize.ClothesScaleSizeId == 0)
                    {
                        dbClothesScaleSize.IsActive = true;
                        dbClothesScaleSize.IsDelete = false;
                        dbClothesScaleSize.DateCreated = DateTime.UtcNow;
                        db.ClothesScaleSizes.Add(dbClothesScaleSize);
                    }
                    dbClothesScaleSize.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }
            }
            var cloth = db.Clothes.Find(model.ClothesId);
            if (cloth != null)
            {
                int quant = (cloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                         + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                if (!cloth.OriginalQty.HasValue)
                    cloth.OriginalQty = quant;
                int soldquant = cloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).
                         SelectMany(x => x.ClothesScaleSizes.Where(y => y.OrderSizes.Any()), (x, y) => y).
                         SelectMany(y => db.OrderSizes.Where(z => z.ClothesSizeId == y.ClothesScaleSizeId), (y, z) => z).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0) +
                 cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false && x.OrderScales.Any()).
                         Select(x => new { pack = x, quant = x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)) }).
                         SelectMany(y => db.OrderScales.Where(z => z.ClothesScaleId == y.pack.ClothesScaleId), (y, z) => new { oPack = z, y.quant }).Sum(x => x.quant * (x.oPack.Quantity.HasValue ? x.oPack.Quantity.Value : 0));
                cloth.AdjustQty = quant - (cloth.OriginalQty.Value - soldquant);
                cloth.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
            }
            if (QuickBookStrings.UseQuickBook())
                AddProductToQuickBook(model.ClothesId);
            Session["selectedFitIds"] = null;
            Session["selectedInseamIds"] = null;
            if (Request["hNew"] != null && Request["hNew"].ToString() == "1")
                return RedirectToAction("Step1", "Clothes");
            else
                return RedirectToAction("Detail", "Home", new { @area = "", @id = cloth.StyleNumber });
        }

        [HttpPost]
        public ActionResult FileUpload(IEnumerable<HttpPostedFileBase> files, string clothesId, string StyleNumber)
        {
            int ClothesId = 0;
            int.TryParse(clothesId, out ClothesId);
            if (ClothesId > 0 && files != null)
            {
                int so = 1;
                var imageOrder = db.ClothesImages.Where(x => x.ClothesId == ClothesId && x.IsActive && !x.IsDelete).ToList().OrderBy(x => x.SortOrder).LastOrDefault();
                if (imageOrder != null)
                    so = (imageOrder.SortOrder.HasValue ? imageOrder.SortOrder.Value : 0) + 1;
                foreach (HttpPostedFileBase file in files)
                {
                    if (SaveUpdateClothesImage2(file, so, ClothesId))
                    {
                        return Json("All files have been successfully stored.");
                    }
                }
            }
            return Json("error");
        }

        [HttpPost]
        public ActionResult SetStyleNumber(int clothesId)
        {
            var cloth = db.Clothes.Find(clothesId);
            bool updated = false;
            if (cloth != null)
            {
                int existLength = cloth.StyleNumber.Length;
                if (cloth.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                {
                    var mainImage = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete && !string.IsNullOrEmpty(x.ImageName) && x.ImageName.Length < existLength).OrderBy(x => x.ImageName.Length).FirstOrDefault();
                    if (mainImage != null)
                    {
                        mainImage.SortOrder = 0;
                        //var existcloth = db.Clothes.FirstOrDefault(x => x.ClothesId != cloth.ClothesId && x.StyleNumber == mainImage.ImageName);
                        var existcloth = db.Clothes.FirstOrDefault(x => x.StyleNumber == mainImage.ImageName);
                        if (existcloth == null)
                            cloth.StyleNumber = mainImage.ImageName;
                        else
                            cloth.StyleNumber = mainImage.ImageName + "_" + cloth.ClothesId;
                        cloth.DateUpdated = DateTime.UtcNow;
                        updated = true;
                        db.SaveChanges();
                    }
                    if (updated)
                    {
                        db.ClothesImages.Where(x => x.ClothesId == cloth.ClothesId && x.IsActive && !x.IsDelete).ToList().ForEach(x => { x.SortOrder = x.SortOrder + 1; x.DateUpdated = DateTime.UtcNow; });
                        db.SaveChanges();
                    }
                }
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("Failure", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetImages(string clothesId)
        {
            int ClothesId = 0;
            int.TryParse(clothesId, out ClothesId);
            if (ClothesId > 0)
            {
                var clothesImages = db.ClothesImages.Where(i => i.ClothesId == ClothesId && i.IsActive && i.IsDelete == false).OrderBy(x => x.SortOrder).ToList().Select(x => new
                {
                    ClothesImageId = x.ClothesImageId,
                    ImageName = x.ImageName,
                    ImagePath = x.ImagePath,
                    SortOrder = x.SortOrder
                });
                return Json(clothesImages, JsonRequestBehavior.AllowGet);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SaveSort(string NewOrders)
        {
            if (!string.IsNullOrEmpty(NewOrders))
            {
                var collection = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SortOrders>>(NewOrders);
                foreach (var item in collection)
                {
                    int ClothesImageId = 0;
                    int.TryParse(item.id, out ClothesImageId);
                    if (ClothesImageId > 0)
                    {
                        var image = db.ClothesImages.Find(ClothesImageId);
                        if (image != null && !image.IsDelete && image.IsActive && image.SortOrder != item.so)
                        {
                            image.SortOrder = item.so;
                            image.DateUpdated = DateTime.UtcNow;
                        }
                        db.SaveChanges();
                    }
                }
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteImage(int ClothesId)
        {
            if (ClothesId > 0)
            {
                var image = db.ClothesImages.Find(ClothesId);
                if (image != null)
                {
                    if (!string.IsNullOrEmpty(image.ImagePath))
                    {
                        System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + image.ImagePath));
                        System.IO.File.Delete(Server.MapPath("~/Library/Uploads/WebThumb/" + image.ImagePath));
                    }
                    image.IsDelete = true;
                    image.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    var clothesImages = db.ClothesImages.Where(i => i.ClothesId == image.ClothesId && i.IsActive && i.IsDelete == false).OrderBy(x => x.SortOrder).ToList().Select(x => new
                    {
                        ClothesImageId = x.ClothesImageId,
                        ImageName = x.ImageName,
                        ImagePath = x.ImagePath,
                        SortOrder = x.SortOrder
                    });
                    return Json(clothesImages, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult AddNewScale(int id, int fit, int inseam, int gid)
        {
            var cloth = db.Clothes.Single(x => x.ClothesId == id);
            var list = db.Sizes.Where(x => x.SizeGroupId == cloth.SizeGroupId).Select(x => new ClothesScaleSizeClass { ClothesScaleId = 0, ClothesScaleSizeId = 0, SizeId = x.SizeId, Quantity = 0, SizeName = x.Name }).ToList();
            var model = new ClothesScaleClass();
            model.ClothesScaleSizeClass = list;
            model.Name = string.Empty;
            model.InvQty = 0;
            model.IsOpenSize = false;
            model.FitId = -1;
            model.InseamId = -1;
            model.ClothesId = id;
            if (fit == 1)
            {
                model.selectedFitId = -1;
            }
            if (inseam == 1)
            {
                model.selectedInseamId = -1;
            }

            model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            ViewData.TemplateInfo.HtmlFieldPrefix = "AvailablePrePacks[" + gid + "]";
            return PartialView(model);
        }

        [HttpGet]
        public ActionResult GetRelatedProducts(int clothId)
        {
            var relatedProductDialogModel = new RelatedProductClass();
            relatedProductDialogModel.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            relatedProductDialogModel.ClothesId = clothId;
            relatedProductDialogModel.StyleNumber = db.Clothes.Single(x => x.ClothesId == clothId).StyleNumber;
            var prodList1 = db.RelatedClothes.Where(x => x.ClothesId == clothId && x.IsActive == true && x.IsDelete == false).Select(x => x.RelClothesId);
            var prodList2 = db.RelatedClothes.Where(x => x.RelClothesId == clothId && x.IsActive == true && x.IsDelete == false).Select(x => x.ClothesId);
            var prodList = new List<int>();
            prodList.AddRange(prodList1);
            prodList.AddRange(prodList2);
            prodList = prodList.FindAll(x => x != clothId).Distinct().ToList();
            relatedProductDialogModel.RelatedProducts = db.Clothes.Where(x => prodList.Contains(x.ClothesId) && x.IsActive == true && x.IsDelete == false).Select(y => new RelatedProductListItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, SubCategoryName = db.Categories.FirstOrDefault(x => x.CategoryId == y.Category.ParentId).Name, CategoryName = db.Categories.FirstOrDefault(x => x.CategoryId == db.Categories.FirstOrDefault(q => q.CategoryId == y.Category.ParentId).ParentId).Name, StyleNumber = y.StyleNumber }).ToList();
            return PartialView("RelatedProduct", relatedProductDialogModel);
        }

        [HttpPost]
        public void SaveRelatedProducts(List<int> RelatedProductIds, int clothId, int categoryId)
        {
            try
            {
                var deleteList = db.RelatedClothes.Where(x => (x.ClothesId == clothId || x.RelClothesId == clothId) && x.CategoryId == categoryId).ToList();
                foreach (var deleteItem in deleteList)
                {
                    db.RelatedClothes.Remove(deleteItem);
                    db.SaveChanges();
                }

                foreach (var relatedId in RelatedProductIds)
                {
                    var addItem = new RelatedClothe();
                    addItem.ClothesId = clothId;
                    addItem.RelClothesId = relatedId;
                    addItem.CategoryId = categoryId;
                    addItem.IsActive = true;
                    addItem.IsDelete = false;
                    addItem.DateCreated = DateTime.UtcNow;
                    addItem.DateUpdated = DateTime.UtcNow;
                    db.RelatedClothes.Add(addItem);
                    db.SaveChanges();
                }
            }
            catch { }
        }

        public ActionResult ClothesImageDelete(int id, int sid)
        {
            var clothesImage = db.ClothesImages.Where(i => i.ClothesId == id && i.SortOrder == sid).FirstOrDefault();
            if (clothesImage != null)
            {
                string imagePath = clothesImage.ImagePath;
                System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + imagePath));
                System.IO.File.Delete(Server.MapPath("~/Library/Uploads/WebThumb/" + imagePath));
                db.ClothesImages.Remove(clothesImage);
                db.SaveChanges();
            }
            return RedirectToAction("Step1", "Clothes", new { id = id });
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetAllProducts(int categoryId, int clothId)
        {
            var list = new AllProductListClass();
            var relatedProductIds = db.RelatedClothes.Where(x => x.ClothesId == clothId && x.CategoryId == categoryId).Select(x => x.RelClothesId).ToList();
            relatedProductIds.AddRange(db.RelatedClothes.Where(x => x.CategoryId == categoryId && x.RelClothesId == clothId && !relatedProductIds.Contains(x.RelClothesId)).Select(x => x.ClothesId));
            relatedProductIds = relatedProductIds.Distinct().ToList();
            list.AllProductList = db.Clothes.Where(x => x.CategoryId == categoryId && x.IsActive == true && x.ClothesId != clothId)
                             .Select(y => new AllProductListItem { ClothesId = y.ClothesId, StyleNumber = y.StyleNumber, Checked = relatedProductIds.Contains(y.ClothesId) }).ToList();

            return PartialView("GetAllProducts", list);
        }

        [HttpGet]
        public ActionResult GetAllColors(int categoryId, int clothId)
        {
            var list = new AllProductListClass();
            var relatedProductIds = db.RelatedColors.Where(x => x.ClothesId == clothId && x.CategoryId == categoryId).Select(x => x.RelClothesId).ToList();
            relatedProductIds.AddRange(db.RelatedColors.Where(x => x.CategoryId == categoryId && x.RelClothesId == clothId && !relatedProductIds.Contains(x.RelClothesId)).Select(x => x.ClothesId));
            relatedProductIds = relatedProductIds.Distinct().ToList();
            list.AllProductList = db.Clothes.Where(x => x.CategoryId == categoryId && x.IsActive == true && x.ClothesId != clothId)
                             .Select(y => new AllProductListItem { ClothesId = y.ClothesId, StyleNumber = y.StyleNumber, Checked = relatedProductIds.Contains(y.ClothesId) }).ToList();
            ViewBag.CatId = categoryId;
            ViewBag.ClothId = clothId;
            return PartialView("GetAllColors", list);
        }
        [HttpPost]
        public ActionResult SaveRelatedColors(string RelatedProductIds, int clothId, int categoryId)
        {
            try
            {
                var deleteList = db.RelatedColors.Where(x => (x.ClothesId == clothId || x.RelClothesId == clothId) && x.CategoryId == categoryId).ToList();
                foreach (var deleteItem in deleteList)
                {
                    db.RelatedColors.Remove(deleteItem);
                }
                db.SaveChanges();
                if (!string.IsNullOrEmpty(RelatedProductIds))
                {
                    var values = RelatedProductIds.Trim(',').Split(',').Select(x => int.Parse(x));
                    foreach (var relatedId in values)
                    {
                        var addItem = new RelatedColor();
                        addItem.ClothesId = clothId;
                        addItem.RelClothesId = relatedId;
                        addItem.CategoryId = categoryId;
                        addItem.IsActive = true;
                        addItem.IsDelete = false;
                        addItem.DateCreated = DateTime.UtcNow;
                        addItem.DateUpdated = DateTime.UtcNow;
                        db.RelatedColors.Add(addItem);
                    }
                    db.SaveChanges();
                }

            }
            catch { }
            return Json("", JsonRequestBehavior.AllowGet);
        }
        public string GetCategories(int Id = 0)
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
                        strParent = dbCategory.Name + " >> " + strParent;
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

        [NonAction]
        protected bool VerifyImage(string contentType, out string fileName, out System.Drawing.Imaging.ImageFormat format)
        {
            bool proceed = true;
            fileName = Guid.NewGuid().ToString().GetHashCode().ToString("x");
            format = System.Drawing.Imaging.ImageFormat.Jpeg;
            switch (contentType)
            {
                case "image/jpg":
                    fileName += ".jpg";
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case "image/jpeg":
                    fileName += ".jpeg";
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case "image/png":
                    fileName += ".png";
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    break;
                case "image/gif":
                    fileName += ".gif";
                    format = System.Drawing.Imaging.ImageFormat.Gif;
                    break;
                case "image/bmp":
                    fileName += ".bmp";
                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                default:
                    fileName = "";
                    format = null;
                    proceed = false;
                    break;
            }
            return proceed;
        }

        [HttpPost]
        public ActionResult UpdateMatrix(int id, string mode)
        {
            if (id != 0 && !string.IsNullOrEmpty(mode))
            {
                if (mode.Trim().ToLower() == "matrix")
                {
                    var scales = db.ClothesScales.Where(x => x.ClothesId == id).ToList();
                    if (scales.Count() > 0)
                    {
                        foreach (var item in scales)
                        {
                            var quantities = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == item.ClothesScaleId).ToList();
                            if (quantities.Count() > 0)
                            {
                                foreach (var sizeitem in quantities)
                                {
                                    db.ClothesScaleSizes.Remove(sizeitem);
                                    db.SaveChanges();
                                }
                            }
                            db.ClothesScales.Remove(item);
                            db.SaveChanges();
                        }
                    }
                }
                else if (mode.Trim().ToLower() == "adjust")
                {
                    var cloth = db.Clothes.Find(id);
                    if (cloth != null)
                    {
                        cloth.AdjustQty = 0;
                        cloth.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }
                else if (mode.Trim().ToLower() == "currentqty")
                {
                    var cloth = db.Clothes.Find(id);
                    if (cloth != null)
                    {
                        cloth.OriginalQty = 0;
                        int Qty = (cloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        cloth.OriginalQty = Qty;
                        cloth.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Step1", new { id = id });
        }

        [HttpPost]
        public ActionResult RemoveScale(int id, int clothId)
        {
            if (id != 0)
            {
                var item = db.ClothesScales.Find(id);
                db.ClothesScales.Remove(item);
                db.SaveChanges();
                return RedirectToAction("Step1", new { id = clothId });
            }
            else
            {
                return RedirectToAction("Step1", new { id = clothId });
            }
        }

        [HttpPost]
        public ActionResult ActivateCloth(int Id)
        {
            var dCloth = db.Clothes.Find(Id);
            if (dCloth != null && dCloth.IsDelete == false)
            {
                dCloth.IsActive = true;
                dCloth.DateUpdated = DateTime.UtcNow;
                dCloth.DateChanged = DateTime.UtcNow;
                db.SaveChanges();
                return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteCloth(int Id)
        {
            Cloth clothe = db.Clothes.Find(Id);
            if (clothe != null)
            {
                clothe.IsDelete = true;
                clothe.DateUpdated = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("DeactivatedProducts", "Home");
        }

        [HttpPost]
        public ActionResult DeleteClothes(int Id)
        {
            Cloth clothe = db.Clothes.Find(Id);
            if (clothe != null && clothe.IsDelete == false)
            {
                clothe.IsDelete = true;
                clothe.DateUpdated = DateTime.Now;
                db.SaveChanges();
                return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TagMaps(int? page, string searchString, string sortOrder, string sortColumn = "Singular")
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

            var maps = db.TagMappings.ToList();
            string searchText = searchString;
            if (!ReferenceEquals(searchText, null))
                maps = maps.Where(e => e.Singular.ToLower().Contains(searchText.ToLower())).ToList();

            Type sortByPropType = typeof(TagMapping).GetProperty(sortColumn).PropertyType;
            List<TagMapping> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(TagMapping), sortByPropType })
                                        .Invoke(maps, new object[] { maps, sortColumn, sortOrder }) as List<TagMapping>;

            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(sortedList.ToPagedList(currentPageIndex, SiteConfiguration.defaultpageSize));
        }

        public ActionResult CreateMap()
        {
            return View("MapCreateEdit");
        }

        [HttpPost]
        public ActionResult CreateMap(DB.TagMapping ret)
        {

            if (!string.IsNullOrEmpty(ret.Singular) && !string.IsNullOrEmpty(ret.Plurals))
            {
                var dbMap = db.TagMappings.FirstOrDefault(x => x.Singular == ret.Singular);
                if (dbMap == null)
                {
                    db.TagMappings.Add(ret);
                    db.SaveChanges();
                    return RedirectToAction("TagMaps");
                }
                else
                    ViewBag.PageMessage = "Record exists already";
            }
            else
            {
                if (string.IsNullOrEmpty(ret.Singular))
                    ModelState.AddModelError("Singular", "This field is required");
                if (string.IsNullOrEmpty(ret.Plurals))
                    ModelState.AddModelError("Plurals", "This field is required");
            }
            return View("MapCreateEdit", ret);
        }

        public ActionResult EditMap(int Id)
        {
            var map = db.TagMappings.Find(Id);
            if (map != null)
            {
                return View("MapCreateEdit", map);
            }
            TempData["PageMessage"] = "Record not found";
            return RedirectToAction("TagMaps");
        }

        [HttpPost]
        public ActionResult EditMap(DB.TagMapping ret)
        {

            if (!string.IsNullOrEmpty(ret.Singular) && !string.IsNullOrEmpty(ret.Plurals))
            {
                var dbMap = db.TagMappings.FirstOrDefault(x => x.Singular == ret.Singular && x.MappingId != ret.MappingId);
                if (dbMap == null)
                {
                    dbMap = db.TagMappings.Find(ret.MappingId);
                    dbMap.Singular = ret.Singular;
                    dbMap.Plurals = ret.Plurals;
                    db.SaveChanges();
                    return RedirectToAction("TagMaps");
                }
                else
                    ViewBag.PageMessage = "Record exists already";
            }
            else
            {
                if (string.IsNullOrEmpty(ret.Singular))
                    ModelState.AddModelError("Singular", "This field is required");
                if (string.IsNullOrEmpty(ret.Plurals))
                    ModelState.AddModelError("Plurals", "This field is required");
            }
            return View("MapCreateEdit", ret);
        }

        public ActionResult DeleteMap(int Id)
        {
            var map = db.TagMappings.Find(Id);
            if (map != null)
            {
                db.TagMappings.Remove(map);
                db.SaveChanges();
            }
            else
                TempData["PageMessage"] = "Record not found";
            return RedirectToAction("TagMaps");
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
                        StyleNumber = !string.IsNullOrEmpty(dbCloth.OldStyleNumber) ? dbCloth.OldStyleNumber : dbCloth.StyleNumber;
                }
                if (!string.IsNullOrEmpty(StyleNumber) && dbCloth != null)
                {
                    newProduct = productService.ExecuteIdsQuery("Select * From Item where Name='" + StyleNumber + "'").FirstOrDefault();
                    if (newProduct == null)
                    {
                        newProduct = new Item();
                        newProduct.SpecialItem = true;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.Name = dbCloth.StyleNumber;
                        newProduct.FullyQualifiedName = GetNames(dbCloth, 1);                        
                        newProduct.Active = activeInactive;
                        newProduct.PurchaseDesc = GetNames(dbCloth, 2);
                        string str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                        string noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                        newProduct.Description = noHTML;
                        newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                        newProduct.UnitPriceSpecified = true;                        
                        newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                        newProduct.PurchaseCostSpecified = true;
                        newProduct.ActiveSpecified = true;
                        newProduct.InvStartDate = DateTime.UtcNow;
                        newProduct.InvStartDateSpecified = true;
                        //newProduct.Type = ItemTypeEnum.Inventory;
                        newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        newProduct.QtyOnHandSpecified = true;
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
                        newProduct.Name = dbCloth.StyleNumber;
                        newProduct.FullyQualifiedName = GetNames(dbCloth, 1);                        
                        newProduct.Active = activeInactive;
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
                            newItem.Description = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                            str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                            noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                            newItem.Description = noHTML;
                            newItem.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                            newItem.UnitPriceSpecified = true;
                            newItem.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                            + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                            newItem.QtyOnHandSpecified = true;
                            newItem.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                            newItem.PurchaseCostSpecified = true;
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
                    dbCloth.OldStyleNumber = dbCloth.StyleNumber;
                    db.SaveChanges();
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
            else
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
    }
}

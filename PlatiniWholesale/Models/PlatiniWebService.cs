using HiQPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MvcPaging;
namespace Platini.Models
{
    public class PlatiniWebService
    {        
        public static string GetPdf(string url)
        {
            byte[] array = CreatePdf(url);
            string str2 = Guid.NewGuid().ToString();
            string str3;
            try
            {
                FileStream fileStream = new FileStream(HttpContext.Current.Server.MapPath("~/Screenshot") + "/" + str2 + ".pdf", FileMode.Create);
                fileStream.Write(array, 0, array.Length);
                fileStream.Flush();
                fileStream.Close();
                str3 = HttpContext.Current.Server.MapPath("~/Screenshot") + "/" + str2 + ".pdf";
            }
            catch (Exception ex)
            {
                str3 = ex.Message;
            }
            return str3;
        }

        public static bool RemovePdf(string path)
        {
            bool flag = false;
            if (File.Exists(HttpContext.Current.Server.MapPath(path)))
            {
                File.Delete(HttpContext.Current.Server.MapPath(path));
                flag = true;
            }
            return flag;
        }

        public static byte[] CreatePdf(string URL)
        {
            HtmlToPdf htmlToPdf = new HtmlToPdf();
            htmlToPdf.SerialNumber = "Uho7AwI2NB47MCAzICtnfGpyY3JjcmNia2VyYWN8Y2B8a2traw==";
            htmlToPdf.Document.PageSize = PdfPageSize.A4;
            htmlToPdf.Document.PageOrientation = PdfPageOrientation.Portrait;
            htmlToPdf.Document.Margins = new PdfMargins(5f);
            htmlToPdf.Document.FontEmbedding = false;
            htmlToPdf.Document.Compress = true;
            htmlToPdf.Document.ImagesCompression = 50;
            htmlToPdf.Document.Security.OpenPassword = "";
            htmlToPdf.Document.Security.AllowPrinting = true;
            htmlToPdf.SetDepFilePath(HttpContext.Current.Server.MapPath("~/Screenshot") + "/" + "HiQPdf.dep");
            byte[] toPdf = htmlToPdf.ConvertUrlToMemory(URL);
            return toPdf;
        }
        public static Document PdfCreater(Document document, PdfWriter writer, IPagedList<LineSheetViewClass> Model, bool isFootHead)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var font1 = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLUE);
            var cell = new PdfPCell();
            var text = new Chunk("", font);
            var remainingPageSpace = 0f;


            var logoTable = new PdfPTable(2);
            var logoPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/images"), "logo_header.png");
            FileInfo lgfi = new FileInfo(logoPath);
            if (lgfi.Exists)
            {
                var _logo = iTextSharp.text.Image.GetInstance(logoPath);
                System.Drawing.Bitmap img = new System.Drawing.Bitmap(lgfi.FullName);
                _logo.ScaleAbsolute(70, 12);
                var logocell = new PdfPCell(_logo);
                // logocell.HorizontalAlignment = Element.ALIGN_CENTER;
                logocell.Padding = 0;
                logocell.PaddingTop = -25f;
                logocell.PaddingBottom = 10f;
                logocell.BorderWidth = 0;
                logocell.BorderWidthRight = 0;
                logoTable.AddCell(logocell);
            }
            else
            {
                cell = new PdfPCell(new Phrase(" "));
                cell.Padding = 0;
                cell.PaddingTop = -25f;
                cell.PaddingBottom = 10f;
                cell.BorderWidth = 0;
                logoTable.AddCell(cell);
            }
            if (!isFootHead)
            {
                var urlpara = new Paragraph("www.platinijeans.com", FontFactory.GetFont(FontFactory.HELVETICA, 12, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml("#CC3300"))));
                cell = new PdfPCell(urlpara);
                cell.Padding = 0;
                cell.PaddingTop = -25f;
                cell.PaddingBottom = 10f;
                cell.BorderWidth = 0;
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                logoTable.AddCell(cell);

            }
            else
            {
                cell = new PdfPCell(new Phrase(" "));
                cell.Padding = 0;
                cell.PaddingTop = -25f;
                cell.PaddingBottom = 10f;
                cell.BorderWidth = 0;
                logoTable.AddCell(cell);
            }
            document.Add(logoTable);
            //document.Add(new Paragraph("\n"));

            for (int i = 0; i < Model.Count; ++i)
            {
                //--switch to New Page-----------------

                float content_reqSpace = Model[i].ClothesScale.Count * 75f;
                //float _dv = Model[i].Images.Count / 3;
                //float _rm = Model[i].Images.Count % 3;
                //float _count = _rm > 0 ? _dv + 1 : _dv;
                //float img_reqSpace = 140f + (20f * _count);
                float img_reqSpace = 180f;
                float finalReq = content_reqSpace > img_reqSpace ? content_reqSpace : img_reqSpace;
                finalReq = finalReq + 30f;
                remainingPageSpace = writer.GetVerticalPosition(false) - document.BottomMargin;
                //remainingPageSpace = writer.GetVerticalPosition(false);
                if (remainingPageSpace < finalReq)
                    document.NewPage();

                var masterTable = new PdfPTable(2);
                float[] width = new float[] { 250f, 1150f };
                masterTable.SetWidths(width);
                var masterCell = new PdfPCell();
                masterCell.Colspan = 2;
                masterCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                masterCell.PaddingTop = 5;
                masterCell.PaddingBottom = 5;

                var table1 = new PdfPTable(!isFootHead ? 10 : 8);
                if (!isFootHead)
                    width = new float[] { 50f, 340f, 100f, 140f, 110f, 140f, 100f, 160f, 90f, 160f };
                else
                    width = new float[] { 50f, 340f, 110f, 140f, 100f, 160f, 90f, 160f };
                table1.SetWidths(width);
                var para = new Paragraph("Style", font);

                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BorderWidth = 0;

                cell.PaddingLeft = -10;
                table1.AddCell(cell);
                para = new Paragraph(Model[i].StyleNumber, font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BackgroundColor = BaseColor.WHITE;
                cell.PaddingBottom = 5;
                cell.BorderWidth = 0.5f;
                table1.AddCell(cell);

                if (!isFootHead)
                {
                    para = new Paragraph(" Price $", font);
                    cell = new PdfPCell();
                    cell.AddElement(para);
                    cell.BorderWidth = 0;
                    cell.PaddingLeft = 2;
                    table1.AddCell(cell);

                    para = new Paragraph(string.Format("{0:0.00}", Model[i].Price), font);
                    para.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.AddElement(para);
                    cell.BackgroundColor = BaseColor.WHITE;
                    cell.BorderWidth = 0.5f;
                    cell.PaddingBottom = 5;
                    table1.AddCell(cell);
                }

                para = new Paragraph(" MSRP $", font);
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BorderWidth = 0;
                cell.PaddingLeft = 2;
                table1.AddCell(cell);

                para = new Paragraph(string.Format("{0:0.00}", Model[i].MSRP), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderWidth = 0.5f;
                cell.PaddingBottom = 5;
                table1.AddCell(cell);

                //para = new Paragraph(" Cost $", font);
                //cell = new PdfPCell();
                //cell.AddElement(para);
                //cell.BorderWidth = 0;
                //cell.PaddingLeft = 2;
                //table1.AddCell(cell);

                //para = new Paragraph(String.Format("{0:0.00}", Model[i].Cost), font);
                //para.Alignment = Element.ALIGN_CENTER;
                //cell = new PdfPCell();
                //cell.AddElement(para);
                //cell.BackgroundColor = BaseColor.WHITE;
                //cell.BorderWidth = 0.5f;
                //cell.PaddingBottom = 5;
                //table1.AddCell(cell);

                para = new Paragraph(" Color", font);
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BorderWidth = 0;
                cell.PaddingLeft = 2;
                table1.AddCell(cell);

                para = new Paragraph(Model[i].Color, font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderWidth = 0.5f;
                cell.PaddingBottom = 5;
                table1.AddCell(cell);

                cell = new PdfPCell(new Phrase(" "));
                cell.BorderWidth = 0;
                table1.AddCell(cell);

                cell = new PdfPCell(new Phrase(" "));
                cell.BorderWidth = 0;
                table1.AddCell(cell);

                masterCell.AddElement(table1);
                masterTable.AddCell(masterCell);


                var _Largeimg = new PdfPTable(5);
                width = new float[] { 50f, 50f, 50f, 50f, 50f };
                _Largeimg.SetWidths(width);
                string imagePath = "";
                iTextSharp.text.Image image = null;
                if (Model[i].Images.Any())
                {
                    imagePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/WebThumb/"), Model[i].Images[0].ImagePath);
                    FileInfo fi = new FileInfo(imagePath);
                    if (fi.Exists)
                    {
                        image = iTextSharp.text.Image.GetInstance(imagePath);
                        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                        image.ScaleAbsolute(112, 160);
                        var _cellfor = new PdfPCell(image);
                        _cellfor.Colspan = 5;
                        _cellfor.BorderWidth = 0;
                        _Largeimg.AddCell(_cellfor);

                    }
                    else
                    {
                        var _cellfor = new PdfPCell(new Phrase(" "));
                        _cellfor.Colspan = 5;
                        _cellfor.BorderWidth = 0;
                        _Largeimg.AddCell(_cellfor);
                    }
                    //int l = 0;
                    //for (int s = 1; s < Model[i].Images.Count; s++)
                    //{
                    //    l = l + 1;
                    //    imagePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/"), Model[i].Images[s].ImagePath);
                    //    fi = new FileInfo(imagePath);
                    //    if (fi.Exists)
                    //    {
                    //        image = iTextSharp.text.Image.GetInstance(imagePath);
                    //        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                    //        image.ScaleAbsolute(20, 25);
                    //        var _cellfor = new PdfPCell(image);
                    //        _cellfor.Padding = 0;
                    //        _cellfor.BorderWidth = 0;
                    //        _cellfor.PaddingTop = 2;
                    //        _cellfor.PaddingLeft = 2;
                    //        _cellfor.PaddingRight = 3;
                    //        _Largeimg.AddCell(_cellfor);
                    //    }
                    //    else
                    //    {
                    //        var _cellfor = new PdfPCell(new Phrase(" "));
                    //        _cellfor.Padding = 0;
                    //        _cellfor.BorderWidth = 0;
                    //        _cellfor.PaddingTop = 2;
                    //        _cellfor.PaddingLeft = 2;
                    //        _cellfor.PaddingRight = 3;
                    //        _Largeimg.AddCell(_cellfor);
                    //    }
                    //}

                    //var remainloop = l > 5 ? (l - 5) + 1 : l < 5 ? 5 - l : 0;
                    //if (remainloop > 0)
                    //{
                    //    for (int z = 0; z < remainloop; z++)
                    //    {
                    //        var _cellfor = new PdfPCell(new Phrase(" "));
                    //        _cellfor.Padding = 0;
                    //        _cellfor.BorderWidth = 0;
                    //        _cellfor.PaddingTop = 2;
                    //        _Largeimg.AddCell(_cellfor);
                    //    }
                    //}

                    masterCell = new PdfPCell(_Largeimg);

                    masterCell.Padding = 0;
                    masterCell.PaddingTop = 5;
                    masterCell.PaddingRight = 2;
                    masterCell.PaddingLeft = 5;
                    masterCell.PaddingBottom = 5;
                    masterCell.BorderWidth = 0.5f;
                    masterCell.BorderWidthRight = 0;
                }
                else
                {
                    imagePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/images"), "NO_IMAGE.jpg");
                    FileInfo fi = new FileInfo(imagePath);
                    if (fi.Exists)
                    {
                        image = iTextSharp.text.Image.GetInstance(imagePath);
                        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                        //if (img.Width > 600)
                        //    image.ScaleAbsoluteWidth(600);
                        //else if (img.Height > 600)
                        //    image.ScaleAbsoluteHeight(600);
                        //else
                        image.ScaleAbsolute(112, 160);
                        var _cellfor = new PdfPCell(image);
                        _cellfor.Colspan = 5;
                        _cellfor.Padding = 0;
                        _cellfor.BorderWidth = 0;
                        _cellfor.BorderWidthRight = 0;
                        _Largeimg.AddCell(_cellfor);
                        masterCell = new PdfPCell(_Largeimg);
                        masterCell.Padding = 0;
                        masterCell.PaddingTop = 5;
                        masterCell.PaddingRight = 2;
                        masterCell.PaddingLeft = 5;
                        masterCell.PaddingBottom = 5;
                        masterCell.BorderWidth = 0.5f;
                        masterCell.BorderWidthRight = 0;

                    }
                }
                masterTable.AddCell(masterCell);

                int colorIndex = 0;
                bool checkIndex = false;
                string FitName = "";
                var colors = new string[] { "#0000FF", "#FF0000", "#daa520" };
                //--mASTER CELL 3


                var cTable_MC3 = new PdfPTable(1);

                var innerTable_Up = new PdfPTable(2);
                width = new float[] { 100f, 1050f };
                innerTable_Up.SetWidths(width);


                bool _In = false;
                for (int k = 0; k < Model[i].ClothesScale.Count; k++)
                {
                    _In = true;
                    if (checkIndex != Model[i].ClothesScale[k].IsOpenSize.Value)
                    {
                        FitName = string.Empty;
                    }
                    if (Model[i].ClothesScale[k].IsOpenSize.Value == true)
                    {

                        if (FitName != Model[i].ClothesScale[k].FitName)
                        {
                            colorIndex = 0;
                            var innrPara = new Paragraph(Model[i].ClothesScale[k].FitName, font1);
                            var _1cell_innrTbl = new PdfPCell();
                            _1cell_innrTbl.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                            _1cell_innrTbl.BorderWidth = 0;
                            _1cell_innrTbl.PaddingLeft = 2;
                            _1cell_innrTbl.AddElement(innrPara);
                            innerTable_Up.AddCell(_1cell_innrTbl);
                        }
                        else
                        {
                            var _1cell_innrTbl = new PdfPCell(new Phrase(" "));
                            _1cell_innrTbl.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                            _1cell_innrTbl.BorderWidth = 0;
                            innerTable_Up.AddCell(_1cell_innrTbl);
                        }

                        var _2cell_innrTbl = new PdfPCell();
                        _2cell_innrTbl.BorderWidth = 0;
                        _2cell_innrTbl.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                        int _cellCount = 13;
                        int _cellCountExtd = _cellCount + 2;
                        var ctable_2cell_innrTbl = new PdfPTable(_cellCountExtd);
                        float remainWidth = 1050 - (40 + 150);
                        width = new float[_cellCountExtd];
                        width[0] = 40f;
                        width[1] = 150f;
                        float colwidth = remainWidth / _cellCount;
                        for (int x = 2; x < _cellCountExtd; x++)
                        {
                            width[x] = colwidth;
                        }
                        ctable_2cell_innrTbl.SetWidths(width);
                        if (FitName != Model[i].ClothesScale[k].FitName)
                        {
                            var innrCell1 = new PdfPCell(new Phrase(" "));
                            innrCell1.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell1);

                            innrCell1 = new PdfPCell(new Phrase(" "));
                            innrCell1.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell1);

                            var innrPara1 = new Paragraph("Available open size", font);
                            innrPara1.Alignment = Element.ALIGN_LEFT;
                            innrCell1 = new PdfPCell();
                            innrCell1.BorderWidth = 0;
                            innrCell1.Colspan = width.Count() - 2;
                            innrCell1.AddElement(innrPara1);
                            ctable_2cell_innrTbl.AddCell(innrCell1);

                            var innrCell2 = new PdfPCell(new Phrase(" "));
                            innrCell2.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell2);
                            innrCell2 = new PdfPCell(new Phrase(" "));
                            innrCell2.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell2);
                            int _counthdr = 0;
                            for (var j = 0; j < Model[i].ClothesScale[k].ClothesScaleSizeClass.Count(); j++)
                            {
                                _counthdr++;
                                innrCell2 = new PdfPCell();


                                innrCell2.BorderWidth = 0;
                                var innrPara2 = new Paragraph(Model[i].ClothesScale[k].ClothesScaleSizeClass[j].SizeName != null ? Model[i].ClothesScale[k].ClothesScaleSizeClass[j].SizeName : " ", font);
                                innrPara2.Alignment = Element.ALIGN_CENTER;
                                innrCell2.AddElement(innrPara2);
                                innrCell2.PaddingBottom = 5;
                                ctable_2cell_innrTbl.AddCell(innrCell2);
                            }
                            var _spacehdr = 13 - _counthdr;
                            if (_spacehdr > 0)
                            {
                                for (int d = 0; d < _spacehdr; d++)
                                {
                                    innrCell2 = new PdfPCell(new Phrase(" "));
                                    innrCell2.BorderWidth = 0;
                                    ctable_2cell_innrTbl.AddCell(innrCell2);
                                }
                            }

                        }
                        var innrCell3 = new PdfPCell(new Phrase(" "));
                        innrCell3.BorderWidth = 0;
                        ctable_2cell_innrTbl.AddCell(innrCell3);
                        var inseamPara = new Paragraph(Model[i].ClothesScale[k].InseamName != null ? Model[i].ClothesScale[k].InseamName : " ", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                        inseamPara.Alignment = Element.ALIGN_CENTER;
                        innrCell3 = new PdfPCell();
                        innrCell3.AddElement(inseamPara);
                        innrCell3.BorderWidth = 0;
                        ctable_2cell_innrTbl.AddCell(innrCell3);
                        int _countloop = 0;
                        for (var j = 0; j < Model[i].ClothesScale[k].ClothesScaleSizeClass.Count(); j++)
                        {
                            _countloop++;
                            var qty = Model[i].ClothesScale[k].ClothesScaleSizeClass[j].Quantity.ToString();
                            var innrPara3 = new Paragraph();
                            if (SiteConfiguration.Mode == "View" && qty == "0")
                                innrPara3 = new Paragraph("X", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK));
                            else
                                innrPara3 = new Paragraph(qty, FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                            innrPara3.Alignment = Element.ALIGN_CENTER;
                            innrCell3 = new PdfPCell();

                            innrCell3.PaddingBottom = 6f;
                            innrCell3.AddElement(innrPara3);
                            innrCell3.BorderWidth = 0.5f;
                            ctable_2cell_innrTbl.AddCell(innrCell3);
                        }
                        var _spaceCount = 13 - _countloop;
                        if (_spaceCount > 0)
                        {
                            for (int d = 0; d < _spaceCount; d++)
                            {
                                innrCell3 = new PdfPCell(new Phrase(" "));
                                innrCell3.BorderWidth = 0;
                                ctable_2cell_innrTbl.AddCell(innrCell3);
                            }
                        }

                        _2cell_innrTbl.AddElement(ctable_2cell_innrTbl);
                        innerTable_Up.AddCell(_2cell_innrTbl);
                    }
                    else
                    {
                        if (FitName != Model[i].ClothesScale[k].FitName)
                        {
                            colorIndex = 0;
                            var innrPara = new Paragraph(Model[i].ClothesScale[k].FitName != null ? Model[i].ClothesScale[k].FitName : string.Empty, font1);
                            var _1cell_innrTbl = new PdfPCell();
                            _1cell_innrTbl.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#E9E9E9"));
                            _1cell_innrTbl.PaddingTop = 5;
                            _1cell_innrTbl.PaddingLeft = 2;
                            _1cell_innrTbl.BorderWidth = 0;
                            _1cell_innrTbl.AddElement(innrPara);
                            innerTable_Up.AddCell(_1cell_innrTbl);
                        }
                        else
                        {
                            var _1cell_innrTbl = new PdfPCell(new Phrase(" "));
                            _1cell_innrTbl.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#E9E9E9"));
                            _1cell_innrTbl.PaddingTop = 5;
                            _1cell_innrTbl.BorderWidth = 0;
                            innerTable_Up.AddCell(_1cell_innrTbl);
                        }
                        var _2cell_innrTbl = new PdfPCell();
                        _2cell_innrTbl.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#E9E9E9"));
                        _2cell_innrTbl.PaddingTop = 5;
                        _2cell_innrTbl.BorderWidth = 0;
                        int _cellCount = 13;
                        int _cellCountExtd = _cellCount + 2;
                        var ctable_2cell_innrTbl = new PdfPTable(_cellCountExtd);
                        float remainWidth = 1050 - (40 + 150);
                        width = new float[_cellCountExtd];
                        width[0] = 40f;
                        width[1] = 150f;
                        float colwidth = remainWidth / _cellCount;
                        for (int x = 2; x < _cellCountExtd; x++)
                        {
                            width[x] = colwidth;
                        }
                        ctable_2cell_innrTbl.SetWidths(width);
                        if (FitName != Model[i].ClothesScale[k].FitName)
                        {
                            var innrCell1 = new PdfPCell(new Phrase(" "));
                            innrCell1.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell1);

                            innrCell1 = new PdfPCell(new Phrase(" "));
                            innrCell1.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell1);

                            var innrPara1 = new Paragraph("Scale", font);
                            innrPara1.Alignment = Element.ALIGN_LEFT;
                            innrCell1 = new PdfPCell();
                            innrCell1.BorderWidth = 0;
                            innrCell1.Colspan = width.Count() - 2;
                            innrCell1.AddElement(innrPara1);
                            ctable_2cell_innrTbl.AddCell(innrCell1);

                            var innrCell2 = new PdfPCell(new Phrase(" "));
                            innrCell2.BorderWidth = 0;
                            ctable_2cell_innrTbl.AddCell(innrCell2);
                            if (Model[i].InseamList.Any())
                            {
                                var _Hinseam = new Paragraph("Inseam", font);
                                _Hinseam.Alignment = Element.ALIGN_CENTER;
                                innrCell2 = new PdfPCell();
                                innrCell2.AddElement(_Hinseam);
                                innrCell2.BorderWidth = 0;
                                innrCell2.PaddingBottom = 5;
                                ctable_2cell_innrTbl.AddCell(innrCell2);

                            }
                            else
                            {
                                innrCell2 = new PdfPCell(new Phrase(" "));
                                innrCell2.BorderWidth = 0;
                                ctable_2cell_innrTbl.AddCell(innrCell2);
                            }

                            int hdrCount = 0;
                            for (var j = 0; j < Model[i].ClothesScale[k].ClothesScaleSizeClass.Count(); j++)
                            {
                                hdrCount++;
                                innrCell2 = new PdfPCell();
                                innrCell2.BorderWidth = 0;
                                var innrPara2 = new Paragraph(Model[i].ClothesScale[k].ClothesScaleSizeClass[j].SizeName != null ? Model[i].ClothesScale[k].ClothesScaleSizeClass[j].SizeName : " ", font);
                                innrPara2.Alignment = Element.ALIGN_CENTER;
                                innrCell2.AddElement(innrPara2);
                                innrCell2.PaddingBottom = 5;
                                ctable_2cell_innrTbl.AddCell(innrCell2);
                            }
                            var qtyPara = new Paragraph("Avl Qty", font);
                            qtyPara.Alignment = Element.ALIGN_LEFT;
                            innrCell2 = new PdfPCell();
                            innrCell2.Colspan = 2;
                            innrCell2.AddElement(qtyPara);
                            innrCell2.BorderWidth = 0;
                            innrCell2.PaddingBottom = 5;
                            ctable_2cell_innrTbl.AddCell(innrCell2);

                            var hdrSpace = 13 - (hdrCount + 2);
                            if (hdrSpace > 0)
                            {
                                for (int w = 0; w < hdrSpace; w++)
                                {
                                    innrCell2 = new PdfPCell(new Phrase(" "));
                                    innrCell2.PaddingBottom = 6f;
                                    innrCell2.BorderWidth = 0;
                                    ctable_2cell_innrTbl.AddCell(innrCell2);
                                }
                            }
                        }

                        var namePara = new Paragraph(Model[i].ClothesScale[k].Name != null ? Model[i].ClothesScale[k].Name : " ", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                        var innrCell3 = new PdfPCell();
                        innrCell3.AddElement(namePara);
                        innrCell3.BorderWidth = 0;
                        ctable_2cell_innrTbl.AddCell(innrCell3);
                        var inseamPara = new Paragraph(Model[i].ClothesScale[k].InseamName != null ? Model[i].ClothesScale[k].InseamName : " ", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                        inseamPara.Alignment = Element.ALIGN_CENTER;
                        innrCell3 = new PdfPCell();
                        innrCell3.AddElement(inseamPara);
                        innrCell3.BorderWidth = 0;
                        ctable_2cell_innrTbl.AddCell(innrCell3);
                        int _countspace = 0;
                        for (var j = 0; j < Model[i].ClothesScale[k].ClothesScaleSizeClass.Count(); j++)
                        {
                            _countspace++;
                            var qty = Model[i].ClothesScale[k].ClothesScaleSizeClass[j].Quantity.ToString();
                            var innrPara3 = new Paragraph();
                            if (SiteConfiguration.Mode == "View" && qty == "0")
                                innrPara3 = new Paragraph("X", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK));
                            else
                                innrPara3 = new Paragraph(qty, FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));

                            innrPara3.Alignment = Element.ALIGN_CENTER;
                            innrCell3 = new PdfPCell();
                            innrCell3.PaddingBottom = 6f;
                            innrCell3.AddElement(innrPara3);
                            innrCell3.BorderWidth = 0.5f;
                            ctable_2cell_innrTbl.AddCell(innrCell3);
                        }
                        var _qtyPara = new Paragraph(Model[i].ClothesScale[k].InvQty.Value.ToString(), FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                        _qtyPara.Alignment = Element.ALIGN_CENTER;
                        innrCell3 = new PdfPCell();
                        innrCell3.PaddingBottom = 6f;
                        innrCell3.AddElement(_qtyPara);
                        innrCell3.BorderWidth = 0.5f;
                        ctable_2cell_innrTbl.AddCell(innrCell3);

                        var _spaceCount = 13 - (_countspace + 1);
                        if (_spaceCount > 0)
                        {
                            for (int w = 0; w < _spaceCount; w++)
                            {
                                innrCell3 = new PdfPCell(new Phrase(" "));
                                innrCell3.PaddingBottom = 6f;
                                innrCell3.BorderWidth = 0;
                                ctable_2cell_innrTbl.AddCell(innrCell3);
                            }
                        }

                        _2cell_innrTbl.AddElement(ctable_2cell_innrTbl);
                        innerTable_Up.AddCell(_2cell_innrTbl);
                    }

                    colorIndex++;
                    FitName = Model[i].ClothesScale[k].FitName;
                    checkIndex = Model[i].ClothesScale[k].IsOpenSize.Value;
                }

                var cell_cTable_MC3 = new PdfPCell(innerTable_Up);
                cell_cTable_MC3.Padding = 0f;
                cell_cTable_MC3.BorderWidth = 0;
                if (!_In)
                {
                    var _emptyCell = new PdfPCell(new Phrase(" "));
                    _emptyCell.BorderWidth = 0;
                    _emptyCell.Colspan = 2;
                    innerTable_Up.AddCell(_emptyCell);
                    cell_cTable_MC3.AddElement(innerTable_Up);
                }

                cTable_MC3.AddCell(cell_cTable_MC3);
                masterCell = new PdfPCell(cTable_MC3);
                masterCell.BorderWidth = 0;
                masterCell.Padding = 0f;
                masterCell.PaddingRight = 5f;
                masterCell.PaddingBottom = 5f;
                masterCell.PaddingLeft = 5f;
                masterCell.PaddingTop = 5f;
                masterCell.BorderWidth = 0.5f;
                masterCell.BorderWidthLeft = 0;
                masterTable.AddCell(masterCell);
                document.Add(masterTable);
                document.Add(new Paragraph("\n"));
            }
            if (!isFootHead)
            {
                PdfContentByte cb = null;
                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED); ;
                PdfPTable pdfTab = new PdfPTable(1);
                String ftext = "Ghacham Inc. 7340 Alondra Blvd. Paramount, CA 90723 -- 562 602 0400 -- sales@platinijeans.com";
                cb = writer.DirectContent;
                PdfTemplate footerTemplate = cb.CreateTemplate(100, 60);
                cb.BeginText();
                cb.SetFontAndSize(bf, 10);
                cb.SetTextMatrix(document.PageSize.GetRight(650), document.PageSize.GetBottom(20));
                cb.ShowText(ftext);

                cb.EndText();
                float len = bf.GetWidthPoint(ftext, 8);
                cb.AddTemplate(footerTemplate, document.PageSize.GetRight(650) + len, document.PageSize.GetBottom(20));
                pdfTab.TotalWidth = document.PageSize.Width - 80f;
                pdfTab.WidthPercentage = 70;
                //call WriteSelectedRows of PdfTable. This writes rows from PdfWriter in PdfTable
                //first param is start row. -1 indicates there is no end row and all the rows to be included to write
                //Third and fourth param is x and y position to start writing
                pdfTab.WriteSelectedRows(0, -1, 30, document.PageSize.Height - 20, writer.DirectContent);
                document.Add(pdfTab);
            }
            return document;
        }

        public static Document OrderMode_Pdf(Document document, PdfWriter writer, IPagedList<LineSheetViewClass> Model, bool isFootHead)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var font1 = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLUE);
            var cell = new PdfPCell();
            var text = new Chunk("", font);
            var remainingPageSpace = 0f;

            var logoTable = new PdfPTable(2);
            var logoPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/images"), "logo_header.png");
            FileInfo lgfi = new FileInfo(logoPath);
            if (lgfi.Exists)
            {
                var _logo = iTextSharp.text.Image.GetInstance(logoPath);
                System.Drawing.Bitmap img = new System.Drawing.Bitmap(lgfi.FullName);
                _logo.ScaleAbsolute(70, 12);
                var logocell = new PdfPCell(_logo);
                // logocell.HorizontalAlignment = Element.ALIGN_CENTER;
                logocell.Padding = 0;
                logocell.PaddingTop = -25f;
                logocell.PaddingBottom = 10f;
                logocell.BorderWidth = 0;
                logocell.BorderWidthRight = 0;
                logoTable.AddCell(logocell);
            }
            else
            {
                cell = new PdfPCell(new Phrase(" "));
                cell.Padding = 0;
                cell.PaddingTop = -25f;
                cell.PaddingBottom = 10f;
                cell.BorderWidth = 0;
                logoTable.AddCell(cell);
            }
            if (!isFootHead)
            {
                var urlpara = new Paragraph("www.platinijeans.com", FontFactory.GetFont(FontFactory.HELVETICA, 12, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml("#CC3300"))));
                cell = new PdfPCell(urlpara);
                cell.Padding = 0;
                cell.PaddingTop = -25f;
                cell.PaddingBottom = 10f;
                cell.BorderWidth = 0;
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                logoTable.AddCell(cell);

            }
            else
            {
                cell = new PdfPCell(new Phrase(" "));
                cell.Padding = 0;
                cell.PaddingTop = -25f;
                cell.PaddingBottom = 10f;
                cell.BorderWidth = 0;
                logoTable.AddCell(cell);
            }
            document.Add(logoTable);
            for (int i = 0; i < Model.Count; ++i)
            {
                //--switch to New Page-----------------
                float contentSpace = 0;
                float imgSpace = 0;
                if (Model[i] != null)
                {
                    int opensizeCount = Model[i].ClothesScale.Where(x => x.IsOpenSize == true).ToList().Count;
                    int prepackCount = 0;
                    for (int k = 0; k < Model[i].ClothesScale.Count(); k++)
                    {
                        if ((Model[i].ClothesScale[k].IsOpenSize == false) && (Model[i].ClothesScale[k].InvQty.HasValue ? Model[i].ClothesScale[k].InvQty.Value > 0 : false) && !string.IsNullOrEmpty(Model[i].ClothesScale[k].Name) && (Model[i].ClothesScale[k].ClothesScaleSizeClass.Sum(x => x.Quantity) > 0))
                        {
                            prepackCount = prepackCount + 1;
                        }
                    }
                    contentSpace = 50 + 50 + (opensizeCount * 30) + 50 + (prepackCount * 30) + 30;
                }

                imgSpace = 180;
                float finalReq = contentSpace > imgSpace ? contentSpace : imgSpace;
                remainingPageSpace = writer.GetVerticalPosition(false) - document.BottomMargin;
                //remainingPageSpace = writer.GetVerticalPosition(false);
                if (remainingPageSpace < finalReq)
                    document.NewPage();


                var masterTable = new PdfPTable(2);
                float[] width = new float[] { 250f, 1150f };
                masterTable.SetWidths(width);
                var masterCell = new PdfPCell();
                masterCell.Colspan = 2;
                masterCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                masterCell.PaddingTop = 5;
                masterCell.PaddingBottom = 5;

                var table1 = new PdfPTable(!isFootHead ? 10 : 8);
                if (!isFootHead)
                    width = new float[] { 50f, 340f, 100f, 140f, 110f, 140f, 100f, 160f, 90f, 160f };
                else
                    width = new float[] { 50f, 340f, 110f, 140f, 100f, 160f, 90f, 160f };
                table1.SetWidths(width);
                var para = new Paragraph("Style", font);

                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BorderWidth = 0;

                cell.PaddingLeft = -10;
                table1.AddCell(cell);
                para = new Paragraph(Model[i].StyleNumber, font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BackgroundColor = BaseColor.WHITE;
                cell.PaddingBottom = 5;
                cell.BorderWidth = 0.5f;
                table1.AddCell(cell);

                if (!isFootHead)
                {
                    para = new Paragraph(" Price $", font);
                    cell = new PdfPCell();
                    cell.AddElement(para);
                    cell.BorderWidth = 0;
                    cell.PaddingLeft = 2;
                    table1.AddCell(cell);

                    para = new Paragraph(string.Format("{0:0.00}", Model[i].Price), font);
                    para.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.AddElement(para);
                    cell.BackgroundColor = BaseColor.WHITE;
                    cell.BorderWidth = 0.5f;
                    cell.PaddingBottom = 5;
                    table1.AddCell(cell);
                }
                para = new Paragraph(" MSRP $", font);
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BorderWidth = 0;
                cell.PaddingLeft = 2;
                table1.AddCell(cell);

                para = new Paragraph(string.Format("{0:0.00}", Model[i].MSRP), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderWidth = 0.5f;
                cell.PaddingBottom = 5;
                table1.AddCell(cell);

                //para = new Paragraph(" Cost $", font);
                //cell = new PdfPCell();
                //cell.AddElement(para);
                //cell.BorderWidth = 0;
                //cell.PaddingLeft = 2;
                //table1.AddCell(cell);

                //para = new Paragraph(String.Format("{0:0.00}", Model[i].Cost), font);
                //para.Alignment = Element.ALIGN_CENTER;
                //cell = new PdfPCell();
                //cell.AddElement(para);
                //cell.BackgroundColor = BaseColor.WHITE;
                //cell.BorderWidth = 0.5f;
                //cell.PaddingBottom = 5;
                //table1.AddCell(cell);

                para = new Paragraph(" Color", font);
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BorderWidth = 0;
                cell.PaddingLeft = 2;
                table1.AddCell(cell);

                para = new Paragraph(Model[i].Color, font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(para);
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderWidth = 0.5f;
                cell.PaddingBottom = 5;
                table1.AddCell(cell);

                cell = new PdfPCell(new Phrase(" "));
                cell.BorderWidth = 0;
                table1.AddCell(cell);

                cell = new PdfPCell(new Phrase(" "));
                cell.BorderWidth = 0;
                table1.AddCell(cell);

                masterCell.AddElement(table1);
                // masterCell = new PdfPCell(table1);              
                masterTable.AddCell(masterCell);


                var _Largeimg = new PdfPTable(5);
                width = new float[] { 50f, 50f, 50f, 50f, 50f };
                _Largeimg.SetWidths(width);
                string imagePath = "";
                iTextSharp.text.Image image = null;
                if (Model[i].Images.Any())
                {
                    imagePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/WebThumb/"), Model[i].Images[0].ImagePath);
                    FileInfo fi = new FileInfo(imagePath);
                    if (fi.Exists)
                    {
                        image = iTextSharp.text.Image.GetInstance(imagePath);
                        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                        image.ScaleAbsolute(112, 130);
                        var _cellfor = new PdfPCell(image);
                        _cellfor.Colspan = 5;
                        _cellfor.BorderWidth = 0;
                        _Largeimg.AddCell(_cellfor);

                    }
                    else
                    {
                        var _cellfor = new PdfPCell(new Phrase(" "));
                        _cellfor.Colspan = 5;
                        _cellfor.BorderWidth = 0;
                        _Largeimg.AddCell(_cellfor);
                    }
                    //int l = 0;
                    //for (int s = 1; s < Model[i].Images.Count; s++)
                    //{
                    //    l = l + 1;
                    //    imagePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/"), Model[i].Images[s].ImagePath);
                    //    fi = new FileInfo(imagePath);
                    //    if (fi.Exists)
                    //    {
                    //        image = iTextSharp.text.Image.GetInstance(imagePath);
                    //        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                    //        image.ScaleAbsolute(20, 25);
                    //        var _cellfor = new PdfPCell(image);
                    //        _cellfor.Padding = 0;
                    //        _cellfor.BorderWidth = 0;
                    //        _cellfor.PaddingTop = 2;
                    //        _cellfor.PaddingLeft = 2;
                    //        _cellfor.PaddingRight = 3;
                    //        _Largeimg.AddCell(_cellfor);
                    //    }
                    //    else
                    //    {
                    //        var _cellfor = new PdfPCell(new Phrase(" "));
                    //        _cellfor.Padding = 0;
                    //        _cellfor.BorderWidth = 0;
                    //        _cellfor.PaddingTop = 2;
                    //        _cellfor.PaddingLeft = 2;
                    //        _cellfor.PaddingRight = 3;
                    //        _Largeimg.AddCell(_cellfor);
                    //    }
                    //}

                    //var remainloop = l > 5 ? (l - 5) + 1 : l < 5 ? 5 - l : 0;
                    //if (remainloop > 0)
                    //{
                    //    for (int z = 0; z < remainloop; z++)
                    //    {
                    //        var _cellfor = new PdfPCell(new Phrase(" "));
                    //        _cellfor.Padding = 0;
                    //        _cellfor.BorderWidth = 0;
                    //        _cellfor.PaddingTop = 2;
                    //        _Largeimg.AddCell(_cellfor);
                    //    }
                    //}

                    masterCell = new PdfPCell(_Largeimg);
                    // masterCell.PaddingLeft = -50f;
                    masterCell.Padding = 0;
                    masterCell.PaddingTop = 5;
                    masterCell.PaddingRight = 2;
                    masterCell.PaddingLeft = 5;
                    masterCell.PaddingBottom = 5;
                    masterCell.BorderWidth = 0.5f;
                    masterCell.BorderWidthRight = 0;
                }
                else
                {
                    imagePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/images"), "NO_IMAGE.jpg");
                    FileInfo fi = new FileInfo(imagePath);
                    if (fi.Exists)
                    {
                        image = iTextSharp.text.Image.GetInstance(imagePath);
                        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                        //if (img.Width > 600)
                        //    image.ScaleAbsoluteWidth(600);
                        //else if (img.Height > 600)
                        //    image.ScaleAbsoluteHeight(600);
                        //else
                        image.ScaleAbsolute(112, 160);
                        var _cellfor = new PdfPCell(image);
                        _cellfor.Colspan = 5;
                        _cellfor.Padding = 0;
                        _cellfor.BorderWidth = 0;
                        _cellfor.BorderWidthRight = 0;
                        _Largeimg.AddCell(_cellfor);
                        masterCell = new PdfPCell(_Largeimg);
                        // masterCell.PaddingLeft = -50f;
                        masterCell.Padding = 0;
                        masterCell.PaddingTop = 5;
                        masterCell.PaddingRight = 2;
                        masterCell.PaddingLeft = 5;
                        masterCell.PaddingBottom = 5;
                        masterCell.BorderWidth = 0.5f;
                        masterCell.BorderWidthRight = 0;

                    }
                }
                masterTable.AddCell(masterCell);

                bool showDdl = Model[i].FitList.Any();
                int colorIndex = 0;
                var colors = new string[] { "#0000FF", "#FF0000", "#daa520" };

                var rightTable = new PdfPTable(2);
                width = new float[] { 150f, 1000f };
                rightTable.SetWidths(width);
                if (showDdl)
                {
                    para = new Paragraph("Fit : " + Model[i].FitList.FirstOrDefault().Value, font);
                    para.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell(para);
                    cell.PaddingTop = 20f;
                    cell.PaddingLeft = 5f;
                    cell.PaddingBottom = 10f;
                    cell.BorderWidth = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                }
                else
                {
                    cell = new PdfPCell(new Phrase(" "));
                    cell.BorderWidth = 0;
                    cell.PaddingBottom = 10f;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                }
                rightTable.AddCell(cell);

                //var numCol = Model[i].ClothesScale.FirstOrDefault(x => x.IsOpenSize == true).ClothesScaleSizeClass.Count() + 1;                

                int numbox = 15 + 2;
                int numCol = 1 + 17;
                var openSizeTble = new PdfPTable(numCol);
                float remainWidth = 1000 - (250);
                width = new float[numCol];
                width[0] = 250f;
                float colwidth = remainWidth / numbox;
                for (int x = 1; x <= numbox; x++)
                {
                    width[x] = colwidth;
                }
                openSizeTble.SetWidths(width);
                cell = new PdfPCell(new Phrase(" "));
                cell.BorderWidth = 0;
                openSizeTble.AddCell(cell);

                para = new Paragraph("Available Open Sizes", font);
                cell = new PdfPCell(para);
                cell.PaddingTop = 10f;
                cell.BorderWidth = 0;
                cell.Colspan = numbox;
                openSizeTble.AddCell(cell);

                cell = new PdfPCell(new Phrase(" "));
                cell.BorderWidth = 0;
                openSizeTble.AddCell(cell);


                var scaleCount = Model[i].ClothesScale.FirstOrDefault(x => x.IsOpenSize == true) != null ? Model[i].ClothesScale.FirstOrDefault(x => x.IsOpenSize == true).ClothesScaleSizeClass.Count() : 0;
                if (scaleCount > 0)
                {
                    int loopcount = 0;
                    for (int o = 0; o < Model[i].ClothesScale.FirstOrDefault(x => x.IsOpenSize == true).ClothesScaleSizeClass.Count; o++)
                    {
                        loopcount++;
                        para = new Paragraph(Model[i].ClothesScale.FirstOrDefault(x => x.IsOpenSize == true).ClothesScaleSizeClass[o].SizeName, font);
                        para.Alignment = Element.ALIGN_CENTER;
                        cell = new PdfPCell();
                        cell.AddElement(para);
                        cell.BorderWidth = 0;
                        cell.PaddingBottom = 3f;
                        openSizeTble.AddCell(cell);
                    }
                    int Space_loop = numbox - loopcount;
                    if (Space_loop > 0)
                    {
                        for (int s = 0; s < Space_loop; s++)
                        {
                            cell = new PdfPCell(new Phrase(" "));
                            cell.BorderWidth = 0;
                            openSizeTble.AddCell(cell);
                        }
                    }
                    //--Filter

                    var fist_Fitid = Model[i].FitList.Any() ? Model[i].FitList.FirstOrDefault().Id : 0;
                    var list = Model[i].ClothesScale.ToList();
                    if (fist_Fitid != 0)
                        list = list.Where(l => l.FitId == fist_Fitid).ToList();
                    for (int d = 0; d < list.Count(); d++)
                    {
                        if (list[d].IsOpenSize == true)
                        {

                            para = new Paragraph(!string.IsNullOrEmpty(list[d].InseamName) ? list[d].InseamName : " ", font);
                            para.Alignment = Element.ALIGN_CENTER;
                            cell = new PdfPCell();
                            cell.AddElement(para);
                            cell.PaddingBottom = 5f;
                            cell.BorderWidth = 0;
                            openSizeTble.AddCell(cell);

                            int lcount = 0;
                            for (int o = 0; o < list[d].ClothesScaleSizeClass.Count; o++)
                            {
                                lcount++;
                                if (list[d].ClothesScaleSizeClass[o].Quantity > 0)
                                {
                                    cell = new PdfPCell(new Phrase(" "));
                                    cell.BorderWidth = 0.5f;
                                    cell.PaddingBottom = 5f;
                                    cell.BackgroundColor = BaseColor.WHITE;
                                    openSizeTble.AddCell(cell);
                                }
                                else
                                {
                                    para = new Paragraph("X", font);
                                    cell.BorderWidth = 0.5f;
                                    cell.PaddingBottom = 5f;
                                    para.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.AddElement(para);
                                    openSizeTble.AddCell(cell);
                                }
                            }
                            Space_loop = 17 - (lcount);
                            if (Space_loop > 0)
                            {
                                for (int s = 0; s < Space_loop; s++)
                                {
                                    cell = new PdfPCell(new Phrase(" "));
                                    cell.BorderWidth = 0;
                                    openSizeTble.AddCell(cell);
                                }
                            }
                        }
                    }

                    cell = new PdfPCell(openSizeTble);
                    cell.BorderWidth = 0;
                    cell.PaddingBottom = 10f;
                    //cell.PaddingRight = -20f;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                    rightTable.AddCell(cell);

                    cell = new PdfPCell(new Phrase(" "));
                    cell.BorderWidth = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                    rightTable.AddCell(cell);


                    if (Model[i].ClothesScale.Any(x => (x.IsOpenSize == false) && (!string.IsNullOrEmpty(x.Name)) && (x.InvQty.HasValue ? x.InvQty.Value > 0 : false) && (x.ClothesScaleSizeClass.Sum(y => y.Quantity) > 0)))
                    {
                        numbox = 15 + 2;
                        numCol = 3 + 17 + 1;
                        var prepackTable = new PdfPTable(numCol);
                        remainWidth = 1000 - (70 + 110 + 40 + 30);
                        width = new float[numCol];
                        width[0] = 70f;
                        width[1] = 110f;
                        width[2] = 40f;
                        width[numCol - 1] = 30f;
                        colwidth = remainWidth / numbox;
                        for (int x = 3; x <= numbox; x++)
                        {
                            width[x] = colwidth;
                        }
                        prepackTable.SetWidths(width);
                        cell = new PdfPCell(new Phrase(" "));
                        cell.Colspan = 3;
                        cell.BorderWidth = 0;
                        prepackTable.AddCell(cell);

                        //var queiconPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/image"), "questionmark.png");
                        //FileInfo icn = new FileInfo(queiconPath);
                        //var _ques = iTextSharp.text.Image.GetInstance(queiconPath);
                        //if (icn.Exists)
                        //{                           
                        //    System.Drawing.Bitmap img = new System.Drawing.Bitmap(icn.FullName);
                        //    _ques.ScaleAbsolute(10, 10);
                        //}
                        var colcount = Model[i].ClothesScale.FirstOrDefault(x => x.IsOpenSize == false && !string.IsNullOrEmpty(x.Name) && (x.InvQty.HasValue ? x.InvQty.Value > 0 : false) && x.ClothesScaleSizeClass.Sum(y => y.Quantity) > 0).ClothesScaleSizeClass.Count + 1;
                        para = new Paragraph("Available Pre-Packs", font);
                        cell = new PdfPCell(para);
                        cell.Colspan = colcount > 0 ? colcount : 15;
                        cell.BorderWidth = 0;
                        cell.PaddingTop = 10f;
                        prepackTable.AddCell(cell);

                        para = new Paragraph("Order Pre-Pack", font);
                        cell = new PdfPCell(para);
                        cell.Colspan = 3;
                        cell.BorderWidth = 0;
                        cell.PaddingTop = 10f;
                        cell.PaddingLeft = 10f;
                        prepackTable.AddCell(cell);

                        // Space_loop = colcount < 13 ? 13 - colcount : colcount - 13;

                        Space_loop = 17 - (colcount + 2);
                        if (Space_loop > 0)
                        {
                            for (int s = 0; s < Space_loop; s++)
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                prepackTable.AddCell(cell);
                            }
                        }

                        cell = new PdfPCell(new Phrase(" "));
                        cell.BorderWidth = 0;
                        cell.Colspan = 3;
                        prepackTable.AddCell(cell);
                        int cols = 0;
                        for (int j = 0; j < Model[i].ClothesScale.FirstOrDefault(x => (x.IsOpenSize == false) && (!string.IsNullOrEmpty(x.Name)) && (x.InvQty.HasValue ? x.InvQty.Value > 0 : false) && (x.ClothesScaleSizeClass.Sum(y => y.Quantity) > 0)).ClothesScaleSizeClass.Count; j++)
                        {
                            cols++;
                            para = new Paragraph(Model[i].ClothesScale.FirstOrDefault(x => (x.IsOpenSize == false) && (!string.IsNullOrEmpty(x.Name)) && (x.InvQty.HasValue ? x.InvQty.Value > 0 : false) && (x.ClothesScaleSizeClass.Sum(y => y.Quantity) > 0)).ClothesScaleSizeClass[j].SizeName, font);
                            para.Alignment = Element.ALIGN_CENTER;
                            cell = new PdfPCell();
                            cell.AddElement(para);
                            cell.PaddingBottom = 3f;
                            cell.BorderWidth = 0;
                            prepackTable.AddCell(cell);
                        }

                        para = new Paragraph("Total", font);
                        para.Alignment = Element.ALIGN_CENTER;
                        cell = new PdfPCell();
                        cell.AddElement(para);
                        cell.BorderWidth = 0;
                        cell.PaddingBottom = 3f;
                        prepackTable.AddCell(cell);

                        Space_loop = 17 - (cols + 1);
                        if (Space_loop > 0)
                        {
                            for (int s = 0; s < Space_loop; s++)
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                prepackTable.AddCell(cell);
                            }
                        }

                        cell = new PdfPCell(new Phrase(" "));
                        cell.BorderWidth = 0;
                        prepackTable.AddCell(cell);

                        string FitName = "";
                        for (int k = 0; k < list.Count(); k++)
                        {
                            if ((list[k].IsOpenSize == false) && (list[k].InvQty.HasValue ? list[k].InvQty.Value > 0 : false) && !string.IsNullOrEmpty(list[k].Name) && (list[k].ClothesScaleSizeClass.Sum(x => x.Quantity) > 0))
                            {
                                if (FitName != Model[i].ClothesScale[k].FitName)
                                {
                                    colorIndex = 0;
                                }
                                para = new Paragraph("Scale " + (!string.IsNullOrEmpty(list[k].Name) ? list[k].Name : " "), FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                                cell = new PdfPCell(para);
                                cell.BorderWidth = 0;
                                cell.PaddingBottom = 5f;
                                prepackTable.AddCell(cell);

                                para = new Paragraph(!string.IsNullOrEmpty(list[k].FitName) ? list[k].FitName : " ", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                                cell = new PdfPCell(para);
                                cell.BorderWidth = 0;
                                cell.PaddingBottom = 5f;
                                prepackTable.AddCell(cell);

                                para = new Paragraph(!string.IsNullOrEmpty(list[k].InseamName) ? list[k].InseamName : " ", FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                                cell = new PdfPCell(para);
                                cell.BorderWidth = 0;
                                cell.PaddingBottom = 5f;
                                prepackTable.AddCell(cell);

                                int cols2 = 0;
                                for (int l = 0; l < list[k].ClothesScaleSizeClass.Count; l++)
                                {
                                    cols2++;
                                    if (list[k].ClothesScaleSizeClass[l].Quantity > 0)
                                    {
                                        para = new Paragraph(list[k].ClothesScaleSizeClass[l].Quantity.ToString(), FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                                        para.Alignment = Element.ALIGN_CENTER;
                                        cell = new PdfPCell();
                                        cell.AddElement(para);
                                        cell.BorderWidth = 0.5f;
                                        cell.PaddingBottom = 5f;
                                        prepackTable.AddCell(cell);
                                    }
                                    else
                                    {
                                        para = new Paragraph("X", font);
                                        para.Alignment = Element.ALIGN_CENTER;
                                        cell = new PdfPCell();
                                        cell.AddElement(para);
                                        cell.BorderWidth = 0.5f;
                                        cell.PaddingBottom = 5f;
                                        prepackTable.AddCell(cell);
                                    }
                                }

                                para = new Paragraph(list[k].ClothesScaleSizeClass.Sum(x => x.Quantity).ToString(), FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                                para.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.AddElement(para);
                                cell.BorderWidth = 0.5f;
                                cell.PaddingBottom = 5f;
                                prepackTable.AddCell(cell);

                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                prepackTable.AddCell(cell);
                                para = new Paragraph(list[k].PurchasedQty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, new BaseColor(System.Drawing.ColorTranslator.FromHtml(colors[colorIndex]))));
                                para.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BackgroundColor = BaseColor.WHITE;
                                cell.AddElement(para);
                                cell.BorderWidth = 0.5f;
                                cell.PaddingBottom = 5f;
                                prepackTable.AddCell(cell);

                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                prepackTable.AddCell(cell);



                                Space_loop = 17 - (cols2 + 1 + 2);
                                if (Space_loop > 0)
                                {
                                    for (int s = 0; s < Space_loop; s++)
                                    {
                                        cell = new PdfPCell(new Phrase(" "));
                                        cell.BorderWidth = 0;
                                        prepackTable.AddCell(cell);
                                    }
                                }
                                ++colorIndex;
                                FitName = list[k].FitName;
                            }
                        }
                        cell = new PdfPCell(prepackTable);
                        cell.PaddingBottom = 20f;
                        // cell.PaddingRight = -20f;
                        cell.BorderWidth = 0;
                        cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                        rightTable.AddCell(cell);
                    }
                    else
                    {
                        cell = new PdfPCell(new Phrase(" "));
                        cell.BorderWidth = 0;
                        cell.PaddingBottom = 20f;
                        cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                        rightTable.AddCell(cell);
                    }
                    cell = new PdfPCell(new Phrase(" "));
                    cell.BorderWidth = 0;
                    cell.PaddingBottom = 20f;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                    rightTable.AddCell(cell);


                    var TOQ = new PdfPTable(2);
                    cell = new PdfPCell(new Phrase(" "));
                    cell.BorderWidth = 0;
                    TOQ.AddCell(cell);

                    para = new Paragraph("Total Order Qty: " + "0" + "  " + "Total:" + "$ 0.00", font);
                    cell = new PdfPCell(para);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingRight = 20f;
                    cell.BorderWidth = 0;
                    TOQ.AddCell(cell);


                    para = new Paragraph("Total Order Qty: " + "0" + "  " + "Total:" + "$ 0.00", font);
                    cell = new PdfPCell(para);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingRight = 20f;
                    cell.BorderWidth = 0;
                    TOQ.AddCell(cell);

                    cell = new PdfPCell(TOQ);
                    cell.BorderWidth = 0;
                    cell.PaddingBottom = 20f;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                    rightTable.AddCell(cell);

                    cell = new PdfPCell(rightTable);
                    //cell.PaddingRight = -50f;
                    cell.BorderWidth = 0;
                    cell.BorderWidthRight = 0.5f;
                    cell.BorderWidthBottom = 0.5f;
                    masterTable.AddCell(cell);
                }
                else
                {
                    cell = new PdfPCell(new Phrase(" "));
                    cell.BorderWidth = 0;
                    cell.BorderWidthRight = 0.5f;
                    cell.BorderWidthBottom = 0.5f;
                    masterTable.AddCell(cell);
                }

                document.Add(masterTable);
                document.Add(new Paragraph("\n"));
            }
            if (!isFootHead)
            {
                PdfContentByte cb = null;
                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED); ;
                PdfPTable pdfTab = new PdfPTable(1);
                String ftext = "Ghacham Inc. 7340 Alondra Blvd. Paramount, CA 90723 -- 562 602 0400 -- sales@platinijeans.com";
                cb = writer.DirectContent;
                PdfTemplate footerTemplate = cb.CreateTemplate(100, 60);
                cb.BeginText();
                cb.SetFontAndSize(bf, 10);
                cb.SetTextMatrix(document.PageSize.GetRight(650), document.PageSize.GetBottom(20));
                cb.ShowText(ftext);

                cb.EndText();
                float len = bf.GetWidthPoint(ftext, 8);
                cb.AddTemplate(footerTemplate, document.PageSize.GetRight(650) + len, document.PageSize.GetBottom(20));
                pdfTab.TotalWidth = document.PageSize.Width - 80f;
                pdfTab.WidthPercentage = 70;
                //call WriteSelectedRows of PdfTable. This writes rows from PdfWriter in PdfTable
                //first param is start row. -1 indicates there is no end row and all the rows to be included to write
                //Third and fourth param is x and y position to start writing
                pdfTab.WriteSelectedRows(0, -1, 30, document.PageSize.Height - 20, writer.DirectContent);
                document.Add(pdfTab);
            }
            return document;
        }


        public static Document CartPdfCreater(Document document, PdfWriter writer, Cart Model, bool HidePrice, string headrTxt)
        {
            var sfont = FontFactory.GetFont(FontFactory.HELVETICA, 7, Font.NORMAL, BaseColor.BLACK);
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var font1 = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK);
            var cell = new PdfPCell();
            var text = new Chunk("", font);

            bool hP = false; bool markConfirm = false;
            hP = HidePrice || SiteIdentity.Roles == RolesEnum.Warehouse.ToString();
            Platini.DB.Entities db = new Platini.DB.Entities();

            //if (hP)
                Model.TotalQty = 0;
            //-headrTable
            var headrTable = new PdfPTable(1);
            var hrpara = new Paragraph(headrTxt, font1);
            hrpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.AddElement(hrpara);
            headrTable.AddCell(cell);
            document.Add(headrTable);
            document.Add(new Paragraph("\n"));

            //-- Ticket Setcion
            var TicketmTable = new PdfPTable(1);
            var iTable1 = new PdfPTable(2);
            float[] width = new float[] { 150f, 850f };
            iTable1.SetWidths(width);
            var logoPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/image"), "logo.png");
            FileInfo lgfi = new FileInfo(logoPath);
            if (lgfi.Exists)
            {
                var _logo = iTextSharp.text.Image.GetInstance(logoPath);
                System.Drawing.Bitmap img = new System.Drawing.Bitmap(lgfi.FullName);
                _logo.ScaleAbsolute(60, 50);
                var logocell = new PdfPCell(_logo);
                logocell.Padding = 5;
                logocell.PaddingLeft = 20;
                logocell.Rowspan = 3;
                iTable1.AddCell(logocell);
            }
            else
            {
                var logocell = new PdfPCell(new Phrase(""));
                iTable1.AddCell(logocell);
            }
            var iTable1o1 = new PdfPTable(3);
            width = new float[] { 250f, 250f, 350f };
            iTable1o1.SetWidths(width);
            cell = new PdfPCell(new Paragraph("Ghacham Inc", font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BorderWidthRight = 0;
            iTable1o1.AddCell(cell);
            cell = new PdfPCell(new Paragraph("PLATINI JEANS CO", font));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BorderWidthLeft = 0;
            cell.BorderWidthRight = 0;
            iTable1o1.AddCell(cell);
            cell = new PdfPCell(new Paragraph("PURCHASE ORDER # " + Model.OrdNum, font));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BorderWidthLeft = 0;
            iTable1o1.AddCell(cell);

            cell = new PdfPCell(iTable1o1);
            iTable1.AddCell(cell);

            Paragraph addpara = new Paragraph("7340 Alondra Blvd. Paramount. CA 90723 / 562-602-0400 / FAX: 562-684-4679 / www.platinijeans.com", font);
            addpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.Colspan = 3;
            cell.AddElement(addpara);
            iTable1.AddCell(cell);

            var iTable2o1 = new PdfPTable(2);
            width = new float[] { 250f, 600f };
            iTable2o1.SetWidths(width);
            string _date = Model.UserId > 0 ? DateTime.UtcNow.ToShortDateString() : "";
            cell = new PdfPCell(new Paragraph("Date : " + _date, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2o1.AddCell(cell);

            cell = new PdfPCell(new Paragraph("Buyer : " + Model.CartOwner.Buyer, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2o1.AddCell(cell);

            cell = new PdfPCell(iTable2o1);
            iTable1.AddCell(cell);

            //--mastercell
            cell = new PdfPCell(iTable1);
            TicketmTable.AddCell(cell);

            //secondtable for 'soldto' and next two
            var iTable2 = new PdfPTable(2);
            width = new float[] { 340f, 510f };
            iTable2.SetWidths(width);

            cell = new PdfPCell(new Paragraph("Sold To : " + Model.CartOwner.CompanyName, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data1 = Model.CartOwner.ShippingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.ShippingAddress.To) ? Model.CartOwner.ShippingAddress.To : " ";
            cell = new PdfPCell(new Paragraph("Ship To : " + data1, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data2 = Model.CartOwner.BillingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.BillingAddress.Line1) ? Model.CartOwner.BillingAddress.Line1 : " ";
            cell = new PdfPCell(new Phrase(data2, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data3 = Model.CartOwner.ShippingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.ShippingAddress.Line1) ? Model.CartOwner.ShippingAddress.Line1 : " ";
            cell = new PdfPCell(new Phrase(data3, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data4 = Model.CartOwner.BillingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.BillingAddress.Line2) ? Model.CartOwner.BillingAddress.Line2 : " ";
            cell = new PdfPCell(new Phrase(data4, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            var iTable1o2 = new PdfPTable(2);
            width = new float[] { 400f, 110f };
            string data5 = Model.CartOwner.ShippingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.ShippingAddress.Line2) ? Model.CartOwner.ShippingAddress.Line2 : " ";
            cell = new PdfPCell(new Paragraph(data5, font));
            cell.BorderWidth = 0;
            iTable1o2.AddCell(cell);

            cell = new PdfPCell(new Paragraph("ZipCode : " + (Model.CartOwner.ShippingAddress != null ? Model.CartOwner.ShippingAddress.ZipCode : " "), font));
            cell.BorderWidth = 0;
            iTable1o2.AddCell(cell);

            cell = new PdfPCell(iTable1o2);
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            cell = new PdfPCell(iTable2);
            TicketmTable.AddCell(cell);

            //last three row section

            var iTable3 = new PdfPTable(3);
            width = new float[] { 350f, 350f, 300f };
            iTable3.SetWidths(width);
            cell = new PdfPCell(new Paragraph("Tel : " + Model.CartOwner.Phone, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Fax : " + Model.CartOwner.Fax, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Email : " + Model.CartOwner.Email, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            var dt = Model.Terms.Where(x => x.Id == Model.CartOwner.TermId).FirstOrDefault();
            cell = new PdfPCell(new Paragraph("Terms : " + (dt != null ? dt.Value : " "), font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Sales Person : " + Model.CartOwner.SalesPerson, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            var dt2 = Model.Shipping.Where(x => x.Id == Model.CartOwner.ShipVia).FirstOrDefault();
            cell = new PdfPCell(new Paragraph("Ship Via : " + (dt2 != null ? dt2.Value : " "), font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            //cell = new PdfPCell(new Paragraph("New User: ", font));
            //cell.Padding = 4;
            //cell.BorderWidth = 0;
            //iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Note : " + Model.Note, font));
            cell.Padding = 4;
            cell.Colspan = 3;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);

            cell = new PdfPCell(iTable3);
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            TicketmTable.AddCell(cell);


            document.Add(TicketmTable);
            document.Add(new Paragraph("\n"));

            //-Shopping Table
            int numOfCol = hP ? 8 : 10;
            var mShoppingTable = new PdfPTable(numOfCol);
            if (!hP)
                width = new float[] { 120f, 90f, 70f, 50f, 90f, 90f, 450f, 60f, 50f, 80f };
            else
                width = new float[] { 120f, 90f, 70f, 50f, 90f, 90f, 590f, 50f };
            mShoppingTable.SetWidths(width);

            var hpara = new Paragraph(" ", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Style", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);


            hpara = new Paragraph("Delivery", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Scale", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Scales", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Inseam", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            int colnumber = 0;
            if (!hP)
            {
                colnumber = 10;
                hpara = new Paragraph("Available Open Size", font);
                //hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);

                hpara = new Paragraph("Price", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);

                hpara = new Paragraph("Qty", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);

                hpara = new Paragraph("Total", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);
            }
            else
            {
                colnumber = 8;
                hpara = new Paragraph("Available Open Size", font);
                // hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                // cell.Colspan = 3;
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);


                hpara = new Paragraph("Qty", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);


            }

            for (int i = 0; i < Model.Clothes.Count; ++i)
            {               
                if (i > 0)
                {
                    cell = new PdfPCell();
                    cell.BorderWidth = 0;
                    cell.BorderWidthBottom = 0.1f;
                    cell.PaddingTop = 10;
                    cell.PaddingBottom = 10;
                    cell.Colspan = hP ? (5 + 3) : (7 + 3);
                    mShoppingTable.AddCell(cell);
                }
                Platini.DB.Order lastOrder;
                lastOrder = db.Orders.Find(Model.OrderId);

                bool first = false; 
                for (int j = 0; j < Model.Clothes[i].Contents.Count; ++j)
                {
                    first = j == 0;
                    var iTable = new PdfPTable(1);                                     
                    
                    if (first)
                    {
                        cell = new PdfPCell(new Paragraph(Model.Clothes[i].GroupName, font));
                        cell.BorderWidth = 0;
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 5;
                        iTable.AddCell(cell);
                    }
                    // image section

                    var imgPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/WebThumb/"), Model.Clothes[i].Contents[j].Image);
                    FileInfo fi = new FileInfo(imgPath);
                    if (fi.Exists)
                    {
                        var _img = iTextSharp.text.Image.GetInstance(imgPath);
                        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                        _img.ScaleAbsolute(50, 75);
                        cell = new PdfPCell(_img);
                        cell.BorderWidth = 0; ;
                        iTable.AddCell(cell);
                    }
                    else
                    {
                        imgPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/"), "NO_IMAGE.jpg");
                        fi = new FileInfo(imgPath);
                        if (fi.Exists)
                        {
                            var _img = iTextSharp.text.Image.GetInstance(imgPath);
                            System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                            _img.ScaleAbsolute(50, 75);
                            cell = new PdfPCell(_img);
                            cell.BorderWidth = 0;
                            iTable.AddCell(cell);
                        }
                        else
                        {
                            cell = new PdfPCell(new Phrase(" "));
                            cell.BorderWidth = 0.5f;
                            iTable.AddCell(cell);
                        }
                    }


                    cell = new PdfPCell(iTable);
                    cell.BorderWidth = 0;
                    mShoppingTable.AddCell(cell);

                    // Style No

                    var ipara = new Paragraph(Model.Clothes[i].Contents[j].StyleNumber, font);
                    ipara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.BorderWidth = 0;
                    cell.PaddingTop = 15;
                    cell.AddElement(ipara);
                    mShoppingTable.AddCell(cell);

                    // Delivery
                    ipara = new Paragraph(Model.Clothes[i].Contents[j].Delivery, font);
                    ipara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.BorderWidth = 0;
                    cell.PaddingTop = 15;
                    cell.AddElement(ipara);
                    mShoppingTable.AddCell(cell);

                    //inner table for other 7 or 5 column
                    int nocc = colnumber == 10 ? 7 : 5;
                    var imTable = new PdfPTable(nocc);
                    if (colnumber == 10)
                        width = new float[] { 50f, 90f, 90f, 450f, 60f, 50f, 80f };
                    else if (colnumber == 8)
                        width = new float[] { 50f, 90f, 90f, 590f, 50f };
                    imTable.SetWidths(width);
                    for (int k = 0; k < Model.Clothes[i].Contents[j].SPs.Count; ++k)
                    {
                        bool wasFitShown = false;
                        if (Model.Clothes[i].Contents[j].SPs[k].Pack != null || Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count > 0)
                        {
                            var iTable22 = new PdfPTable(1);
                            var scaleList = lastOrder.OrderScales.Where(x => x.ClothesScale.ClothesId == Model.Clothes[i].Contents[j].ClothesId);
                            if (Model.Clothes[i].Contents[j].SPs[k].Pack != null)
                            {
                                if (Model.Clothes[i].Contents[j].SPs[k].Pack.ClothesScaleId > 0 && Model.Clothes[i].Contents[j].SPs[k].Pack.ClothesId > 0 && Model.Clothes[i].Contents[j].SPs[k].Pack.OrderSSId != Guid.Empty)
                                {
                                    ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Pack.Name, sfont);
                                    ipara.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.AddElement(ipara);
                                    cell.BorderWidth = 0;
                                    iTable22.AddCell(cell);
                                    if (hP)
                                    {                                       
                                        if (Model.Clothes[i].Contents[j].SPs[k].isConfirmed)
                                            Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].Pack.QuantSum.Value * Model.Clothes[i].Contents[j].SPs[k].Pack.PurchasedQty.Value;
                                    }
                                    else
                                    {
                                        if (Model.Clothes[i].Contents[j].SPs[k].isConfirmed)
                                            Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].Pack.QuantSum.Value * Model.Clothes[i].Contents[j].SPs[k].Pack.PurchasedQty.Value;
                                    }
                                    ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Pack.PurchasedQty.ToString(), font);
                                    ipara.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.AddElement(ipara);
                                    cell.BorderWidth = 0.5f;
                                    iTable22.AddCell(cell);

                                    cell = new PdfPCell(iTable22);
                                    cell.BorderWidth = 0;
                                    imTable.AddCell(cell);
                                }
                                else
                                {
                                    cell = new PdfPCell(new Phrase(" "));
                                    cell.BorderWidth = 0;
                                    imTable.AddCell(cell);
                                }
                            }
                            else
                            {

                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                imTable.AddCell(cell);
                            }

                            if (!string.IsNullOrEmpty(Model.Clothes[i].Contents[j].SPs[k].Fit))
                            {
                                wasFitShown = true;
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Fit, font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            else
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                imTable.AddCell(cell);
                            }

                            if (!string.IsNullOrEmpty(Model.Clothes[i].Contents[j].SPs[k].Inseam))
                            {
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Inseam, font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            else
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                imTable.AddCell(cell);
                            }

                            float sizee = hP ? 590f : 450f;
                            int noc = Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count < 12 ? 12 : Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count;
                            float boxsize = sizee / noc;
                            var iTable33 = new PdfPTable(noc);
                            width = new float[noc];
                            for (int n = 0; n < noc; n++)
                            {
                                width[n] = boxsize;
                            }

                            for (int l = 0; l < Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count; l++)
                            {

                                var iiTable = new PdfPTable(1);
                                // float ff = kk + 1;
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].SizeName, sfont);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.AddElement(ipara);
                                iiTable.AddCell(cell);

                                string pval = "X";
                                if ((Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].Quantity.HasValue ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].Quantity.Value > 0 : false) || (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.HasValue ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value > 0 : false))
                                {
                                    if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.HasValue)
                                    {
                                        if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value < 0)
                                        {                                            
                                            pval = Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.ToString();                                          
                                            ipara = new Paragraph(pval, font);
                                            ipara.Alignment = Element.ALIGN_CENTER;
                                            cell = new PdfPCell();
                                            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#ff4c4c"));
                                            cell.PaddingBottom = 5;
                                            cell.BorderWidth = 0.5f;
                                            cell.AddElement(ipara);
                                        }
                                        else
                                        {
                                            pval = Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.ToString();
                                            if (hP)
                                            {
                                                if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].isConfirm)
                                                {
                                                    markConfirm = true;
                                                    Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity != null ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value : 0;
                                                }
                                            }
                                            else
                                            {
                                                if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].isConfirm)
                                                {
                                                    markConfirm=true;
                                                    Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity != null ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value : 0;
                                                }
                                            }
                                           
                                            ipara = new Paragraph(pval, font);
                                            ipara.Alignment = Element.ALIGN_CENTER;
                                            cell = new PdfPCell();
                                            cell.PaddingBottom = 5;
                                            cell.BorderWidth = 0.5f;
                                            cell.AddElement(ipara);
                                        }
                                    }
                                    else
                                    {
                                        ipara = new Paragraph(pval, font);
                                        ipara.Alignment = Element.ALIGN_CENTER;
                                        cell = new PdfPCell();
                                        cell.PaddingBottom = 5;
                                        cell.BorderWidth = 0.5f;
                                        cell.AddElement(ipara);
                                    }
                                }
                                else
                                {
                                    ipara = new Paragraph(pval, font);
                                    ipara.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.PaddingBottom = 5;
                                    cell.BorderWidth = 0.5f;
                                    cell.AddElement(ipara);
                                }
                                iiTable.AddCell(cell);

                                cell = new PdfPCell(iiTable);
                                cell.BorderWidth = 0;
                                iTable33.AddCell(cell);

                            }
                            int spacebox = 12 - Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count;
                            if (spacebox > 0)
                            {
                                for (int s = 0; s < spacebox; s++)
                                {
                                    cell = new PdfPCell(new Phrase(" "));
                                    cell.BorderWidth = 0;
                                    iTable33.AddCell(cell);
                                }
                            }

                            cell = new PdfPCell(iTable33);
                            cell.BorderWidth = 0;
                            imTable.AddCell(cell);

                            if (!hP)
                            {                                
                                ipara = new Paragraph("$" + Model.Clothes[i].Contents[j].Price.ToString("F2"), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }

                            //yellow Mark
                            if (hP && (Model.Clothes[i].Contents[j].SPs[k].isConfirmed || markConfirm))
                            {
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Quantity.ToString(), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFF00"));
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            else
                            {
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Quantity.ToString(), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                if (Model.Clothes[i].Contents[j].SPs[k].isConfirmed || markConfirm)
                                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFF00"));
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            markConfirm = false;
                            if (!hP)
                            {
                                ipara = new Paragraph("$" + Model.Clothes[i].Contents[j].SPs[k].Total.ToString("F2"), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }

                            cell = new PdfPCell(new Phrase(" "));
                            cell.BorderWidth = 0;
                            cell.Colspan = hP ? 5 : 7;
                            imTable.AddCell(cell);

                        }
                    }
                    cell = new PdfPCell(imTable);
                    cell.Colspan = hP ? 5 : 7;
                    cell.BorderWidth = 0;
                    cell.PaddingTop = 5;
                    mShoppingTable.AddCell(cell);

                    cell = new PdfPCell(new Phrase(" "));
                    cell.Colspan = hP ? (5 + 3) : (7 + 3);
                    cell.BorderWidth = 0;
                    mShoppingTable.AddCell(cell);                                 
                }
            }

            document.Add(mShoppingTable);
            document.Add(new Paragraph("\n"));

            //Total Table
            var mtotalTable = new PdfPTable(2);
            width = new float[] { 20f, 980f };
            mtotalTable.SetWidths(width);

            cell = new PdfPCell(new Phrase(" "));
            cell.BorderWidth = 0.5f;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#000000"));
            mtotalTable.AddCell(cell);

            var itabel = new PdfPTable(5);
            width = new float[] { 175f, 175f, 280f,170f, 180f };
            itabel.SetWidths(width);

            if (hP)
            {
                Paragraph tpara = new Paragraph("Total Pcs. Packed: " + Model.TotalQty.ToString(), font);
                tpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.Padding = 5;
                cell.Colspan = 5;
                cell.BorderWidthRight = 0;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                cell.AddElement(tpara);
                itabel.AddCell(cell);
            }
            else
            {
                Paragraph tpara = new Paragraph("Total Pcs. Packed: " + Model.TotalQty.ToString(), font);
                tpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.Padding = 5;
                cell.BorderWidthRight = 0;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                cell.AddElement(tpara);
                itabel.AddCell(cell);

                #region OldCodeComment
                tpara = new Paragraph("Grand Total: " + "$" + Model.GrandTotal.ToString("F2"), font);
                tpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.Padding = 5;
                cell.BorderWidthLeft = 0;
                cell.BorderWidthRight = 0;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                cell.AddElement(tpara);
                itabel.AddCell(cell);

                if (Model.Discount > 0)
                {
                    tpara = new Paragraph("Disc (%): " + Model.Discount + " Disc Amt: $" + (Model.GrandTotal * (Model.Discount / 100)).ToString("f"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.BorderWidthLeft = 0;
                    cell.BorderWidthRight = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);

                    tpara = new Paragraph("Shipping Cost: " + "$" + Model.ShippingAmount.ToString("F2"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.BorderWidthLeft = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);

                    tpara = new Paragraph("Final Amount: " + "$" + Model.FinalAmount.ToString("F2"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.BorderWidthLeft = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);

                    
                }
                else
                {
                    tpara = new Paragraph("Shipping Cost: " + "$" + Model.ShippingAmount.ToString("F2"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.Colspan = 2;
                    cell.BorderWidthLeft = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);

                    tpara = new Paragraph("Final Amount: " + "$" + Model.FinalAmount.ToString("F2"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    //cell.Colspan = 2;
                    cell.BorderWidthLeft = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);
                }
                #endregion
            }
           

            cell = new PdfPCell();
            cell.AddElement(itabel);
            cell.BorderWidth = 0.5f;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#000000"));
            mtotalTable.AddCell(cell);
            document.Add(mtotalTable);
            return document;
        }
        public static Document CartPdfCreaterWithoutImage(Document document, PdfWriter writer, Cart Model, bool HidePrice, string headrTxt, bool HideImage)
        {
            var sfont = FontFactory.GetFont(FontFactory.HELVETICA, 7, Font.NORMAL, BaseColor.BLACK);
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var font1 = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK);
            var cell = new PdfPCell();
            var text = new Chunk("", font);

            bool hP = false; bool markConfirm = false;
            hP = HidePrice || SiteIdentity.Roles == RolesEnum.Warehouse.ToString();
            Platini.DB.Entities db = new Platini.DB.Entities();

            //if (hP)
                Model.TotalQty = 0;
            //-headrTable
            var headrTable = new PdfPTable(1);
            var hrpara = new Paragraph(headrTxt, font1);
            hrpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.AddElement(hrpara);
            headrTable.AddCell(cell);
            document.Add(headrTable);
            document.Add(new Paragraph("\n"));

            //-- Ticket Setcion
            var TicketmTable = new PdfPTable(1);
            var iTable1 = new PdfPTable(2);
            float[] width = new float[] { 150f, 850f };
            iTable1.SetWidths(width);
            var logoPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Styles/image"), "logo.png");
            FileInfo lgfi = new FileInfo(logoPath);
            if (lgfi.Exists)
            {
                var _logo = iTextSharp.text.Image.GetInstance(logoPath);
                System.Drawing.Bitmap img = new System.Drawing.Bitmap(lgfi.FullName);
                _logo.ScaleAbsolute(60, 50);
                var logocell = new PdfPCell(_logo);
                logocell.Padding = 5;
                logocell.PaddingLeft = 20;
                logocell.Rowspan = 3;
                iTable1.AddCell(logocell);
            }
            else
            {
                var logocell = new PdfPCell(new Phrase(""));
                iTable1.AddCell(logocell);
            }
            var iTable1o1 = new PdfPTable(3);
            width = new float[] { 250f, 250f, 350f };
            iTable1o1.SetWidths(width);
            cell = new PdfPCell(new Paragraph("Ghacham Inc", font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BorderWidthRight = 0;
            iTable1o1.AddCell(cell);
            cell = new PdfPCell(new Paragraph("PLATINI JEANS CO", font));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BorderWidthLeft = 0;
            cell.BorderWidthRight = 0;
            iTable1o1.AddCell(cell);
            cell = new PdfPCell(new Paragraph("PURCHASE ORDER # " + Model.OrdNum, font));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BorderWidthLeft = 0;
            iTable1o1.AddCell(cell);

            cell = new PdfPCell(iTable1o1);
            iTable1.AddCell(cell);

            Paragraph addpara = new Paragraph("7340 Alondra Blvd. Paramount. CA 90723 / 562-602-0400 / FAX: 562-684-4679 / www.platinijeans.com", font);
            addpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.Colspan = 3;
            cell.AddElement(addpara);
            iTable1.AddCell(cell);

            var iTable2o1 = new PdfPTable(2);
            width = new float[] { 250f, 600f };
            iTable2o1.SetWidths(width);
            string _date = Model.UserId > 0 ? DateTime.UtcNow.ToShortDateString() : "";
            cell = new PdfPCell(new Paragraph("Date : " + _date, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2o1.AddCell(cell);

            cell = new PdfPCell(new Paragraph("Buyer : " + Model.CartOwner.Buyer, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2o1.AddCell(cell);

            cell = new PdfPCell(iTable2o1);
            iTable1.AddCell(cell);

            //--mastercell
            cell = new PdfPCell(iTable1);
            TicketmTable.AddCell(cell);

            //secondtable for 'soldto' and next two
            var iTable2 = new PdfPTable(2);
            width = new float[] { 340f, 510f };
            iTable2.SetWidths(width);

            cell = new PdfPCell(new Paragraph("Sold To : " + Model.CartOwner.CompanyName, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data1 = Model.CartOwner.ShippingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.ShippingAddress.To) ? Model.CartOwner.ShippingAddress.To : " ";
            cell = new PdfPCell(new Paragraph("Ship To : " + data1, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data2 = Model.CartOwner.BillingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.BillingAddress.Line1) ? Model.CartOwner.BillingAddress.Line1 : " ";
            cell = new PdfPCell(new Phrase(data2, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data3 = Model.CartOwner.ShippingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.ShippingAddress.Line1) ? Model.CartOwner.ShippingAddress.Line1 : " ";
            cell = new PdfPCell(new Phrase(data3, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            string data4 = Model.CartOwner.BillingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.BillingAddress.Line2) ? Model.CartOwner.BillingAddress.Line2 : " ";
            cell = new PdfPCell(new Phrase(data4, font));
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            var iTable1o2 = new PdfPTable(2);
            width = new float[] { 400f, 110f };
            string data5 = Model.CartOwner.ShippingAddress != null && !string.IsNullOrEmpty(Model.CartOwner.ShippingAddress.Line2) ? Model.CartOwner.ShippingAddress.Line2 : " ";
            cell = new PdfPCell(new Paragraph(data5, font));
            cell.BorderWidth = 0;
            iTable1o2.AddCell(cell);

            cell = new PdfPCell(new Paragraph("ZipCode : " + (Model.CartOwner.ShippingAddress != null ? Model.CartOwner.ShippingAddress.ZipCode : " "), font));
            cell.BorderWidth = 0;
            iTable1o2.AddCell(cell);

            cell = new PdfPCell(iTable1o2);
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            iTable2.AddCell(cell);

            cell = new PdfPCell(iTable2);
            TicketmTable.AddCell(cell);

            //last three row section

            var iTable3 = new PdfPTable(3);
            width = new float[] { 350f, 350f, 300f };
            iTable3.SetWidths(width);
            cell = new PdfPCell(new Paragraph("Tel : " + Model.CartOwner.Phone, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Fax : " + Model.CartOwner.Fax, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Email : " + Model.CartOwner.Email, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            var dt = Model.Terms.Where(x => x.Id == Model.CartOwner.TermId).FirstOrDefault();
            cell = new PdfPCell(new Paragraph("Terms : " + (dt != null ? dt.Value : " "), font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Sales Person : " + Model.CartOwner.SalesPerson, font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            var dt2 = Model.Shipping.Where(x => x.Id == Model.CartOwner.ShipVia).FirstOrDefault();
            cell = new PdfPCell(new Paragraph("Ship Via : " + (dt2 != null ? dt2.Value : " "), font));
            cell.Padding = 4;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);
            //cell = new PdfPCell(new Paragraph("New User: ", font));
            //cell.Padding = 4;
            //cell.BorderWidth = 0;
            //iTable3.AddCell(cell);
            cell = new PdfPCell(new Paragraph("Note : " + Model.Note, font));
            cell.Padding = 4;
            cell.Colspan = 3;
            cell.BorderWidth = 0;
            iTable3.AddCell(cell);

            cell = new PdfPCell(iTable3);
            cell.PaddingLeft = 5;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            TicketmTable.AddCell(cell);


            document.Add(TicketmTable);
            document.Add(new Paragraph("\n"));

            //-Shopping Table
            int numOfCol = hP ? 8 : 10;
            var mShoppingTable = new PdfPTable(numOfCol);
            if (!hP)
                width = new float[] { 120f, 90f, 70f, 50f, 90f, 90f, 450f, 60f, 50f, 80f };
            else
                width = new float[] { 120f, 90f, 70f, 50f, 90f, 90f, 590f, 50f };
            mShoppingTable.SetWidths(width);

            var hpara = new Paragraph(" ", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Style", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);


            hpara = new Paragraph("Delivery", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Scale", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Scales", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            hpara = new Paragraph("Inseam", font);
            hpara.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.AddElement(hpara);
            cell.BorderWidth = 0;
            cell.PaddingTop = 5;
            cell.PaddingBottom = 5;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
            mShoppingTable.AddCell(cell);

            int colnumber = 0;
            if (!hP)
            {
                colnumber = 10;
                hpara = new Paragraph("Available Open Size", font);
                //hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);

                hpara = new Paragraph("Price", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);

                hpara = new Paragraph("Qty", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);

                hpara = new Paragraph("Total", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);
            }
            else
            {
                colnumber = 8;
                hpara = new Paragraph("Available Open Size", font);
                // hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                // cell.Colspan = 3;
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);


                hpara = new Paragraph("Qty", font);
                hpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.AddElement(hpara);
                cell.BorderWidth = 0;
                cell.PaddingTop = 5;
                cell.PaddingBottom = 5;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#D9D9D9"));
                mShoppingTable.AddCell(cell);


            }

            for (int i = 0; i < Model.Clothes.Count; ++i)
            {
                if (i > 0)
                {
                    cell = new PdfPCell();
                    cell.BorderWidth = 0;
                    cell.BorderWidthBottom = 0.1f;
                    cell.PaddingTop = 10;
                    cell.PaddingBottom = 10;
                    cell.Colspan = hP ? (5 + 3) : (7 + 3);
                    mShoppingTable.AddCell(cell);
                }
                Platini.DB.Order lastOrder;
                lastOrder = db.Orders.Find(Model.OrderId);

                bool first = false;
                for (int j = 0; j < Model.Clothes[i].Contents.Count; ++j)
                {
                    first = j == 0;
                    var iTable = new PdfPTable(1);
                    if (first)
                    {
                        cell = new PdfPCell(new Paragraph(Model.Clothes[i].GroupName, font));
                        cell.BorderWidth = 0;
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 5;
                        iTable.AddCell(cell);
                    }

                    // image section
                    if (HideImage)
                    {
                        
                    }
                    else
                    {
                        var imgPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/WebThumb/"), Model.Clothes[i].Contents[j].Image);
                        FileInfo fi = new FileInfo(imgPath);
                        if (fi.Exists)
                        {
                            var _img = iTextSharp.text.Image.GetInstance(imgPath);
                            System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                            _img.ScaleAbsolute(50, 75);
                            cell = new PdfPCell(_img);
                            cell.BorderWidth = 0; ;
                            iTable.AddCell(cell);
                        }
                        else
                        {
                            imgPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/"), "NO_IMAGE.jpg");
                            fi = new FileInfo(imgPath);
                            if (fi.Exists)
                            {
                                var _img = iTextSharp.text.Image.GetInstance(imgPath);
                                System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                                _img.ScaleAbsolute(50, 75);
                                cell = new PdfPCell(_img);
                                cell.BorderWidth = 0;
                                iTable.AddCell(cell);
                            }
                            else
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0.5f;
                                iTable.AddCell(cell);
                            }
                        }
                    }

                    cell = new PdfPCell(iTable);
                    cell.BorderWidth = 0;
                    mShoppingTable.AddCell(cell);

                    // Style No

                    var ipara = new Paragraph(Model.Clothes[i].Contents[j].StyleNumber, font);
                    ipara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.BorderWidth = 0;
                    cell.PaddingTop = 15;
                    cell.AddElement(ipara);
                    mShoppingTable.AddCell(cell);

                    // Delivery
                    ipara = new Paragraph(Model.Clothes[i].Contents[j].Delivery, font);
                    ipara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.BorderWidth = 0;
                    cell.PaddingTop = 15;
                    cell.AddElement(ipara);
                    mShoppingTable.AddCell(cell);

                    //inner table for other 7 or 5 column
                    int nocc = colnumber == 10 ? 7 : 5;
                    var imTable = new PdfPTable(nocc);
                    if (colnumber == 10)
                        width = new float[] { 50f, 90f, 90f, 450f, 60f, 50f, 80f };
                    else if (colnumber == 8)
                        width = new float[] { 50f, 90f, 90f, 590f, 50f };
                    imTable.SetWidths(width);
                    for (int k = 0; k < Model.Clothes[i].Contents[j].SPs.Count; ++k)
                    {
                        bool wasFitShown = false;
                        if (Model.Clothes[i].Contents[j].SPs[k].Pack != null || Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count > 0)
                        {
                            var iTable22 = new PdfPTable(1);
                            var scaleList = lastOrder.OrderScales.Where(x => x.ClothesScale.ClothesId == Model.Clothes[i].Contents[j].ClothesId);
                            if (Model.Clothes[i].Contents[j].SPs[k].Pack != null)
                            {
                                if (Model.Clothes[i].Contents[j].SPs[k].Pack.ClothesScaleId > 0 && Model.Clothes[i].Contents[j].SPs[k].Pack.ClothesId > 0 && Model.Clothes[i].Contents[j].SPs[k].Pack.OrderSSId != Guid.Empty)
                                {
                                    ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Pack.Name, sfont);
                                    ipara.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.AddElement(ipara);
                                    cell.BorderWidth = 0;
                                    iTable22.AddCell(cell);
                                    if (hP)
                                    {
                                        if (Model.Clothes[i].Contents[j].SPs[k].isConfirmed)
                                            Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].Pack.QuantSum.Value * Model.Clothes[i].Contents[j].SPs[k].Pack.PurchasedQty.Value;
                                    }
                                    else
                                    {
                                        if (Model.Clothes[i].Contents[j].SPs[k].isConfirmed)
                                            Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].Pack.QuantSum.Value * Model.Clothes[i].Contents[j].SPs[k].Pack.PurchasedQty.Value;
                                    }
                                    ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Pack.PurchasedQty.ToString(), font);
                                    ipara.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.AddElement(ipara);
                                    cell.BorderWidth = 0.5f;
                                    iTable22.AddCell(cell);

                                    cell = new PdfPCell(iTable22);
                                    cell.BorderWidth = 0;
                                    imTable.AddCell(cell);
                                }
                                else
                                {
                                    cell = new PdfPCell(new Phrase(" "));
                                    cell.BorderWidth = 0;
                                    imTable.AddCell(cell);
                                }
                            }
                            else
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                imTable.AddCell(cell);
                            }

                            if (!string.IsNullOrEmpty(Model.Clothes[i].Contents[j].SPs[k].Fit))
                            {
                                wasFitShown = true;
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Fit, font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            else
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                imTable.AddCell(cell);
                            }

                            if (!string.IsNullOrEmpty(Model.Clothes[i].Contents[j].SPs[k].Inseam))
                            {
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Inseam, font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            else
                            {
                                cell = new PdfPCell(new Phrase(" "));
                                cell.BorderWidth = 0;
                                imTable.AddCell(cell);
                            }

                            float sizee = hP ? 590f : 450f;
                            int noc = Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count < 12 ? 12 : Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count;
                            float boxsize = sizee / noc;
                            var iTable33 = new PdfPTable(noc);
                            width = new float[noc];
                            for (int n = 0; n < noc; n++)
                            {
                                width[n] = boxsize;
                            }

                            for (int l = 0; l < Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count; l++)
                            {

                                var iiTable = new PdfPTable(1);
                                // float ff = kk + 1;
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].SizeName, sfont);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.AddElement(ipara);
                                iiTable.AddCell(cell);

                                string pval = "X";
                                if ((Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].Quantity.HasValue ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].Quantity.Value > 0 : false) || (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.HasValue ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value > 0 : false))
                                {
                                    if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.HasValue)
                                    {
                                        if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value < 0)
                                        {
                                            pval = Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.ToString();
                                            ipara = new Paragraph(pval, font);
                                            ipara.Alignment = Element.ALIGN_CENTER;
                                            cell = new PdfPCell();
                                            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#ff4c4c"));
                                            cell.PaddingBottom = 5;
                                            cell.BorderWidth = 0.5f;
                                            cell.AddElement(ipara);
                                        }
                                        else
                                        {
                                            pval = Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.ToString();
                                            if (hP)
                                            {
                                                if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].isConfirm)
                                                {
                                                    markConfirm = true;
                                                    Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity != null ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value : 0;
                                                }
                                            }
                                            else
                                            {
                                                if (Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].isConfirm)
                                                {
                                                    markConfirm = true;
                                                    Model.TotalQty += Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity != null ? Model.Clothes[i].Contents[j].SPs[k].OpenSizes[l].PurchasedQuantity.Value : 0;
                                                }
                                            }

                                            ipara = new Paragraph(pval, font);
                                            ipara.Alignment = Element.ALIGN_CENTER;
                                            cell = new PdfPCell();
                                            cell.PaddingBottom = 5;
                                            cell.BorderWidth = 0.5f;
                                            cell.AddElement(ipara);
                                        }
                                    }
                                    else
                                    {
                                        ipara = new Paragraph(pval, font);
                                        ipara.Alignment = Element.ALIGN_CENTER;
                                        cell = new PdfPCell();
                                        cell.PaddingBottom = 5;
                                        cell.BorderWidth = 0.5f;
                                        cell.AddElement(ipara);
                                    }
                                }
                                else
                                {
                                    ipara = new Paragraph(pval, font);
                                    ipara.Alignment = Element.ALIGN_CENTER;
                                    cell = new PdfPCell();
                                    cell.PaddingBottom = 5;
                                    cell.BorderWidth = 0.5f;
                                    cell.AddElement(ipara);
                                }
                                iiTable.AddCell(cell);

                                cell = new PdfPCell(iiTable);
                                cell.BorderWidth = 0;
                                iTable33.AddCell(cell);

                            }
                            int spacebox = 12 - Model.Clothes[i].Contents[j].SPs[k].OpenSizes.Count;
                            if (spacebox > 0)
                            {
                                for (int s = 0; s < spacebox; s++)
                                {
                                    cell = new PdfPCell(new Phrase(" "));
                                    cell.BorderWidth = 0;
                                    iTable33.AddCell(cell);
                                }
                            }

                            cell = new PdfPCell(iTable33);
                            cell.BorderWidth = 0;
                            imTable.AddCell(cell);

                            if (!hP)
                            {
                                //String.Format("{0:0.00}", Model.Clothes[i].Contents[j].Price)
                                ipara = new Paragraph("$" + Model.Clothes[i].Contents[j].Price.ToString("F2"), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }

                            //yellow Mark
                            if (hP && (Model.Clothes[i].Contents[j].SPs[k].isConfirmed || markConfirm))
                            {
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Quantity.ToString(), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                if (Model.Clothes[i].Contents[j].SPs[k].isConfirmed || markConfirm)
                                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFF00"));
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFF00"));
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            else
                            {
                                ipara = new Paragraph(Model.Clothes[i].Contents[j].SPs[k].Quantity.ToString(), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }
                            markConfirm = false;
                            if (!hP)
                            {
                                ipara = new Paragraph("$" + Model.Clothes[i].Contents[j].SPs[k].Total.ToString("F2"), font);
                                ipara.Alignment = Element.ALIGN_CENTER;
                                cell = new PdfPCell();
                                cell.BorderWidth = 0;
                                cell.PaddingTop = 12;
                                cell.AddElement(ipara);
                                imTable.AddCell(cell);
                            }

                            cell = new PdfPCell(new Phrase(" "));
                            cell.BorderWidth = 0;
                            cell.Colspan = hP ? 5 : 7;
                            imTable.AddCell(cell);

                        }
                    }
                    cell = new PdfPCell(imTable);
                    cell.Colspan = hP ? 5 : 7;
                    cell.BorderWidth = 0;
                    cell.PaddingTop = 5;
                    mShoppingTable.AddCell(cell);

                    //cell = new PdfPCell(new Phrase(" "));
                    //cell.Colspan = hP ? (5 + 3) : (7 + 3);
                    //cell.BorderWidth = 0;
                    //mShoppingTable.AddCell(cell);


                }
                //cell = new PdfPCell();
                //cell.BorderWidth = 0;
                //cell.BorderWidthBottom = 0.1f;
                //cell.PaddingTop = 10;
                //cell.PaddingBottom = 10;
                //cell.Colspan = hP ? (5 + 3) : (7 + 3);
                //mShoppingTable.AddCell(cell);


                //Chunk linebreak = new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(4f, 100f, BaseColor.GRAY, Element.ALIGN_CENTER, -1));
                //document.Add(linebreak);

            }

            document.Add(mShoppingTable);
            document.Add(new Paragraph("\n"));

            //Total Table
            var mtotalTable = new PdfPTable(2);
            width = new float[] { 300f, 700f };
            mtotalTable.SetWidths(width);

            cell = new PdfPCell(new Phrase(" "));
            cell.BorderWidth = 0.5f;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#000000"));
            mtotalTable.AddCell(cell);

            var itabel = new PdfPTable(4);
            width = new float[] { 200f, 200f, 150f, 180f };
            itabel.SetWidths(width);


            if (hP)
            {
                Paragraph tpara = new Paragraph("Total Pcs. Packed: " + Model.TotalQty.ToString(), font);
                tpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.Padding = 5;
                cell.Colspan = 4;
                cell.BorderWidthRight = 0;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                cell.AddElement(tpara);
                itabel.AddCell(cell);
            }
            else
            {
                Paragraph tpara = new Paragraph("Total Pcs. Packed: " + Model.TotalQty.ToString(), font);
                tpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.Padding = 5;
                cell.BorderWidthRight = 0;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                cell.AddElement(tpara);
                itabel.AddCell(cell);

                #region OldCodeComment
                tpara = new Paragraph("Grand Total: " + "$" + Model.GrandTotal.ToString("F2"), font);
                tpara.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.Padding = 5;
                cell.BorderWidthLeft = 0;
                cell.BorderWidthRight = 0;
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                cell.AddElement(tpara);
                itabel.AddCell(cell);

                if (Model.Discount > 0)
                {
                    tpara = new Paragraph("Disc (%): " + Model.Discount, font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.BorderWidthLeft = 0;
                    cell.BorderWidthRight = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);

                    tpara = new Paragraph("Final Amount: " + "$" + Model.FinalAmount.ToString("F2"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.BorderWidthLeft = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);
                }
                else
                {
                    tpara = new Paragraph("Final Amount: " + "$" + Model.FinalAmount.ToString("F2"), font);
                    tpara.Alignment = Element.ALIGN_CENTER;
                    cell = new PdfPCell();
                    cell.Padding = 5;
                    cell.Colspan = 2;
                    cell.BorderWidthLeft = 0;
                    cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFFc"));
                    cell.AddElement(tpara);
                    itabel.AddCell(cell);
                }
                #endregion
            }
           

            cell = new PdfPCell();
            cell.AddElement(itabel);
            cell.BorderWidth = 0.5f;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#000000"));
            mtotalTable.AddCell(cell);
            document.Add(mtotalTable);
            return document;
        }

        public static Document Report_SaleByCustomer_Pdfs(Document document, PdfWriter writer, List<SBCRept> list)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var lfont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK);
            var cell = new PdfPCell();
            var para = new Paragraph();
            var text = new Chunk("", font);

            var dTable = new PdfPTable(6);
            var width = new float[] { 150f, 250f, 250f, 120f, 110f, 120f };
            dTable.SetWidths(width);
            para = new Paragraph("Subnitted On", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Customer Name", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Sales Person Name", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Amount", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Cost", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Profit", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);
            //document.Add(hTable);

            decimal totalAmt = 0;
            decimal totalCost = 0;
            decimal totalProfit = 0;
            //var dTable = new PdfPTable(6);
            //width = new float[] { 150f, 250f, 250f, 120f, 110f, 120f };
            //dTable.SetWidths(width);
            foreach (var item in list)
            {
                para = new Paragraph(item.iDate != null ? item.iDate.ToShortDateString() : " ", font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(!string.IsNullOrEmpty(item.Name) ? item.Name : " ", font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(!string.IsNullOrEmpty(item.SalesPerson) ? item.SalesPerson : " ", font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalAmt = totalAmt + item.Amount;
                para = new Paragraph(item.Amount.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalCost = totalCost + item.Cost;
                para = new Paragraph(item.Cost.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalProfit = totalProfit + item.Profit;
                para = new Paragraph(item.Profit.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);
            }
            //Total

            cell = new PdfPCell(new Phrase(" "));
            cell.BorderWidth = 0;
            cell.Colspan = 3;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            dTable.AddCell(cell);

            para = new Paragraph(totalAmt.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);
            para = new Paragraph(totalCost.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(totalProfit.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);
            document.Add(dTable);

            return document;
        }

        public static Document Report_SaleByProduct_Pdfs(Document document, PdfWriter writer, List<InvRept> list)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var lfont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK);
            var cell = new PdfPCell();
            var para = new Paragraph();
            var text = new Chunk("", font);


            var dTable = new PdfPTable(8);
            var width = new float[] { 160f, 140f, 150f, 100f, 160f, 100f, 100f, 90f };
            dTable.SetWidths(width);
            para = new Paragraph("Style #", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Qty. Sold per style", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Amt. Sold per style", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Qty. On Hand", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Amt. Of Qty. On Hand", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("% of qty. sold", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Cost", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Profit", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            decimal totalsQty = 0;
            decimal totalsAmt = 0;
            decimal totalhQTy = 0;
            decimal totalhmQty = 0;
            decimal totalsoAmt = 0;
            decimal totalcost = 0;
            decimal totalprofit = 0;

            foreach (var item in list)
            {
                para = new Paragraph(item.Style, font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalsQty = totalsQty + item.sQty;
                para = new Paragraph(item.sQty.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalsAmt = totalsAmt + item.Amount;
                para = new Paragraph(item.Amount.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalhQTy = totalhQTy + item.cQty;
                para = new Paragraph(item.cQty.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalhmQty = totalhmQty + item.Total;
                para = new Paragraph(item.Total.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalsoAmt = totalsoAmt + item.pQty;
                para = new Paragraph(item.pQty.ToString("F1"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalcost = totalcost + item.Cost;
                para = new Paragraph(item.Cost.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);

                totalprofit = totalprofit + item.Profit;
                para = new Paragraph(item.Profit.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.Padding = 5;
                cell.AddElement(para);
                dTable.AddCell(cell);
            }


            cell = new PdfPCell(new Phrase(" "));
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            dTable.AddCell(cell);

            para = new Paragraph(totalsQty.ToString(), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(totalsAmt.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(totalhQTy.ToString(), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(totalhmQty.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(totalcost.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(totalprofit.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            document.Add(dTable);

            return document;
        }

        public static Document Report_SaleByInventory_Pdfs(Document document, PdfWriter writer, List<InvRept> list)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
            var lfont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK);
            var cell = new PdfPCell();
            var para = new Paragraph();
            var text = new Chunk("", font);
            var dTable = new PdfPTable(13);
            var width = new float[] { 150f, 100f, 100f, 80f, 80f, 80f, 100f, 80f, 80f, 80f, 80f, 80, 110f };
            dTable.SetWidths(width);
            para = new Paragraph("Picture", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Style #", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Date inventory was put in", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Original qty", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("Qty. Adjusted", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ of Qty. Adjusted", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("qty. on hand (current qty.)", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("qty. sold (difference)", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("% of qty. sold", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Cost", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph("$ Amt. Sold", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);


            para = new Paragraph("$ Profit", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);


            para = new Paragraph("Days since inventory was entered", font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.PaddingTop = 5;
            cell.PaddingBottom = 2;
            cell.PaddingLeft = 2;
            cell.PaddingRight = 2;
            cell.AddElement(para);
            dTable.AddCell(cell);

            int total_oQty = 0;
            int total_hQty = 0;
            decimal total_sQty = 0;
            decimal total_cost = 0;
            decimal total_amtSold = 0;
            decimal total_Profit = 0;
            foreach (var item in list)
            {
                var imgPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/MobileThumb/"), item.Pic);
                FileInfo fi = new FileInfo(imgPath);
                if (fi.Exists)
                {
                    var _img = iTextSharp.text.Image.GetInstance(imgPath);
                    System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                    _img.ScaleAbsolute(50, 75);
                    cell = new PdfPCell(_img);
                    cell.PaddingTop = 5;
                    cell.PaddingBottom = 2;
                    cell.PaddingLeft = 2;
                    cell.PaddingRight = 2;
                    cell.BorderWidth = 0;
                    cell.BorderWidthBottom = 0.1f;
                    cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                    dTable.AddCell(cell);
                }
                else
                {
                    imgPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Library/Uploads/"), "NO_IMAGE.jpg");
                    fi = new FileInfo(imgPath);
                    if (fi.Exists)
                    {
                        var _img = iTextSharp.text.Image.GetInstance(imgPath);
                        System.Drawing.Bitmap img = new System.Drawing.Bitmap(fi.FullName);
                        _img.ScaleAbsolute(50, 75);
                        cell = new PdfPCell(_img);
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 2;
                        cell.PaddingLeft = 2;
                        cell.PaddingRight = 2;
                        cell.BorderWidth = 0;
                        cell.BorderWidthBottom = 0.1f;
                        cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                        dTable.AddCell(cell);
                    }
                    else
                    {
                        cell = new PdfPCell(new Phrase(" "));
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 2;
                        cell.PaddingLeft = 2;
                        cell.PaddingRight = 2;
                        cell.BorderWidth = 0.5f;
                        cell.BorderWidthBottom = 0.1f;
                        cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                        dTable.AddCell(cell);
                    }
                }

                para = new Paragraph(item.Style, font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(item.iDate.ToShortDateString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                total_oQty = total_oQty + item.oQty;
                para = new Paragraph(item.oQty.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(item.aQty.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(item.aCost.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                total_hQty = total_hQty + item.cQty;
                para = new Paragraph(item.cQty.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                total_sQty = total_sQty + item.sQty;
                para = new Paragraph(item.sQty.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(item.pQty.ToString("F1"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                total_cost = total_cost + item.Cost;
                para = new Paragraph(item.Cost.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                total_amtSold = total_amtSold + item.Amount;
                para = new Paragraph(item.Amount.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                total_Profit = total_Profit + item.Profit;
                para = new Paragraph(item.Profit.ToString("F2"), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);

                para = new Paragraph(item.Days.ToString(), font);
                para.Alignment = Element.ALIGN_CENTER;
                cell = new PdfPCell();
                cell.BorderWidth = 0;
                cell.BorderWidthBottom = 0.1f;
                cell.BorderColorBottom = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
                cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#FFFFFF"));
                cell.PaddingTop = 5;
                cell.PaddingBottom = 2;
                cell.PaddingLeft = 2;
                cell.PaddingRight = 2;
                cell.AddElement(para);
                dTable.AddCell(cell);
            }
            cell = new PdfPCell(new Phrase(" "));
            cell.Colspan = 3;
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            dTable.AddCell(cell);

            para = new Paragraph(total_oQty.ToString(), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            cell = new PdfPCell(new Phrase(" "));
            cell.Colspan = 2;
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            dTable.AddCell(cell);

            para = new Paragraph(total_hQty.ToString(), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(total_sQty.ToString(), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            cell = new PdfPCell(new Phrase(" "));
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            dTable.AddCell(cell);

            para = new Paragraph(total_cost.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(total_amtSold.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            para = new Paragraph(total_Profit.ToString("F2"), font);
            para.Alignment = Element.ALIGN_CENTER;
            cell = new PdfPCell();
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            cell.AddElement(para);
            dTable.AddCell(cell);

            cell = new PdfPCell(new Phrase(" "));
            cell.BorderWidth = 0;
            cell.BackgroundColor = new BaseColor(System.Drawing.ColorTranslator.FromHtml("#F5F5F5"));
            cell.Padding = 5;
            dTable.AddCell(cell);

            document.Add(dTable);
            return document;
        }
    }
}
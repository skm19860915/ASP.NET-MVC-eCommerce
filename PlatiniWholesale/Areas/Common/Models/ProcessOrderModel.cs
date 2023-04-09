using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class ProcessOrderModel
    {
        public string CategoryName { get; set; }

        public string ImagePath { get; set; }

        public string StyleNumber { get; set; }

        public string Delivery { get; set; }

        public string Scale { get; set; }

        public string Scales { get; set; }

        public string Inseam { get; set; }

        public int Quantity { get; set; }

        public List<SizeQty> SizeList { get; set; }
    }

    public class SizeQty
    {
        public int SizeId { get; set; }

        public string SizeName { get; set; }

        public int Quantity { get; set; }

    }

    public class ProcessModel
    {
        public string OrderId { get; set; }

        public int AccountId { get; set; }

        public List<ProcessOrderModel> POMs { get; set; }
    }

    public class PackingInfo
    {
        public string OrderId { get; set; }

        public string CustomerName { get; set; }

        public string ShippingAddress { get; set; }

        public List<DropDownList> ShipViaList { get; set; }

    }
}
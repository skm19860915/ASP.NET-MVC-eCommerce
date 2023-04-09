using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string StyleNumber { get; set; }

        public string Color { get; set; }

        public decimal? Price { get; set; }

        public string Description { get; set; }

        public int? Clearance { get; set; }

        public decimal? ProductCost { get; set; }

        public int? OriginalQty { get; set; }

        public decimal? MSRP { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class CustomerSalesRecord
    {
        public string OrderId { get; set; }

        public string CustomerName { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? SubmittedOn { get; set; }

        public DateTime? PackedOn { get; set; }

        public DateTime? ShippedOn { get; set; }

        public decimal? GrandTotal { get; set; }

        public decimal? Discount { get; set; }

        public decimal? FinalAmount { get; set; }

        public int? OriginalQuantity { get; set; }

        public int? PackedQuantity { get; set; }
    }
}
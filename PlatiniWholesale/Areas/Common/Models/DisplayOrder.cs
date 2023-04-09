using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class DisplayOrder
    {
        public string OrderId { get; set; }

        public string Address { get; set; }

        public DateTime? Date { get; set; }

        public string CompanyName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int? OriginalQuantity { get; set; }

        public int? PackedQuantity { get; set; }

        public decimal? GrandTotal { get; set; }

        public decimal? Discount { get; set; }

        public decimal? FinalAmount { get; set; }
    }
}
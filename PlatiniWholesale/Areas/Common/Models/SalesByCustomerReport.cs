using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class SalesByCustomerReport
    {
        public int SalesPersonId { get; set; }

        public int OrderStatusId { get; set; }

        public List<DropDownList> SalesPersonList { get; set; }

        public List<Platini.Models.SelectedListValues> OrderSatusList { get; set; }

        public List<Platini.Models.SelectedListValues> Accounts { get; set; }

    }
}
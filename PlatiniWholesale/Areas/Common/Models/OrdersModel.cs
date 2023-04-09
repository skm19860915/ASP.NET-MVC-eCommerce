using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class OrdersModel
    {
        public int RoleId { get; set; }

        public int SalesPersonId { get; set; }

        public string GrandTotal { get; set; }

        public List<Platini.DB.Account> SalesPersons { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class Roles
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public RolePermissions RolePermissions { get; set; }
    }
}
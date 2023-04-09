using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class PermissionModel
    {
        public int RoleId { get; set; }

        public int PermissionId { get; set; }

        public string PermissionPage { get; set; }

        public Platini.DB.RolesPermission RolePermission { get; set; }

    }
}
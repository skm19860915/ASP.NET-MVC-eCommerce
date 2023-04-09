using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class RolePermissionsWithName
    {
        [Required]
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        [Required]
        public int PermissionId { get; set; }

        public string PermissionPage { get; set; }

        public bool? CanView { get; set; }

        public bool? CanEdit { get; set; }

        public bool? CanOrder { get; set; }
    }
}
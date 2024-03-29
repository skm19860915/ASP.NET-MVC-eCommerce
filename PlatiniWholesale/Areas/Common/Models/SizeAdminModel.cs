﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class SizeAdminModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int CategoryId { get; set; }

        public int SubCategoryId { get; set; }

        public int CategoryTypeId { get; set; }

        public int SizeGroupId { get; set; }

        public List<Platini.DB.Category> CategoryList { get; set; }

        public List<Platini.DB.Category> SubCategoryList { get; set; }

        public List<Platini.DB.Category> CategoryTypeList { get; set; }

        public List<Platini.DB.SizeGroup> SizeGroupList { get; set; }
    }
}
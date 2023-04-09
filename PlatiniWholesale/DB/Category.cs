//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Platini.DB
{
    using System;
    using System.Collections.Generic;
    
    public partial class Category
    {
        public Category()
        {
            this.Clothes = new HashSet<Cloth>();
            this.RelatedClothes = new HashSet<RelatedClothe>();
            this.RelatedColors = new HashSet<RelatedColor>();
            this.Sizes = new HashSet<Size>();
            this.SizeGroups = new HashSet<SizeGroup>();
        }
    
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsDelete { get; set; }
        public int ParentId { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
        public int CategoryLevel { get; set; }
    
        public virtual ICollection<Cloth> Clothes { get; set; }
        public virtual ICollection<RelatedClothe> RelatedClothes { get; set; }
        public virtual ICollection<RelatedColor> RelatedColors { get; set; }
        public virtual ICollection<Size> Sizes { get; set; }
        public virtual ICollection<SizeGroup> SizeGroups { get; set; }
    }
}
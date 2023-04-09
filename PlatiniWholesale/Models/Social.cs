using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Platini.Models
{
    public class Location
    {
        public string city { get; set; }
        public string country { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string zip { get; set; }
    }

    public class LocationBase
    {
        public Location location { get; set; }
        public string id { get; set; }
    }

    public class FacebookUser
    {
        [Required]
        public string first_name { get; set; }
        public string last_name { get; set; }
        [Required]
        public string email { get; set; }
        public LocationBase location { get; set; }
        [Required]
        public string id { get; set; }
        [Required]
        public bool isWholeSale { get; set; }
    }

    public class GoogleUser
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string email { get; set; }
        [Required]
        public string id { get; set; }
        [Required]
        public bool isWholeSale { get; set; }
    }
}
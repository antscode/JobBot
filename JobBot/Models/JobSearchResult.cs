using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobBot.Models
{
    public class JobSearchResult
    {
        public string Id { get; set; }
        public string ExternalJobNo { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Categories { get; set; }
        public string Locations { get; set; }
        public string[] LocationList { get; set; }
        public string WorkType { get; set; }
        public string ApplyUrl { get; set; }
    }
}
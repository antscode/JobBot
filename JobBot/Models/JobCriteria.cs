using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobBot.Models
{
    public enum WorkType
    {
        FullTime,
        PartTime,
        Casual,
        Contract
    };

    public enum Location
    {
        Mel,
        Syd
    };

    public class JobCriteria
    {
        public string Title { get; set; }

        public string Location { get; set; }
        public WorkType WorkType { get; set; }
    }
}
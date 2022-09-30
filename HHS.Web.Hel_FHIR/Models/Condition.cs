using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Condition
    {
        public string Category { get; set; }
        public string Code { get; set; }
        public string System { get; set; }
        public string Text { get; set; }
        public DateTime OnsetDate { get; set; }
        public string Asserter { get; set; }
        public string ReferenceUrl { get; set; }
    }
}
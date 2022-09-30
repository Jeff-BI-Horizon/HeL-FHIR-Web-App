using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Observation
    {
        public string Category { get; set; }
        public string Text { get; set; }
        public string Status { get; set; }
        public List<ObservationCoding> ObservationCodings { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Performer { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Interpretation { get; set; }
        public string Range { get; set; }
        public string ReferenceUrl { get; set; }
    }


    public class ObservationCoding
    {
        public string System { get; set; }
        public string Code { get; set; }
        public string Display { get; set; }
    }
}
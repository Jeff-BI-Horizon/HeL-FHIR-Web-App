using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Medication
    {
        public string MedicationStatus { get; set; }
        public string MedicationName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Route { get; set; }
        public string InformationSource { get; set; }
        public string ReferenceUrl { get; set; }
    }
}
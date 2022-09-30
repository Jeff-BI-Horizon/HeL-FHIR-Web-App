using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Immunization
    {
        public string ImmunizationStatus { get; set; }
        public string VaccineName { get; set; }
        public string VaccineDate { get; set; }
        public string Manufacturer { get; set; }
        public string LotNumber { get; set; }
        public string Route { get; set; }
        public string Issuer { get; set; }
        public string ReferenceUrl { get; set; }
    }
}
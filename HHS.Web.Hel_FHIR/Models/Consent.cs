using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Consent
    {
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Organization { get; set;}
        public string Policy { get; set; }
        public DateTime ProvisionStart { get; set; }
        public DateTime ProvisionEnd { get; set; }
        public string Actor { get; set; }
        public string ReferenceUrl { get; set; }

    }
}
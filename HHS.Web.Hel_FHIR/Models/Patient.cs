using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Patient
    {
        public string PatientID { get; set; }
        public string PatientNameUsual { get; set; }
        public List<PatientIdentifier> PatientIdentifiers { get; set; }
        public List<PatientName> PatientNames { get; set; }
        public DateTime BirthDate { get; set; }
        public string MartialStatus { get; set; }
        public List<PatientTele> PatientTeles { get; set; }
        public List<PatientAddress> PatientAddresses { get; set; }
        public List<PatientCommunication> PatientCommunications { get; set; }
        public string ReferenceUrl { get; set; }
    }


    public class PatientIdentifier
    {
        public string Use { get; set; }
        public string IdentifierValue { get; set; }
        public string IdentifierSystem { get; set; }
    }

    public class PatientName
    {
        public string Use { get; set; }
        public string NameText { get; set; }
        public string FamilyName { get; set; }
        public List<string> GivenNames { get; set; }
        public string NamePrefix { get; set; }
        public string NameSuffix { get; set; }
        public DateTime StartDate { get; set; }
    }


    public class PatientTele
    {
        public string Use { get; set; }
        public string TeleValue { get; set; }
        public string TeleSystem { get; set; }
    }

    public class PatientAddress
    {
        public string Use { get; set; }
        public List<string> AddressLines { get; set; }
        public string AddressCity { get; set; }
        public string AddressState { get; set; }
        public string AddressPostal { get; set; }
        public string AddressCountry { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class PatientCommunication
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Display { get; set; }
        public bool Preferred { get; set; }
    }

}
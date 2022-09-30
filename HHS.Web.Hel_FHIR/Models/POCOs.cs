using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{


    public class PersonSearch
    {
        public decimal id { get; set; }
        public string value { get; set; }
        public DateTime? dob { get; set; }
    }



    public class TokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
        public string sub { get; set; }
        public string aud { get; set; }
        public string name { get; set; }
        public string iss { get; set; }
        public string jti { get; set; }
    }



    public class FhirPostParameters
    {
        public string accessToken { get; set; }
        public int patientMrn { get; set; }
    }


    public class FhirEntrytParameters
    {
        public string accessToken { get; set; }
        public string fhirUrl { get; set; }
    }



    public class FhirCallResponseRows
    {
        public int conditionRows { get; set; }
        public string conditionResponse { get; set; }
    }
}
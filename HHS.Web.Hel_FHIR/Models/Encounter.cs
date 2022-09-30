using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHS.Web.Hel_FHIR.Models
{
    public class Encounter
    {
        public string EncounterClass { get; set; }
        public List<EncounterParticipant> EncounterParticipants { get; set; }
        public int ParticipantCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? Length { get; set; }
        public string Units { get; set; }
        public string Reason { get; set; }
        public string Location { get; set; }
        public string ServiceProvider { get; set; }
        public List<EncounterDiagnosis> EncounterDiagnoses {get; set;}
        public string ReferenceUrl { get; set; }
        public DateTime QueryDate { get; set; }
    }



    public class EncounterParticipant
    {
        public string ParticipantName { get; set; }
        public string ParticipantType { get; set; }
    }


    public class EncounterDiagnosis
    {
        public string Condition { get; set; }
    }
}
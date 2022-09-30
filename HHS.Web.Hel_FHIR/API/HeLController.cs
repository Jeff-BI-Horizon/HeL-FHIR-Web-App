using HHS.Web.Hel_FHIR.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http.Headers;
using System.Xml;
using System;
using System.Web;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;

namespace HHS.Web.Hel_FHIR.API
{
    public class HeLController : ApiController
    {




        private HttpClient _client = new HttpClient();

        //private string oid = "2.25.256133121442266547198931747355024016667.1.1.1";
        //private string oid = "2.16.840.1.113883.3.3502.1.1";
        private string oid = "HEL";



        /*
             * 
             *  https://fhirtest.wnyhealthelink.com/R4/Patient/_search?identifier=HEL%7C33789 
             *  https://fhirtest.wnyhealthelink.com/R4/Consent?patient.identifier=HEL%7C33789
             *  https://fhirtest.wnyhealthelink.com/R4/Condition?patient.identifier=HEL%7C33789
             *  https://fhirtest.wnyhealthelink.com/R4/Observation?patient.identifier=HEL%7C33789&category=vital-signs
             *  https://fhirtest.wnyhealthelink.com/R4/Observation?patient.identifier=HEL%7C33789&category=laboratory&date=gt2019-01-01
             *  https://fhirtest.wnyhealthelink.com/R4/Encounter?patient.identifier=HEL%7C33789
             *  https://fhirtest.wnyhealthelink.com/R4/Immunization?patient.identifier=HEL%7C33789
             *  https://fhirtest.wnyhealthelink.com/R4/MedicationStatement?patient.identifier=HEL%7C33789
             *  
             */



        [Route("api/hel/get-access-token")]
        public async Task<TokenResponse> GetAccessToken()
        {
            TokenResponse tokenResponse = new TokenResponse();

            var values = new Dictionary<string, string>
            {
                {"client_id", "horizon" },
                {"client_secret", "vqDk4cpdgPMIWgamdR1MUaDwWbxshlZ6" },
                {"grant_type", "client_credentials"}
            };

            var content = new FormUrlEncodedContent(values);

            HttpResponseMessage response = await _client.PostAsync("https://fhirtest.wnyhealthelink.com/oauth/token", content);

            tokenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

            return tokenResponse;
        }




        [HttpPost, Route("api/hel/patient-details")]
        public async Task<List<Patient>> PatientDemographics([FromBody] FhirPostParameters parameters)
        {


            List<Patient> patientList = new List<Patient>();


            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Patient/_search?identifier={oid}%7C{parameters.patientMrn}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Patient", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);

            //System.Diagnostics.Debug.WriteLine(responseContent);

            XElement document = XElement.Parse(responseContent);

            XNamespace ns = "http://hl7.org/fhir";

            IEnumerable<XElement> entries = document.Descendants(ns + "entry");


            foreach (XElement entry in entries)
            {

                Patient patient = new Patient();
                List<PatientIdentifier> patientIdentifiers = new List<PatientIdentifier>();
                List<PatientName> patientNames = new List<PatientName>();
                List<PatientTele> patientTeles = new List<PatientTele>();
                List<PatientAddress> patientAddresses = new List<PatientAddress>();
                List<PatientCommunication> patientCommunications = new List<PatientCommunication>();


                XElement patientNode = entry.Element(ns + "resource").Element(ns + "Patient");


                if (patientNode.Elements(ns + "id").Any())
                {
                    patient.PatientID = patientNode.Element(ns + "id").Attribute("value").Value;
                }
                if (patientNode.Elements(ns + "birthDate").Any())
                {
                    patient.BirthDate = DateTime.Parse(patientNode.Element(ns + "birthDate").Attribute("value").Value);
                }

                List<XElement> nameNodes = patientNode.Elements(ns + "name").ToList();

                //System.Diagnostics.Debug.WriteLine($"Name Nodes: {nameNodes.Count}");

                foreach (XElement nameNode in nameNodes)
                {
                    PatientName patientName = new PatientName();

                    if (nameNode.Elements(ns + "use").Any())
                    {
                        patientName.Use = nameNode.Element(ns + "use").Attribute("value").Value;
                    }
                    if (nameNode.Elements(ns + "text").Any())
                    {
                        patientName.NameText = nameNode.Element(ns + "text").Attribute("value").Value;
                    }
                    if (nameNode.Elements(ns + "family").Any())
                    {
                        patientName.FamilyName = nameNode.Element(ns + "family").Attribute("value").Value;
                    }
                    if (nameNode.Elements(ns + "period").Elements(ns + "start").Any())
                    {
                        patientName.StartDate = DateTime.Parse(nameNode.Element(ns + "period").Element(ns + "start").Attribute("value").Value);
                    }

                    List<XElement> givenNameNodes = nameNode.Elements(ns + "given").ToList();
                    List<string> givenNames = new List<string>();

                    foreach (XElement givenNameNode in givenNameNodes)
                    {
                        string givenName = givenNameNode.Attribute("value").Value;
                        givenNames.Add(givenName);

                    }
                    patientName.GivenNames = givenNames;

                    if (nameNode.Elements(ns + "prefix").Any())
                    {
                        patientName.NamePrefix = nameNode.Element(ns + "prefix").Attribute("value").Value;
                    }
                    if (nameNode.Elements(ns + "suffix").Any())
                    {
                        patientName.NameSuffix = nameNode.Element(ns + "suffix").Attribute("value").Value;
                    }

                    patientNames.Add(patientName);
                }

                patient.PatientNames = patientNames;


                var patientNameUsual = patient.PatientNames.Where(n => n.Use.ToUpper() == "USUAL")
                                        .OrderByDescending(n => n.StartDate).FirstOrDefault();

                if (patientNameUsual != null)
                {
                    string givenNameUsual = string.Empty;
                    foreach(string givenName in patientNameUsual.GivenNames)
                    {
                        givenNameUsual += $"{givenName} ";
                    }
                    patient.PatientNameUsual = $"{patientNameUsual.FamilyName}, {givenNameUsual.Trim()}";
                }


                List<XElement> idNodes = patientNode.Elements(ns + "identifier").ToList();

                foreach (XElement idNode in idNodes)
                {
                    PatientIdentifier patientIdentifier = new PatientIdentifier();

                    if (idNode.Elements(ns + "use").Any())
                    {
                        patientIdentifier.Use = idNode.Element(ns + "use").Attribute("value").Value;
                    }
                    if (idNode.Elements(ns + "value").Any())
                    {
                        patientIdentifier.IdentifierValue = idNode.Element(ns + "value").Attribute("value").Value;
                    }
                    if (idNode.Elements(ns + "system").Any())
                    {
                        patientIdentifier.IdentifierSystem = idNode.Element(ns + "system").Attribute("value").Value;
                    }

                    patientIdentifiers.Add(patientIdentifier);
                }

                patient.PatientIdentifiers = patientIdentifiers;



                List<XElement> addressNodes = patientNode.Elements(ns + "address").ToList();

                foreach (XElement addressNode in addressNodes)
                {
                    PatientAddress patientAddress = new PatientAddress();

                    if (addressNode.Elements(ns + "use").Any())
                    {
                        patientAddress.Use = addressNode.Element(ns + "use").Attribute("value").Value;
                    }

                    List<XElement> addressLineNodes = addressNode.Elements(ns + "line").ToList();
                    List<string> addressLines = new List<string>();
                    foreach (XElement addressLineNode in addressLineNodes)
                    {
                        string addressLine = addressLineNode.Attribute("value").Value;
                        addressLines.Add(addressLine);
                    }
                    patientAddress.AddressLines = addressLines;

                    if (addressNode.Elements(ns + "city").Any())
                    {
                        patientAddress.AddressCity = addressNode.Element(ns + "city").Attribute("value").Value;
                    }
                    if (addressNode.Elements(ns + "state").Any())
                    {
                        patientAddress.AddressState = addressNode.Element(ns + "state").Attribute("value").Value;
                    }
                    if (addressNode.Elements(ns + "postalCode").Any())
                    {
                        patientAddress.AddressPostal = addressNode.Element(ns + "postalCode").Attribute("value").Value;
                    }
                    if (addressNode.Elements(ns + "country").Any())
                    {
                        patientAddress.AddressCountry = addressNode.Element(ns + "country").Attribute("value").Value;
                    }
                    if (addressNode.Elements(ns + "period").Elements(ns + "start").Any())
                    {
                        patientAddress.StartDate = DateTime.Parse(addressNode.Element(ns + "period").Element(ns + "start").Attribute("value").Value);
                    }
                    patientAddresses.Add(patientAddress);
                }

                patient.PatientAddresses = patientAddresses;

                List<XElement> teleNodes = patientNode.Elements(ns + "telecom").ToList();

                foreach (XElement teleNode in teleNodes)
                {

                    PatientTele patientTele = new PatientTele();

                    if (teleNode.Elements(ns + "use").Any())
                    {
                        patientTele.Use = teleNode.Element(ns + "use").Attribute("value").Value;
                    }
                    if (teleNode.Elements(ns + "value").Any())
                    {
                        patientTele.TeleValue = teleNode.Element(ns + "value").Attribute("value").Value;
                    }
                    if (teleNode.Elements(ns + "system").Any())
                    {
                        patientTele.TeleSystem = teleNode.Element(ns + "system").Attribute("value").Value;
                    }

                    patientTeles.Add(patientTele);
                }

                patient.PatientTeles = patientTeles;


                List<XElement> communicationNodes = patientNode.Elements(ns + "communication").ToList();

                foreach (XElement communicationNode in communicationNodes)
                {
                    PatientCommunication patientCommunication = new PatientCommunication();

                    if (communicationNode.Elements(ns + "language").Any())
                    {
                        patientCommunication.Type = "Language";
                    }
                    if (communicationNode.Elements(ns + "language").Elements(ns + "coding").Elements(ns + "display").Any())
                    {
                        patientCommunication.Display = communicationNode.Element(ns + "language").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                    }
                    if (communicationNode.Elements(ns + "language").Elements(ns + "coding").Elements(ns + "code").Any())
                    {
                        patientCommunication.Code = communicationNode.Element(ns + "language").Element(ns + "coding").Element(ns + "code").Attribute("value").Value;
                    }
                    if (communicationNode.Elements(ns + "preferred").Any())
                    {
                        patientCommunication.Preferred = bool.Parse(communicationNode.Element(ns + "preferred").Attribute("value").Value);
                    }

                    patientCommunications.Add(patientCommunication);
                }

                patient.PatientCommunications = patientCommunications;


                if (patientNode.Elements(ns + "maritalStatus").Elements(ns + "coding").Elements(ns + "display").Any())
                {
                    patient.MartialStatus = patientNode.Element(ns + "maritalStatus").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                }


                patientList.Add(patient);


                //await LogRequestDetail(logId, patient.ReferenceUrl, "System", patient.sys)
            }


            return patientList;
        }



        [HttpPost, Route("api/hel/patient-consents")]
        public async Task<List<Consent>> PatientConsents([FromBody] FhirPostParameters parameters)
        {


            List<Consent> consentList = new List<Consent>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 3, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Consent?patient.identifier={oid}%7C{parameters.patientMrn}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Consents", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);


            XElement document = XElement.Parse(responseContent);

            XNamespace ns = "http://hl7.org/fhir";

            IEnumerable<XElement> entries = document.Descendants(ns + "entry");


            foreach (XElement entry in entries)
            {
                Consent consent = new Consent();

                XElement consentNode = entry.Element(ns + "resource").Element(ns + "Consent");


                if (consentNode.Elements(ns + "status").Any())
                {
                    consent.Status = consentNode.Element(ns + "status").Attribute("value").Value;
                }

                if (consentNode.Elements(ns + "dateTime").Any())
                {
                    consent.Date = DateTime.Parse(consentNode.Element(ns + "dateTime").Attribute("value").Value);
                }

                if (consentNode.Element(ns + "organization").Elements(ns + "display").Any())
                {
                    consent.Organization = consentNode.Element(ns + "organization").Element(ns + "display").Attribute("value").Value;
                }
                if (consentNode.Elements(ns + "policyRule").Elements(ns + "coding").Elements(ns + "code").Any())
                {
                    consent.Policy = consentNode.Element(ns + "policyRule").Element(ns + "coding").Element(ns + "code").Attribute("value").Value;
                }
                if (consentNode.Elements(ns + "provision").Elements(ns + "period").Elements(ns + "start").Any())
                {
                    consent.ProvisionStart = DateTime.Parse(consentNode.Element(ns + "provision").Element(ns + "period").Element(ns + "start").Attribute("value").Value);
                }
                if (consentNode.Elements(ns + "provision").Elements(ns + "period").Elements(ns + "end").Any())
                {
                    consent.ProvisionEnd = DateTime.Parse(consentNode.Element(ns + "provision").Element(ns + "period").Element(ns + "end").Attribute("value").Value);
                }
                if (consentNode.Elements(ns + "provision").Elements(ns + "actor").Elements(ns + "reference").Elements(ns + "display").Any())
                {
                    consent.Actor = consentNode.Element(ns + "provision").Element(ns + "actor").Element(ns + "reference").Element(ns + "display").Attribute("value").Value;
                }




                consent.ReferenceUrl = entry.Element(ns + "fullUrl").Attribute("value").Value;

                consentList.Add(consent);

                await LogRequestDetail(logId, consent.ReferenceUrl, "Organization", consent.Organization);
            }

            return consentList;
        }



        [HttpPost, Route("api/hel/patient-encounters")]
        public async Task<List<Encounter>> PatientEncounters([FromBody] FhirPostParameters parameters)
        {


            List<Encounter> encounterList = new List<Encounter>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 3, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Encounter?patient.identifier={oid}%7C{parameters.patientMrn}&date=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Encounters", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);



            XElement document = XElement.Parse(responseContent);

            var namespaceResolver = new XmlNamespaceManager(new NameTable());
            namespaceResolver.AddNamespace("f", "http://hl7.org/fhir");
            XNamespace ns = "http://hl7.org/fhir";

            IEnumerable<XElement> entries = document.Descendants(ns + "entry");

            //System.Diagnostics.Debug.WriteLine($"Entry Nodes: {entries.Count}");

            foreach (XElement entry in entries)
            {
                Encounter encounter = new Encounter();
                List<EncounterParticipant> encounterParticipants = new List<EncounterParticipant>();
                List<EncounterDiagnosis> encounterDiagnoses = new List<EncounterDiagnosis>();

                XElement encounterNode = entry.Element(ns + "resource").Element(ns + "Encounter");



                var participantNodes = encounterNode.Descendants(ns + "participant").ToList();
                if (participantNodes.Count > 0)
                {
                    foreach (XElement participant in participantNodes)
                    {
                        encounterParticipants.Add(new EncounterParticipant
                        {
                            ParticipantName = participant.Element(ns + "individual").Element(ns + "display").Attribute("value").Value,
                            ParticipantType = participant.Element(ns + "type").Element(ns + "coding").Element(ns + "display").Attribute("value").Value
                        });
                    }
                }

                var diagnosisNodes = encounterNode.Descendants(ns + "diagnosis").ToList();
                if (diagnosisNodes.Count > 0)
                {
                    foreach (XElement diagnosis in diagnosisNodes)
                    {
                        encounterDiagnoses.Add(new EncounterDiagnosis
                        {
                            Condition = diagnosis.Element(ns + "condition").Element(ns + "display").Attribute("value").Value
                        });
                    }
                }



                encounter.EncounterClass = encounterNode.Element(ns + "class").Element(ns + "display").Attribute("value").Value;
                encounter.EncounterParticipants = encounterParticipants;
                encounter.EncounterDiagnoses = encounterDiagnoses;

                if (encounterNode.Elements(ns + "period").Elements(ns + "start").Any())
                {
                    encounter.StartDate = DateTime.Parse(encounterNode.Element(ns + "period").Element(ns + "start").Attribute("value").Value);
                }
                if (encounterNode.Elements(ns + "period").Elements(ns + "end").Any())
                {
                    encounter.EndDate = DateTime.Parse(encounterNode.Element(ns + "period").Element(ns + "end").Attribute("value").Value);
                }
                if (encounterNode.Elements(ns + "reasonCode").Elements(ns + "coding").Elements(ns + "display").Any())
                {
                    encounter.Reason = encounterNode.Element(ns + "reasonCode").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                }
                if (encounterNode.Elements(ns + "location").Elements(ns + "location").Elements(ns + "display").Any())
                {
                    encounter.Location = encounterNode.Element(ns + "location").Element(ns + "location").Element(ns + "display").Attribute("value").Value;
                }
                if (encounterNode.Elements(ns + "serviceProvider").Elements(ns + "display").Any())
                {
                    encounter.ServiceProvider = encounterNode.Element(ns + "serviceProvider").Element(ns + "display").Attribute("value").Value;
                }

                encounter.ReferenceUrl = entry.Element(ns + "fullUrl").Attribute("value").Value;

                encounterList.Add(encounter);

                await LogRequestDetail(logId, encounter.ReferenceUrl, "ServiceProvider", encounter.ServiceProvider);
            }

            encounterList = encounterList.OrderByDescending(c => c.StartDate).ToList();

            return encounterList;


        }




        [HttpPost, Route("api/hel/patient-conditions")]
        public async Task<List<Condition>> PatientConditions([FromBody] FhirPostParameters parameters)
        {


            List<Condition> conditionList = new List<Condition>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 7, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Condition?patient.identifier={oid}%7C{parameters.patientMrn}&onset-date=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Conditions", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);

            XmlDocument document = new XmlDocument();
            document.LoadXml(responseContent);

            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("f", "http://hl7.org/fhir");
            XmlNodeList entryNodes = document.SelectNodes("//f:entry", nsmgr);

            foreach (XmlNode node in entryNodes)
            {

                XmlNode referenceUrlNode = node.SelectSingleNode("f:fullUrl", nsmgr);
                XmlNode conditionNode = node.SelectSingleNode("f:resource/f:Condition", nsmgr);
                XmlNode categoryNode = conditionNode.SelectSingleNode("f:category/f:coding/f:display", nsmgr);
                XmlNode codeNode = conditionNode.SelectSingleNode("f:code/f:coding/f:code", nsmgr);
                XmlNode systemNode = conditionNode.SelectSingleNode("f:code/f:coding/f:system", nsmgr);
                XmlNode textNode = conditionNode.SelectSingleNode("f:code/f:coding/f:display", nsmgr);
                XmlNode onsetDateNode = conditionNode.SelectSingleNode("f:onsetDateTime", nsmgr);
                XmlNode asserterNode = conditionNode.SelectSingleNode("f:asserter/f:display", nsmgr);



                conditionList.Add(new Condition
                {
                    Category = NodeAttributeToString(categoryNode, "value"),
                    Code = NodeAttributeToString(codeNode, "value"),
                    System = NodeAttributeToString(systemNode, "value"),
                    Text = NodeAttributeToString(textNode, "value"),
                    OnsetDate = NodeAttributeToDate(onsetDateNode, "value"),
                    Asserter = NodeAttributeToString(asserterNode, "value"),
                    ReferenceUrl = referenceUrlNode == null ? string.Empty : referenceUrlNode.Attributes["value"].Value
                });

                await LogRequestDetail(logId, referenceUrlNode == null ? string.Empty : referenceUrlNode.Attributes["value"].Value,
                                    "Asserter", NodeAttributeToString(asserterNode, "value"));
            }


            conditionList = conditionList.OrderByDescending(c => c.OnsetDate).ToList();



            return conditionList;


        }



        [HttpPost, Route("api/hel/patient-medications")]
        public async Task<List<Medication>> PatientMedications([FromBody] FhirPostParameters parameters)
        {


            List<Medication> medicationList = new List<Medication>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 7, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/MedicationStatement?patient.identifier={oid}%7C{parameters.patientMrn}&effective=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Medications", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);

            XmlDocument document = new XmlDocument();
            document.LoadXml(responseContent);

            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("f", "http://hl7.org/fhir");
            XmlNodeList entryNodes = document.SelectNodes("//f:entry", nsmgr);

            foreach (XmlNode node in entryNodes)
            {

                XmlNode referenceUrlNode = node.SelectSingleNode("f:fullUrl", nsmgr);
                XmlNode medicationNode = node.SelectSingleNode("f:resource/f:MedicationStatement", nsmgr);
                XmlNode statusNode = medicationNode.SelectSingleNode("f:status", nsmgr);
                XmlNode medicationNameNode = medicationNode.SelectSingleNode("f:medicationCodeableConcept/f:coding/f:display", nsmgr);
                XmlNode startDateNode = medicationNode.SelectSingleNode("f:effectivePeriod/f:start", nsmgr);
                XmlNode endDateNode = medicationNode.SelectSingleNode("f:effectivePeriod/f:end", nsmgr);
                XmlNode informationSourceNode = medicationNode.SelectSingleNode("f:informationSource/f:display", nsmgr);
                XmlNode routeNode = medicationNode.SelectSingleNode("f:dosage/f:route/f:coding/f:display", nsmgr);



                medicationList.Add(new Medication
                {
                    MedicationStatus = NodeAttributeToString(statusNode, "value"),
                    MedicationName = NodeAttributeToString(medicationNameNode, "value"),
                    StartDate = NodeAttributeToDate(startDateNode, "value"),
                    EndDate = NodeAttributeToDate(endDateNode, "value"),
                    Route = NodeAttributeToString(routeNode, "value"),
                    InformationSource = NodeAttributeToString(informationSourceNode, "value"),
                    ReferenceUrl = NodeAttributeToString(referenceUrlNode, "value")
                });


                // await LogRequestDetail(logId, )
            }


            medicationList = medicationList.OrderByDescending(c => c.StartDate).ToList();



            return medicationList;


        }



        [HttpPost, Route("api/hel/patient-labs")]
        public async Task<List<Observation>> PatientLabs([FromBody] FhirPostParameters parameters)
        {


            List<Observation> observationList = new List<Observation>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 3, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Observation?patient.identifier={oid}%7C{parameters.patientMrn}&category=laboratory&date=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            _client.Timeout = new TimeSpan(0, 3, 0);

            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Labs", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);


            XElement document = XElement.Parse(responseContent);

            XNamespace ns = "http://hl7.org/fhir";

            IEnumerable<XElement> entries = document.Descendants(ns + "entry");


            foreach (XElement entry in entries)
            {
                Observation observation = new Observation();

                XElement observationNode = entry.Element(ns + "resource").Element(ns + "Observation");

                if (observationNode.Elements(ns + "text").Any())
                {
                    //observation.Text = entry.Element(ns + "text").ToString();
                    //observation.Text = string.Join("", entry.Element(ns + "text").Elements().Select(o => o.ToString()));
                    var reader = observationNode.Element(ns + "text").CreateReader();
                    reader.MoveToContent();
                    observation.Text = reader.ReadInnerXml();
                    reader.Dispose();
                }

                observation.Category = observationNode.Element(ns + "category").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;

                if (observationNode.Elements(ns + "status").Any())
                {
                    observation.Status = observationNode.Element(ns + "status").Attribute("value").Value;
                }

                if (observationNode.Elements(ns + "effectivePeriod").Elements(ns + "start").Any())
                {
                    observation.StartDate = DateTime.Parse(observationNode.Element(ns + "effectivePeriod").Element(ns + "start").Attribute("value").Value);
                }
                if (observationNode.Elements(ns + "effectivePeriod").Elements(ns + "end").Any())
                {
                    observation.EndDate = DateTime.Parse(observationNode.Element(ns + "effectivePeriod").Element(ns + "end").Attribute("value").Value);
                }
                if (observationNode.Element(ns + "performer").Elements(ns + "display").Any())
                {
                    observation.Performer = observationNode.Element(ns + "performer").Element(ns + "display").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "valueQuantity").Elements(ns + "value").Any())
                {
                    observation.Value = observationNode.Element(ns + "valueQuantity").Element(ns + "value").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "valueQuantity").Elements(ns + "unit").Any())
                {
                    observation.Unit = observationNode.Element(ns + "valueQuantity").Element(ns + "unit").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "interpretation").Elements(ns + "coding").Elements(ns + "display").Any())
                {
                    observation.Interpretation = observationNode.Element(ns + "interpretation").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "referenceRange").Elements(ns + "text").Any())
                {
                    observation.Range = observationNode.Element(ns + "referenceRange").Element(ns + "text").Attribute("value").Value;
                }

                List<ObservationCoding> observationCodings = new List<ObservationCoding>();

                //var codingNodes = observationNode.Descendants(ns + "code").ToList();
                List<XElement> codingNodes = observationNode.Elements(ns + "code").ToList();
                if (codingNodes.Count > 0)
                {
                    foreach (XElement code in codingNodes)
                    {
                        ObservationCoding observationCoding = new ObservationCoding();

                        if (code.Elements(ns + "coding").Elements(ns + "code").Any())
                        {
                            observationCoding.Code = code.Element(ns + "coding").Element(ns + "code").Attribute("value").Value;
                        }
                        //if (code.Elements(ns + "coding").Elements(ns + "system").Any())
                        //{
                        //    observationCoding.System = code.Element(ns + "coding").Element(ns + "system").Attribute("value").Value;
                        //}
                        if (code.Elements(ns + "coding").Elements(ns + "display").Any())
                        {
                            observationCoding.Display = code.Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                        }
                        observationCodings.Add(observationCoding);
                    }
                }
                observation.ObservationCodings = observationCodings;

                observation.ReferenceUrl = entry.Element(ns + "fullUrl").Attribute("value").Value;

                observationList.Add(observation);

                await LogRequestDetail(logId, observation.ReferenceUrl, "Performer", observation.Performer);
            }

            return observationList;
        }



        [HttpPost, Route("api/hel/patient-vitals")]
        public async Task<List<Observation>> PatientVitals([FromBody] FhirPostParameters parameters)
        {


            List<Observation> observationList = new List<Observation>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 3, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Observation?patient.identifier={oid}%7C{parameters.patientMrn}&category=vital-signs&date=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Vitals", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);


            XElement document = XElement.Parse(responseContent);

            XNamespace ns = "http://hl7.org/fhir";

            IEnumerable<XElement> entries = document.Descendants(ns + "entry");


            foreach (XElement entry in entries)
            {
                Observation observation = new Observation();

                XElement observationNode = entry.Element(ns + "resource").Element(ns + "Observation");

                if (observationNode.Elements(ns + "text").Any())
                {
                    var reader = observationNode.Element(ns + "text").CreateReader();
                    reader.MoveToContent();
                    observation.Text = reader.ReadInnerXml();
                    reader.Dispose();
                }

                observation.Category = observationNode.Element(ns + "category").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;

                if (observationNode.Elements(ns + "status").Any())
                {
                    observation.Status = observationNode.Element(ns + "status").Attribute("value").Value;
                }

                if (observationNode.Elements(ns + "effectivePeriod").Elements(ns + "start").Any())
                {
                    observation.StartDate = DateTime.Parse(observationNode.Element(ns + "effectivePeriod").Element(ns + "start").Attribute("value").Value);
                }
                if (observationNode.Elements(ns + "effectivePeriod").Elements(ns + "end").Any())
                {
                    observation.EndDate = DateTime.Parse(observationNode.Element(ns + "effectivePeriod").Element(ns + "end").Attribute("value").Value);
                }
                if (observationNode.Element(ns + "performer").Elements(ns + "display").Any())
                {
                    observation.Performer = observationNode.Element(ns + "performer").Element(ns + "display").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "valueQuantity").Elements(ns + "value").Any())
                {
                    observation.Value = observationNode.Element(ns + "valueQuantity").Element(ns + "value").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "valueQuantity").Elements(ns + "unit").Any())
                {
                    observation.Unit = observationNode.Element(ns + "valueQuantity").Element(ns + "unit").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "interpretation").Elements(ns + "coding").Elements(ns + "display").Any())
                {
                    observation.Interpretation = observationNode.Element(ns + "interpretation").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "referenceRange").Elements(ns + "text").Any())
                {
                    observation.Range = observationNode.Element(ns + "referenceRange").Element(ns + "text").Attribute("value").Value;
                }

                List<ObservationCoding> observationCodings = new List<ObservationCoding>();

                //var codingNodes = observationNode.Descendants(ns + "code").ToList();
                List<XElement> codingNodes = observationNode.Elements(ns + "code").ToList();
                if (codingNodes.Count > 0)
                {
                    foreach (XElement code in codingNodes)
                    {
                        ObservationCoding observationCoding = new ObservationCoding();

                        if (code.Elements(ns + "coding").Elements(ns + "code").Any())
                        {
                            observationCoding.Code = code.Element(ns + "coding").Element(ns + "code").Attribute("value").Value;
                        }
                        //if (code.Elements(ns + "coding").Elements(ns + "system").Any())
                        //{
                        //    observationCoding.System = code.Element(ns + "coding").Element(ns + "system").Attribute("value").Value;
                        //}
                        if (code.Elements(ns + "coding").Elements(ns + "display").Any())
                        {
                            observationCoding.Display = code.Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                        }
                        observationCodings.Add(observationCoding);
                    }
                }
                observation.ObservationCodings = observationCodings;

                observation.ReferenceUrl = entry.Element(ns + "fullUrl").Attribute("value").Value;

                observationList.Add(observation);


                await LogRequestDetail(logId, observation.ReferenceUrl, "Performer", observation.Performer);
            }

            return observationList;
        }


        [HttpPost, Route("api/hel/patient-socials")]
        public async Task<List<Observation>> PatientSocials([FromBody] FhirPostParameters parameters)
        {


            List<Observation> observationList = new List<Observation>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 3, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Observation?patient.identifier={oid}%7C{parameters.patientMrn}&category=social-care&date=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            await LogRequest(parameters.patientMrn, "Social Care", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);

            System.Diagnostics.Debug.WriteLine(responseContent);

            XElement document = XElement.Parse(responseContent);

            XNamespace ns = "http://hl7.org/fhir";

            IEnumerable<XElement> entries = document.Descendants(ns + "entry");


            foreach (XElement entry in entries)
            {
                Observation observation = new Observation();

                XElement observationNode = entry.Element(ns + "resource").Element(ns + "Observation");

                if (observationNode.Elements(ns + "text").Any())
                {
                    var reader = observationNode.Element(ns + "text").CreateReader();
                    reader.MoveToContent();
                    observation.Text = reader.ReadInnerXml();
                    reader.Dispose();
                }

                observation.Category = observationNode.Element(ns + "category").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;

                if (observationNode.Elements(ns + "status").Any())
                {
                    observation.Status = observationNode.Element(ns + "status").Attribute("value").Value;
                }

                if (observationNode.Elements(ns + "effectivePeriod").Elements(ns + "start").Any())
                {
                    observation.StartDate = DateTime.Parse(observationNode.Element(ns + "effectivePeriod").Element(ns + "start").Attribute("value").Value);
                }
                if (observationNode.Elements(ns + "effectivePeriod").Elements(ns + "end").Any())
                {
                    observation.EndDate = DateTime.Parse(observationNode.Element(ns + "effectivePeriod").Element(ns + "end").Attribute("value").Value);
                }
                if (observationNode.Element(ns + "performer").Elements(ns + "display").Any())
                {
                    observation.Performer = observationNode.Element(ns + "performer").Element(ns + "display").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "valueQuantity").Elements(ns + "value").Any())
                {
                    observation.Value = observationNode.Element(ns + "valueQuantity").Element(ns + "value").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "valueQuantity").Elements(ns + "unit").Any())
                {
                    observation.Unit = observationNode.Element(ns + "valueQuantity").Element(ns + "unit").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "interpretation").Elements(ns + "coding").Elements(ns + "display").Any())
                {
                    observation.Interpretation = observationNode.Element(ns + "interpretation").Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                }
                if (observationNode.Elements(ns + "referenceRange").Elements(ns + "text").Any())
                {
                    observation.Range = observationNode.Element(ns + "referenceRange").Element(ns + "text").Attribute("value").Value;
                }

                List<ObservationCoding> observationCodings = new List<ObservationCoding>();

                //var codingNodes = observationNode.Descendants(ns + "code").ToList();
                List<XElement> codingNodes = observationNode.Elements(ns + "code").ToList();
                if (codingNodes.Count > 0)
                {
                    foreach (XElement code in codingNodes)
                    {
                        ObservationCoding observationCoding = new ObservationCoding();

                        if (code.Elements(ns + "coding").Elements(ns + "code").Any())
                        {
                            observationCoding.Code = code.Element(ns + "coding").Element(ns + "code").Attribute("value").Value;
                        }
                        //if (code.Elements(ns + "coding").Elements(ns + "system").Any())
                        //{
                        //    observationCoding.System = code.Element(ns + "coding").Element(ns + "system").Attribute("value").Value;
                        //}
                        if (code.Elements(ns + "coding").Elements(ns + "display").Any())
                        {
                            observationCoding.Display = code.Element(ns + "coding").Element(ns + "display").Attribute("value").Value;
                        }
                        observationCodings.Add(observationCoding);
                    }
                }
                observation.ObservationCodings = observationCodings;

                observation.ReferenceUrl = entry.Element(ns + "fullUrl").Attribute("value").Value;

                observationList.Add(observation);
            }

            return observationList;
        }




        [HttpPost, Route("api/hel/patient-immunizations")]
        public async Task<List<Immunization>> PatientImmunizations([FromBody] FhirPostParameters parameters)
        {

            List<Immunization> immunizationList = new List<Immunization>();


            int currentYear = DateTime.Today.Year;
            string queryDate = new DateTime(currentYear - 5, 1, 1).ToString("yyyy-dd-MM");

            string requestUrl = $"https://fhirtest.wnyhealthelink.com/R4/Immunization?patient.identifier={oid}%7C{parameters.patientMrn}&date=ge{queryDate}&_format=xml";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(requestUrl);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            int logId = await LogRequest(parameters.patientMrn, "Immunizations", requestUrl, ((int)responseMessage.StatusCode), statusMessage, string.Empty);


            XmlDocument document = new XmlDocument();
            document.LoadXml(responseContent);

            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("f", "http://hl7.org/fhir");
            XmlNodeList entryNodes = document.SelectNodes("//f:entry", nsmgr);

            foreach (XmlNode node in entryNodes)
            {

                XmlNode referenceUrlNode = node.SelectSingleNode("f:fullUrl", nsmgr);
                XmlNode immunizationNode = node.SelectSingleNode("f:resource/f:Immunization", nsmgr);
                XmlNode statusNode = immunizationNode.SelectSingleNode("f:status", nsmgr);
                XmlNode vaccineNameNode = immunizationNode.SelectSingleNode("f:vaccineCode/f:coding/f:display", nsmgr);
                XmlNode occurrenceDateNode = immunizationNode.SelectSingleNode("f:occurrenceDateTime", nsmgr);
                XmlNode manufacturerNode = immunizationNode.SelectSingleNode("f:manufacturer/f:display", nsmgr);
                XmlNode lotNumberNode = immunizationNode.SelectSingleNode("f:lotNumber", nsmgr);
                XmlNode routeNode = immunizationNode.SelectSingleNode("f:route/f:coding/f:display", nsmgr);
                XmlNode issuerNode = immunizationNode.SelectSingleNode("f:performer/f:actor/f:display", nsmgr);



                immunizationList.Add(new Immunization
                {
                    ImmunizationStatus = statusNode == null ? string.Empty : statusNode.Attributes["value"].Value,
                    VaccineName = vaccineNameNode == null ? string.Empty : vaccineNameNode.Attributes["value"].Value,
                    VaccineDate = NodeAttributeToDate(occurrenceDateNode, "value").ToShortDateString(),
                    Manufacturer = NodeAttributeToString(manufacturerNode, "value"),
                    LotNumber = NodeAttributeToString(lotNumberNode, "value"),
                    Route = NodeAttributeToString(routeNode, "value"),
                    Issuer = NodeAttributeToString(issuerNode, "value"),
                    ReferenceUrl = referenceUrlNode == null ? string.Empty : referenceUrlNode.Attributes["value"].Value
                });

                
            }





            return immunizationList;


        }




        [HttpPost, Route("api/hel/entry-details")]
        public async Task EntryDetails([FromBody]FhirEntrytParameters parameters)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", parameters.accessToken);
            HttpResponseMessage responseMessage = await _client.GetAsync(parameters.fhirUrl + "?_format=xml");

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            string statusMessage = string.Empty;

            if (!responseMessage.IsSuccessStatusCode)
            {
                statusMessage = responseContent;
            }

            DateTime fileStamp = DateTime.Now;
            File.WriteAllText($"H:\\HeL FHIR App\\test_{fileStamp.ToString("yyyyMMddhhmmss")}.xml", responseContent);

            
        }





        private string NodeAttributeToString(XmlNode node, string attributeName)
        {
            string attributeValue = string.Empty;
            if (node != null)
            {
                if (node.Attributes[attributeName] != null)
                {
                    attributeValue = node.Attributes[attributeName].Value;
                }
            }

            return attributeValue;
        }


        private DateTime NodeAttributeToDate(XmlNode node, string attributeName)
        {
            DateTime attributeValue = DateTime.MinValue;
            if (node != null)
            {
                if (node.Attributes[attributeName] != null)
                {
                    attributeValue = DateTime.Parse(node.Attributes[attributeName].Value);
                }
            }

            return attributeValue;
        }


        private decimal? NodeAttributeToDecimal(XmlNode node, string attributeName)
        {

            if (node != null)
            {
                if (node.Attributes[attributeName] != null)
                {
                    return decimal.Parse(node.Attributes[attributeName].Value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }


        }



        private string XelementToString(XElement element, string attributeName)
        {
            string attributeValue = string.Empty;
            if (element != null)
            {
                if (element.Attribute(attributeName) != null)
                {
                    attributeValue = element.Attribute(attributeName).Value;
                }
            }

            return attributeValue;
        }

        private DateTime XelementToDate(XElement element, string attributeName)
        {
            DateTime attributeValue = DateTime.MinValue;
            if (element != null)
            {
                if (element.Attribute(attributeName) != null)
                {
                    attributeValue = DateTime.Parse(element.Attribute(attributeName).Value);
                }
            }

            return attributeValue;
        }




        private async Task<int> LogRequest(int patientMrn, string requestDomain, string requestUrl, int requestStatusCode, string requestStatusMessage, string phiSource)
        {
            AnaReportingEntities db = new AnaReportingEntities();

            RequestLog log = new RequestLog();

            log.requestStamp = DateTime.Now;
            log.userName = RequestContext.Principal.Identity.Name;
            log.userStation = $"{HttpContext.Current.Request.UserHostName}; {HttpContext.Current.Request.UserHostAddress}";
            log.patientMRN = patientMrn;
            log.requestDomain = requestDomain;
            log.requestUrl = requestUrl;
            log.requestStatusCode = requestStatusCode;
            log.requestStatusMessage = requestStatusMessage;
            log.phiSource = phiSource;

            db.RequestLogs.Add(log);
            await db.SaveChangesAsync();

            db.Dispose();

            return log.helFhirLogId;
        }


        private async Task LogRequestDetail(int logId, string referenceUrl, string sourceNode, string sourceText)
        {
            AnaReportingEntities db = new AnaReportingEntities();

            RequestLogDetail detail = new RequestLogDetail();

            detail.helFhirLogId = logId;
            detail.referenceUrl = referenceUrl;
            detail.informationSourceNode = sourceNode;
            detail.informationSource = sourceText;

            db.RequestLogDetails.Add(detail);
            await db.SaveChangesAsync();

            db.Dispose();
        }


        protected override void Dispose(bool disposing)
        {
            _client.Dispose();
            base.Dispose(disposing);
        }
    }
}
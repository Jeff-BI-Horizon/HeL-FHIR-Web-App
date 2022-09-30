using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace HHS.Web.Hel_FHIR.API
{
    public class PersonController : ApiController
    {

        private Models.AnaReportingEntities _db = new Models.AnaReportingEntities();



        [HttpGet, Route("api/person/search")]
        public IEnumerable<Models.PersonSearch> Search(string query)
        {

            query = query.ToUpper();

            var people = from s in _db.People
                         where s.Person_Name_Full_Format.ToUpper().Contains(query)
                         orderby s.Person_Name_Full_Format
                         select new Models.PersonSearch
                         {
                             id = s.Person_MRN,
                             value = s.Person_Name_Last + ", " + s.Person_Name_First + " " + s.Person_Name_Middle,
                             dob = s.Person_Abs_Birth_Dt_Tm
                         };


            return people;
        }



        protected override void Dispose(bool disposing)
        {
            _db.Dispose();
            base.Dispose(disposing);
        }

    }
}
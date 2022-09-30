using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HHS.Web.Hel_FHIR.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            ViewBag.UserName = HttpContext.User.Identity.Name.ToString();
            return View();
        }
    }
}
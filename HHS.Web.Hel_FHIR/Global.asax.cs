using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HHS.Web.Hel_FHIR
{
    public class Global : System.Web.HttpApplication
    {



        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default",                                              // Route name
                "{controller}/{action}/{id}",                           // URL with parameters
                new { controller = "Home", action = "Index", id = "" }  // Parameter defaults
            );
        }


        public static void RegisterApi(HttpConfiguration config)
        {
            // TODO: Add any additional configuration code.


            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }


        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/scripts").Include(
                    "~/Scripts/jquery-{version}.js",
                    //"~/Scripts/jquery-ui-{version}.js",
                    "~/Scripts/moment.min.js",
                    "~/Scripts/bootstrap.min.js",
                    "~/Scripts/vue.min.js",
                    "~/Scripts/axios.min.js"
                ));


            bundles.Add(new StyleBundle("~/content/theme").Include(
                    //"~/Content/font-awesome.min.css",
                    "~/Content/bootstrap/bootstrap.min.css",
                    "~/Content/css/default.css"
                ));




        }

        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(RegisterApi);
            RegisterRoutes(RouteTable.Routes);
            RegisterBundles(BundleTable.Bundles);
        }
       

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}
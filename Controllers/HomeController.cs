using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVC_testApp.Models;

namespace MVC_testApp.Controllers
{
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC poooo!";

            var model2 = new UserInfo()
            {
               FirstName = "first",
               LastName  = "Last"
            };

            return View(model2);
        }

        public ActionResult About()
        {
            return View();
        }
    }
}

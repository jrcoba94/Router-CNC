using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Net;

namespace gocagestion.Controllers
{
    public class PrincipalController : Controller
    {
        public ActionResult Index(string parcookiename)
        {
            return View();
        }
    }
}
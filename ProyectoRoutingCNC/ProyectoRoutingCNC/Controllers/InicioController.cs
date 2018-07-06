using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProyectoRoutingCNC.Controllers
{
    public class InicioController : Controller
    {
        // GET: Inicio
        public ActionResult Index()
        {
            return View("Index");
        }

        public ActionResult Volver()
        {
            return View("Index");
        }
    }
}
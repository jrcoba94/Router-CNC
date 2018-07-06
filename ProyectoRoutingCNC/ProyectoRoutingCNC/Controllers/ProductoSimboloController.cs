using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Servicios.Model;
using Servicios.Servicios;
using PagedList;

namespace ProyectoRoutingCNC.Controllers
{
    public class ProductoSimboloController : Controller
    {
        #region Instancias y Variables
        SrvProductoSimbolo oSrvProductoSimbolo = new SrvProductoSimbolo();
        private RoutingCNCEntities db = new RoutingCNCEntities();
        public string UploadDirectory = "";
        #endregion

        // GET: ProductoSimbolo
        public ActionResult Index(int? page)
        {
            return View(db.ProductoSimbolo.ToList().ToPagedList(page ?? 1, 5));
        }
    }
}
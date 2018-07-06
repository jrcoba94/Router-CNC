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
    public class ProductoDibujosAnimadosController : Controller
    {
        #region Instancias y Variables
        SrvProductoDibujosAnimados oSrvProductoDibujosAnimados = new SrvProductoDibujosAnimados();
        private RoutingCNCEntities db = new RoutingCNCEntities();
        public string UploadDirectory = "";
        #endregion

        // GET: ProductoDibujosAnimados
        public ActionResult Index(int? page)
        {
            return View(db.ProductoDibujosAnimados.ToList().ToPagedList(page ?? 1, 5));
        }
    }
}
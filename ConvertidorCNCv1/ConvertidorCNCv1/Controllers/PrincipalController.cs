using ServicesCNC.Model;
using ServicesCNC.Servicios;
using System.Web.Mvc;

namespace ConvertidorCNCv1.Controllers
{
    public class PrincipalController : Controller
    {
        #region Instancias y Variables
        SrvProducto oSrvProducto = new SrvProducto();
        Contactos oContacto = new Contactos();
        #endregion

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Conocenos()
        {
            return View("Conocenos");
        }
        public ActionResult Contacto()
        {
            return View("Contacto");
        }
        public ActionResult Mision()
        {
            return View("Mision");
        }
        public ActionResult Productos()
        {
            var model = oSrvProducto.GetProducto();
            return View("Productos", model);
        }
        public ActionResult Vision()
        {
            return View("Vision");
        }
        public ActionResult Index1()
        {
            return View("");
        }
        public ActionResult Index2()
        {
            return View("");
        }
    }
}
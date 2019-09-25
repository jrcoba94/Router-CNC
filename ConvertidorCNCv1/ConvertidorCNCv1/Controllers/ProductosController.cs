using ConvertidorCNC.DOM;
using ServicesCNC.Model;
using ServicesCNC.Servicios;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace ConvertidorCNCv1.Controllers
{
    public class ProductosController : Controller
    {
        #region Instancias y Variables
        public string UploadDirectory = "~/Documentos/Imagenes/";
        private Object respuesta = new Object();
        SrvProducto oSrvProducto = new SrvProducto();
        SrvArchivos oSrvArchivos = new SrvArchivos();
        #endregion
        public ActionResult Index()
        {
            var model = SrCoreOD.GetCateorias();
            return View(model);
        }
        public ActionResult CreateProducto()
        {
            return PartialView("_CreateProducto");
        }
        public ActionResult EditProductos(int id)
        {
            var model = oSrvProducto._getCategoriaProductos(id);
            return PartialView("GridProductos", model);
        }
        public ActionResult GridProductos(int idCategoria)
        {
            var model = oSrvProducto._getCategoriaProductos(idCategoria);
            return PartialView("GridProductos", model);
        }
        #region Métodos POST
        [HttpPost]
        public JsonResult Create(_ProductoDOM item)
        {
            int st = 0, sa = 0;
            if (ModelState.IsValid)
            {
                try
                {
                    HttpPostedFileBase file = Request.Files["archivourl"];
                    if (file != null)
                    {
                        Productos oProducto = new Productos();
                        oProducto.nombre = item.nombre;
                        oProducto.descripcion = item.descripcion;
                        oProducto.categoriaID = item.Categorias.categoriaID;

                        st = oSrvProducto.AgregarProducto(oProducto);
                        if (st != 0)
                        {
                            string newPath = FileUpload(file);
                            if (newPath.Length > 0)
                            {
                                ProductoArchivos oArchivo = new ProductoArchivos();
                                oArchivo.productoID = st;
                                oArchivo.archivourl = newPath;
                                oArchivo.fecha = DateTime.Today;
                                oArchivo.tipoarchivo = "img";

                                sa = oSrvArchivos.AgregarArchivo(oArchivo);
                                if (sa != 0)
                                {
                                    respuesta = new { accion = true, msj = "Datos Guardados", Tipo = "Mensaje a Cliente" };
                                }
                                else
                                {
                                    respuesta = new { accion = true, msj = "Error de operación", Tipo = "Mensaje a Cliente" };
                                }
                            }
                            else
                            {
                                respuesta = new { accion = true, msj = "Error de operación", Tipo = "Mensaje a Cliente" };
                            }
                        }
                        else
                        {
                            respuesta = new { accion = true, msj = "Error de operación", Tipo = "Mensaje a Cliente" };
                        }
                    }
                    else
                    {
                        respuesta = new { accion = true, msj = "Error de operación", Tipo = "Mensaje a Cliente" };
                    }
                }
                catch (Exception ex)
                {
                    respuesta = new { accion = true, msj = ex.Message, Tipo = "Mensaje a Cliente" };
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return Json(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(respuesta), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region UploadFile
        public string FileUpload(HttpPostedFileBase file)
        {
            var directorio = UploadDirectory;
            string pathRandom = Path.GetRandomFileName().Replace("~/Documentos/Imagenes/", "");
            string resultFileName = pathRandom + '_' + file.FileName.Replace("~/Documentos/Imagenes/", "");
            string resultFileUrl = directorio + resultFileName;
            string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

            if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
            {
                file.SaveAs(resultFilePath);
            }
            return resultFileUrl;
        }
        #endregion
    }
}
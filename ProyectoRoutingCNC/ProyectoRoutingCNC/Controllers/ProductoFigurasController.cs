using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Servicios.Model;
using Servicios.Servicios;
using PagedList;
using System.Net;
using System.IO;
using System.Data.Entity;

namespace ProyectoRoutingCNC.Controllers
{
    public class ProductoFigurasController : Controller
    {
        #region Variables e Instancias

        SrvProductoFigurasGeometricas oSrvProductoFigurasGeometricas = new SrvProductoFigurasGeometricas();
        private RoutingCNCEntities db = new RoutingCNCEntities();
        public string UploadDirectory = "";

        #endregion

        // GET: ProductoFiguras
        public ActionResult Index(int? page)
        {
            try
            {
                ViewBag.MensajeExitoso = TempData["MensajeExitoso"].ToString();
            }
            catch
            {

            }

            try
            {
                ViewBag.MensajeActualizar = TempData["MensajeActualizar"].ToString();
            }
            catch
            {

            }

            try
            {
                ViewBag.MensajeEliminar = TempData["MensajeEliminar"].ToString();
            }
            catch
            {

            }

            return View(db.ProductoFigurasGeometricas.ToList().ToPagedList(page ?? 1, 30));
        }

        public ActionResult CrearFigura()
        {
            return View("Crear");
        }

        public ActionResult Detalles(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Find(id);
            if (oProductoFigurasGeometricas == null)
            {
                return HttpNotFound();
            }
            return View("Detalle", oProductoFigurasGeometricas);
        }

        #region Método que se encarga de dar de alta a un nuevo Producto

        // GET: Productos/Create
        public ActionResult Create()
        {
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor");
            return View("Crear");
        }

        // POST: Productos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductoFigurasGeometricas oProductoFigurasGeometricas, HttpPostedFileBase files)
        {
            try
            {


                if (ModelState.IsValid)
                {
                    try
                    {
                        UploadDirectory = ProyectoRoutingCNC.Properties.Settings.Default.DirectorioImagenes;
                        HttpPostedFileBase file = Request.Files["files"];
                        var directorio = UploadDirectory;
                        string pathRandom = Path.GetRandomFileName().Replace(/*'.', '-'*/"~/", "");
                        string resultFileName = pathRandom + '_' + file.FileName.Replace("~/", "");
                        string resultFileUrl = directorio + resultFileName;
                        string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

                        bool hasFile = false;
                        ImagenProducto oImgProduct = null;

                        if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                        {
                            file.SaveAs(resultFilePath);
                            oProductoFigurasGeometricas.ImagenPortadaFigura = /*servername +*/ resultFileUrl.Replace("~/", "");

                            oImgProduct = new ImagenProducto();
                            oImgProduct.Url = oProductoFigurasGeometricas.ImagenPortadaFigura.Replace("~/", "");

                            hasFile = true;
                        }

                        db.ProductoFigurasGeometricas.Add(oProductoFigurasGeometricas);
                        db.SaveChanges();


                        oImgProduct.ProductoID = oProductoFigurasGeometricas.ProductoFiguraID;
                        db.ImagenProducto.Add(oImgProduct);
                        db.SaveChanges();

                        //Mensaje que se imprime en un alert
                        TempData["MensajeExitoso"] = "El registro se agrego de manera exitosa.";
                        ViewBag.MensajeExitoso = TempData["MensajeExitoso"];

                        if (hasFile)
                        {
                            oImgProduct.ProductoID = oProductoFigurasGeometricas.ProductoFiguraID;
                            db.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(SrvMessages.getMessageSQL(ex));
                    }


                    //}

                    return RedirectToAction("Index");
                }
                //}
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }

            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View(oProductoFigurasGeometricas);

        }

        #endregion


        #region Método que se encarga de editar la información de un Producto

        // GET: Productos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Find(id);
            if (oProductoFigurasGeometricas == null)
            {
                return HttpNotFound();
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View("Editar", oProductoFigurasGeometricas);
        }

        // POST: Productos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductoFigurasGeometricas oProductoFigurasGeometricas)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    UploadDirectory = ProyectoRoutingCNC.Properties.Settings.Default.DirectorioImagenes;
                    HttpPostedFileBase file = Request.Files["files"];
                    var directorio = UploadDirectory;
                    string pathRandom = Path.GetRandomFileName().Replace(/*'.', '-'*/"~/", "");
                    string resultFileName = pathRandom + '_' + file.FileName.Replace("~/", "");
                    string resultFileUrl = directorio + resultFileName;
                    string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

                    bool hasFile = false;
                    ImagenProducto oImgProduct = null;
                    if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    {
                        file.SaveAs(resultFilePath);
                        oProductoFigurasGeometricas.ImagenPortadaFigura = /*servername +*/ resultFileUrl.Replace("~/", "");

                        oImgProduct = new ImagenProducto();
                        oImgProduct.Url = oProductoFigurasGeometricas.ImagenPortadaFigura.Replace("~/", "");
                        hasFile = true;
                    }
                    db.SaveChanges();

                    oImgProduct.ProductoID = oProductoFigurasGeometricas.ProductoFiguraID;
                    db.SaveChanges();

                    if (hasFile)
                    {
                        oImgProduct.ProductoID = oProductoFigurasGeometricas.ProductoFiguraID;
                        db.SaveChanges();
                    }

                    db.Entry(oProductoFigurasGeometricas).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["MensajeActualizar"] = "La información se actualizo correctamente.";
                    ViewBag.MensajeActualizar = TempData["MensajeActualizar"];

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View(oProductoFigurasGeometricas);
        }

        #endregion


        #region Método que se encarga de dar de baja a un registro

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Find(id);
            //if (oProductoFigurasGeometricas == null)
            //{
            //    return HttpNotFound();
            //}
            //return View("Eliminar", oProductoFigurasGeometricas);

            try
            {
                ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Find(id);
                db.ProductoFigurasGeometricas.Remove(oProductoFigurasGeometricas);
                oProductoFigurasGeometricas.Estatus = false;
                db.SaveChanges();
                TempData["MensajeEliminar"] = "El registro de elimino correctamente.";
                ViewBag.MensajeEliminar = TempData["MensajeEliminar"];
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                //TempData["MensajeEliminar"] = "No se pudo eliminar el registro.";
                //ViewBag.MensajeEliminar = TempData["MensajeEliminar"];
                throw new Exception(SrvMessages.getMessageSQL(ex));
                return RedirectToAction("Index");
            }
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {

            ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Find(id);
            db.ProductoFigurasGeometricas.Remove(oProductoFigurasGeometricas);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion
    }
}
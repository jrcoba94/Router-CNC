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

        public ActionResult CrearDibujo()
        {
            return View("Crear");
        }

        public ActionResult Regresar()
        {
            return View("Index");
        }

        public ActionResult Detalles(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoDibujosAnimados oProductoDibujosAnimados = db.ProductoDibujosAnimados.Find(id);
            if (oProductoDibujosAnimados == null)
            {
                return HttpNotFound();
            }
            return View("Detalle", oProductoDibujosAnimados);
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
        public ActionResult Create(ProductoDibujosAnimados oProductoDibujosAnimados, HttpPostedFileBase files)
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
                        oProductoDibujosAnimados.ImagenPortadaDibujo = /*servername +*/ resultFileUrl.Replace("~/", "");

                        oImgProduct = new ImagenProducto();
                        oImgProduct.Url = oProductoDibujosAnimados.ImagenPortadaDibujo.Replace("~/", "");

                        hasFile = true;
                    }

                    db.ProductoDibujosAnimados.Add(oProductoDibujosAnimados);
                    db.SaveChanges();

                    oImgProduct.ProductoID = oProductoDibujosAnimados.ProductoDibujosAnimadosID;
                    db.ImagenProducto.Add(oImgProduct);
                    db.SaveChanges();

                    if (hasFile)
                    {
                        oImgProduct.ProductoID = oProductoDibujosAnimados.ProductoDibujosAnimadosID;
                        db.SaveChanges();
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
            return View(oProductoDibujosAnimados);

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
            ProductoDibujosAnimados oProductoDibujosAnimados = db.ProductoDibujosAnimados.Find(id);
            if (oProductoDibujosAnimados == null)
            {
                return HttpNotFound();
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View("Editar", oProductoDibujosAnimados);
        }

        // POST: Productos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductoDibujosAnimados oProductoDibujosAnimados)
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
                        oProductoDibujosAnimados.ImagenPortadaDibujo = /*servername +*/ resultFileUrl.Replace("~/", "");

                        oImgProduct = new ImagenProducto();
                        oImgProduct.Url = oProductoDibujosAnimados.ImagenPortadaDibujo.Replace("~/", "");
                        hasFile = true;
                    }
                    db.SaveChanges();

                    oImgProduct.ProductoID = oProductoDibujosAnimados.ProductoDibujosAnimadosID;
                    db.SaveChanges();

                    if (hasFile)
                    {
                        oImgProduct.ProductoID = oProductoDibujosAnimados.ProductoDibujosAnimadosID;
                        db.SaveChanges();
                    }

                    db.Entry(oProductoDibujosAnimados).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View(oProductoDibujosAnimados);
        }

        #endregion


        #region Método que se encarga de dar de baja a un registro

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoDibujosAnimados oProductoDibujosAnimados = db.ProductoDibujosAnimados.Find(id);
            if (oProductoDibujosAnimados == null)
            {
                return HttpNotFound();
            }
            return View("Eliminar", oProductoDibujosAnimados);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {

            ProductoDibujosAnimados oProductoDibujosAnimados = db.ProductoDibujosAnimados.Find(id);
            db.ProductoDibujosAnimados.Remove(oProductoDibujosAnimados);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion
    }
}
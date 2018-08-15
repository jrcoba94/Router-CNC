﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Servicios.Model;
using Servicios.Servicios;
using System.Net;
using PagedList;
using System.Data.Entity;
using System.IO;

namespace ProyectoRoutingCNC.Controllers
{
    public class UsuariosController : Controller
    {
        #region Instancias y Variables

        SrvUsuario oSrvUsuario = new SrvUsuario();
        private RoutingCNCEntities db = new RoutingCNCEntities();
        public string UploadDirectory = "";

        #endregion
        
        // GET: Usuarios
        public ActionResult Index(int? page)
        {
            return View(db.Usuario.ToList().ToPagedList(page ?? 1, 5));
        }

        public ActionResult CrearUsuario()
        {
            return View("Crear");
        }

        public ActionResult Detalles(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Usuario oUsuario = db.Usuario.Find(id);
            if (oUsuario == null)
            {
                return HttpNotFound();
            }
            return View("Detalle", oUsuario);
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
        public ActionResult Create(Usuario oUsuario, HttpPostedFileBase files)
        {
            try
            {


                if (ModelState.IsValid)
                {

                    //UploadDirectory = ProyectoRoutingCNC.Properties.Settings.Default.DirectorioImagenes;
                    //HttpPostedFileBase file = Request.Files["files"];
                    //var directorio = UploadDirectory;
                    //string pathRandom = Path.GetRandomFileName().Replace(/*'.', '-'*/"~/", "");
                    //string resultFileName = pathRandom + '_' + file.FileName.Replace("~/", "");
                    //string resultFileUrl = directorio + resultFileName;
                    //string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

                    //bool hasFile = false;
                    //ImagenProducto oImgProduct = null;

                    //if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    //{
                    //    file.SaveAs(resultFilePath);
                    //    oUsuario.ImagenPortada = /*servername +*/ resultFileUrl.Replace("~/", "");

                    //    oImgProduct = new ImagenProducto();
                    //    oImgProduct.Url = oUsuario.ImagenPortada.Replace("~/", "");

                    //    hasFile = true;
                    //}

                    db.Usuario.Add(oUsuario);
                    db.SaveChanges();

                    //oImgProduct.ProductoID = producto.ProductoID;
                    //db.ImagenProducto.Add(oImgProduct);
                    //db.SaveChanges();

                    //if (hasFile)
                    //{
                    //    oImgProduct.ProductoID = producto.ProductoID;
                    //    db.SaveChanges();
                    //}


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
            return View(oUsuario);

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
            Usuario oUsuario = db.Usuario.Find(id);
            if (oUsuario == null)
            {
                return HttpNotFound();
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View("Editar", oUsuario);
        }

        // POST: Productos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Usuario oUsuario)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //UploadDirectory = ProyectoRoutingCNC.Properties.Settings.Default.DirectorioImagenes;
                    //HttpPostedFileBase file = Request.Files["files"];
                    //var directorio = UploadDirectory;
                    //string pathRandom = Path.GetRandomFileName().Replace(/*'.', '-'*/"~/", "");
                    //string resultFileName = pathRandom + '_' + file.FileName.Replace("~/", "");
                    //string resultFileUrl = directorio + resultFileName;
                    //string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

                    //bool hasFile = false;
                    //ImagenProducto oImgProduct = null;
                    //if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    //{
                    //    file.SaveAs(resultFilePath);
                    //    producto.ImagenPortada = /*servername +*/ resultFileUrl.Replace("~/", "");

                    //    oImgProduct = new ImagenProducto();
                    //    oImgProduct.Url = producto.ImagenPortada.Replace("~/", "");
                    //    hasFile = true;
                    //}
                    //db.SaveChanges();

                    //oImgProduct.ProductoID = producto.ProductoID;
                    //db.SaveChanges();

                    //if (hasFile)
                    //{
                    //    oImgProduct.ProductoID = producto.ProductoID;
                    //    db.SaveChanges();
                    //}

                    db.Entry(oUsuario).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View(oUsuario);
        }

        #endregion


        #region Método que se encarga de dar de baja a un registro

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Usuario oUsuario = db.Usuario.Find(id);
            if (oUsuario == null)
            {
                return HttpNotFound();
            }
            return View("Eliminar", oUsuario);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {

            Usuario oUsuario = db.Usuario.Find(id);
            db.Usuario.Remove(oUsuario);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Servicios.Model;
using Servicios.Servicios;
using System.ComponentModel.DataAnnotations;
using System.Web.Script.Serialization;

namespace ProyectoTelas.Controllers
{
    public class SitioController : Controller
    {
        #region Instancias y Variables
        //SrvProveedor oSrvProveedor = new SrvProveedor();
        SrvProducto oSrvProducto = new SrvProducto();
        #endregion

        // GET: Sitio
        public ActionResult Index()
        {
            return View("Index");
        }

        public ActionResult Nosotros()
        {
            return View("Nosotros");
        }

        public ActionResult Mision()
        {
            return View("Mision");
        }

        public ActionResult Vision()
        {
            return View("Vision");
        }

        public ActionResult Contactanos()
        {
            return View("Contacto");
        }

        public ActionResult Productos()
        {
            var model = oSrvProducto.GetProducto();
            return View("Productos", model);
        }

        //public ActionResult get_Productos()
        //{
        //    List<Producto> model;
        //    Object respuesta = new Object();
        //    try
        //    {
        //        model = oSrvProducto.GetProducto();
        //        respuesta = new { accion = true, msj = "", urlImage = model.Select(x => x.ImagenProducto), Tipo = "Mensaje a Cliente" };
        //    }
        //    catch (ValidationException ex)
        //    {
        //        respuesta = new { action = false, msj = "", urlImage = "", Tipo = "Mensaje a Cliente" };
        //    }
        //    return Json(new JavaScriptSerializer().Serialize(respuesta), JsonRequestBehavior.AllowGet);
        //}

        //[HttpPost]
        //public ActionResult EnviarCorreo(Contacto oContacto, string nombre, string correo, string comentario)
        //{
        //    var model = oSrvProveedor.getContacto();

        //    if (ModelState.IsValid)
        //    {
        //        using (Entities db = new Entities())
        //        {
        //            oContacto.CorreoElectronico = correo;
        //            db.Contacto.Add(oContacto);
        //            db.SaveChanges();
        //            return RedirectToAction("Index");
        //        }
        //    }

        //    return View();
        //}

        [HttpGet]
        public ActionResult Contacto()
        {
            return View();
        }

        //[HttpPost]
        //public ActionResult Contacto(string receiverEmail, string subject, string message, Contacto oContacto, string correo)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            var senderemail = new MailAddress("iamfastherz@gmail.com", "Demo");
        //            var receiveremail = new MailAddress(receiverEmail, "Recibido");

        //            var password = "Champion12345";
        //            var sub = subject;
        //            var body = message;

        //            var smtp = new SmtpClient
        //            {
        //                Host = "smtp.gmail.com",
        //                Port = 587,
        //                EnableSsl = true,
        //                DeliveryMethod = SmtpDeliveryMethod.Network,
        //                UseDefaultCredentials = false,
        //                Credentials = new NetworkCredential(senderemail.Address, password)

        //            };

        //            using (var mess = new MailMessage(senderemail, receiveremail)
        //            {
        //                Subject = subject,
        //                Body = body
        //            })
        //            {
        //                smtp.Send(mess);
        //            }

        //            //using (Entities db = new Entities())
        //            //{
        //            //    oContacto.CorreoElectronico = correo;
        //            //    db.Contacto.Add(oContacto);
        //            //    db.SaveChanges();
        //            //    return RedirectToAction("Index");
        //            //}
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error : " + ex.Message);
        //    }
        //    return View();
        //}
    }
}
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
        Contacto oContacto = new Contacto();
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
        //public ActionResult EnviarCorreo(string nombre, string asunto, string mensaje, HttpPostedFileBase fichero)
        //{
        //    try
        //    {
        //        MailMessage correo = new MailMessage();
        //        correo.From = new MailAddress("proyectoroutingcnc@gmail.com", "CNC");
        //        correo.To.Add(nombre);
        //        correo.Subject = asunto;
        //        correo.Body = mensaje;
        //        correo.IsBodyHtml = true;
        //        correo.Priority = MailPriority.Normal;

        //        //Agregadas
        //        correo.SubjectEncoding = System.Text.Encoding.UTF8;
        //        correo.BodyEncoding = System.Text.Encoding.UTF8;



        //        //Se almacena los archivos adjuntos en una carpeta creada en el proyecto temporal
        //        string ruta = Server.MapPath("../Temporal");
        //        fichero.SaveAs(ruta + "\\" + fichero.FileName);

        //        Attachment adjunto = new Attachment(ruta + "\\" + fichero.FileName);
        //        correo.Attachments.Add(adjunto);

        //        //Configuración del servidor smtp
        //        SmtpClient smtp = new SmtpClient();
        //        smtp.Host = "smtp.gmail.com";
        //        smtp.Port = 25;
        //        smtp.EnableSsl = true;
        //        smtp.UseDefaultCredentials = true;
        //        string CuentaCorreo = "proyectoroutingcnc@gmail.com";
        //        string ContraseñaCorreo = "Proyecto123";
        //        smtp.Credentials = new System.Net.NetworkCredential(CuentaCorreo, ContraseñaCorreo);

        //        smtp.Send(correo);
        //        ViewBag.Mensaje = "¡Gracias por contactarnos, su mensaje se envio correctamente!";

        //        //using (RoutingCNCEntities db = new RoutingCNCEntities())
        //        //{                                       
        //        //oContacto.CorreoElectronico = correo;
        //        //db.Contacto.Add(oContacto);
        //        //db.SaveChanges();
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(SrvMessages.getMessageSQL(ex));
        //    }
        //    return View();
        //}

        //[HttpPost]
        //public ActionResult Contacto(string receiverEmail, string subject, string message, HttpPostedFileBase fichero)
        //{
        //    try
        //    {
        //        MailMessage co = new MailMessage();
        //        co.From = new MailAddress(receiverEmail);
        //        co.To.Add("proyectoroutingcnc@gmail.com");
        //        co.Subject = subject;
        //        co.SubjectEncoding = System.Text.Encoding.UTF8;
        //        co.Body = message;
        //        co.BodyEncoding = System.Text.Encoding.UTF8;
        //        co.IsBodyHtml = true;
        //        co.Priority = MailPriority.Normal;

        //        string ruta = Server.MapPath("~/Temporal");
        //        fichero.SaveAs(ruta + "\\" + fichero.FileName);

        //        Attachment adjunto = new Attachment(ruta + "\\" + fichero.FileName);
        //        co.Attachments.Add(adjunto);

        //        SmtpClient smtp = new SmtpClient();
        //        smtp.Host = "smtp.gmail.com";
        //        smtp.Port = 25;
        //        smtp.EnableSsl = true;
        //        smtp.UseDefaultCredentials = true;
        //        smtp.Credentials = new NetworkCredential("proyectoroutingcnc@gmail.com", "RoutingCNC-2018");
        //        smtp.Send(co);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error : " + ex.Message);
        //    }
        //    return View();
        //}

        [HttpPost]
        public ActionResult Contacto(string receiverEmail, string subject, string message, HttpPostedFileBase fichero)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //Nuevo
                    MailMessage MensajeCorreo = new MailMessage();

                    var senderemail = new MailAddress("proyectoroutingcnc@gmail.com", "CNC");
                    var receiveremail = new MailAddress(receiverEmail, "Recibido");

                    var password = "Proyecto123";
                    var sub = subject;
                    var body = message;

                    string ruta = Server.MapPath("~/Temporal");
                    fichero.SaveAs(ruta + "\\" + fichero.FileName);

                    Attachment adjunto = new Attachment(ruta + "\\" + fichero.FileName);
                    MensajeCorreo.Attachments.Add(adjunto);


                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(senderemail.Address, password)

                    };

                    using (var mess = new MailMessage(senderemail, receiveremail)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(mess);
                    }

                    //using (RoutingCNCEntities db = new RoutingCNCEntities())
                    //{

                        //oContacto.CorreoElectronico = correo;
                        //db.Contacto.Add(oContacto);
                        //db.SaveChanges();
                        //return RedirectToAction("Index");
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }
            return View();
        }
    }
}
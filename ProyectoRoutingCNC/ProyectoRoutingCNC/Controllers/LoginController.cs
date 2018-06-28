using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Servicios.Model;
using Servicios.Servicios;
using Servicios.DTO;

namespace ProyectoRoutingCNC.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        public ActionResult Index()
        {
            return View("Login");
        }

        public ActionResult PanelAdministrativo()
        {
            return View("Panel");
        }

        [HttpPost]
        public ActionResult Accesar(ModelLogin model)
        {
            try
            {
                if (model.usuario == null)
                {
                    ViewBag.Msj = "Nombre de usuario y/o contraseña inválidos";
                }
                else if (model.clave == null)
                {
                    ViewBag.MsjContraseña = "Contraseña inválida";
                }
                else if (model.clave != null && model.usuario != null)
                {
                    SrvLogin srv = new SrvLogin();
                    VisaUsuario visa = srv.DoLogin(model);

                    if (this.IniciarSesion(visa))
                    {
                        return RedireccionaPrincipal(visa);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("Error: " + ex.Message);
            }

            return View("Login");
        }

        public bool IniciarSesion(Servicios.DTO.VisaUsuario Session)
        {
            if (Session == null) return false;
            setCookie(Session);
            System.Web.HttpContext.Current.Session["SesionActiva"] = Session;
            return true;
        }
        public ActionResult RedireccionaLogin()
        {
            return RedirectToAction("Index", "Login");
        }
        public ActionResult RedireccionaPrincipal(VisaUsuario Session)
        {
            if (Session != null && Session.usuario != null)
            {
                if (Session.usuario.NombreUsuario == "Admin")
                {
                    return RedirectToAction("Index", "Sitio");
                }                   
            }
            else
            {
                return RedirectToAction("Index", "Inicio");
            }
            return RedireccionaLogin();
        }

        public HttpCookie getCookie()
        {
            HttpCookie Cookie = System.Web.HttpContext.Current.Request.Cookies["_CookiedeSession"];
            if (Cookie != null)
            {
                return Cookie;
            }
            else
            {
                return null;
            }
        }
        public void setCookie(Servicios.DTO.VisaUsuario sesion)
        {
            HttpCookie Cookie = new HttpCookie("_CookiedeSession");
            Cookie.Values.Add("Nombre", sesion.usuario.NombreUsuario);
            Cookie.Expires = DateTime.Now.AddHours(12);
            System.Web.HttpContext.Current.Response.Cookies.Add(Cookie);
        }
        public void deleteCookie()
        {
            HttpCookie Cookie = new HttpCookie("_CookiedeSession");
            Cookie.Expires = DateTime.Now.AddDays(-1d);
            Response.Cookies.Add(Cookie);
        }

        public ActionResult Salir()
        {
            dataSession.usuario = null;
            return RedirectToAction("Index", "Login");
        }

        public VisaUsuario dataSession { get { return (VisaUsuario)System.Web.HttpContext.Current.Session["SesionActiva"]; } }
    }
}
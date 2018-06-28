using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Servicios.DTO;
using System.Security.Cryptography;
using System.IO;

namespace APP.Core
{
    public class CoreController : Controller
    {
        public int IdCargaResponsable;
        public string Usuario;
        public string Rol;
        public const int Error = 1;
        public const int SinError = 0;
        public HttpCookie CookiedeSession = null;
        //public string Layout
        //{
        //    get
        //    {
        //        if (this.dataSession == null)
        //        {
        //            return @"~/Views/Shared/_Layout.cshtml";
        //        }
        //        if (this.dataSession.usuario.seg_roles.rol == "Transferencias")
        //        {
        //            return @"~/Views/Shared/_LayoutMasterCorporativo.cshtml";
        //        }
        //        if (this.dataSession.usuario.seg_roles.rol == "Administrador Sistema")
        //        {
        //            return @"~/Views/Shared/_LayoutSys.cshtml";
        //        }
        //        else
        //        {
        //            return @"~/Views/Shared/_Layout.cshtml";
        //        }
        //    }
        //}
        public VisaUsuario dataSession { get { return (VisaUsuario)System.Web.HttpContext.Current.Session["SesionActiva"]; } }
        public static string MenuActivo
        {
            set
            {
                VisaUsuario visa = (VisaUsuario)System.Web.HttpContext.Current.Session["SesionActiva"];
                visa.menuactual = value;
                System.Web.HttpContext.Current.Session["SesionActiva"] = visa;
            }
            get
            {
                VisaUsuario visa = (VisaUsuario)System.Web.HttpContext.Current.Session["SesionActiva"];
                return visa.menuactual;
            }
        }
        
        public CoreController()
        {
            CookiedeSession = getCookie();
            if (CookiedeSession != null)
            {
                Usuario = CookiedeSession["Nombre"] != null ? CookiedeSession["Nombre"].ToString() : "";
                Rol = CookiedeSession["Rol"].ToString();
            }

        }
        public void MessagesToGrids(int errores)
        {
            if (errores > 0)
            {
                ViewData["EditResult"] = "Se procesó la información, sin embargo 1 o más registros no se insertaron, de clic en aceptar para ver errores";
            }
            else
                ViewData["EditResult"] = "Se hizo la actualización completa de los registros";
        }
        public void MessagesToGridInstalacion(int errores, int Refrescar)
        {
            if (errores > 0)
            {
                ViewData["EditResult"] = "Se procesó la información, sin embargo 1 o más registros no se insertaron, de clic en aceptar para ver errores";
            }
            else
            {
                if (Refrescar == 1)
                    ViewData["EditResult"] = "Se hizo la actualización completa de los registros, se refrescará su pantalla al cerrar esta ventana";
                else
                    ViewData["EditResult"] = "Se hizo la actualización completa de los registros";

            }
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
                    return RedirectToAction("Index", "Inicio");
                }
            }
            else
            {
                return RedirectToAction("Index", "Principal");
            }
            return RedireccionaLogin();
        }
        //public ActionResult RedireccionaCaja(VisaUsuario Session)
        //{
        //    if (Session != null && Session.usuario != null)
        //    {
        //        return RedirectToAction("Index", "Principal");
        //    }
        //    else
        //    {
        //        return RedireccionaLogin();
        //    }
        //}
        public ActionResult Salir()
        {
            dataSession.usuario = null;
            if (Request.Cookies["CookiedeSession"] != null)
            {
                HttpCookie myCookie = new HttpCookie("CookiedeSession");
                myCookie.Expires = DateTime.Now.AddDays(-1d);
                Response.Cookies.Add(myCookie);

                return RedirectToAction("Index", "Login");
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }

        }
        public static string GestorErrores(ModelStateDictionary ModelState, string Mensaje = "", bool MostrarDetalle = true)
        {
            string cad = "";
            string def = "Corrija, Todos los errores:";
            string detalle = ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage).Aggregate((a, b) => a + ", " + b).ToString();
            if (!string.IsNullOrWhiteSpace(Mensaje)) def = Mensaje;
            cad = def + Mensaje + detalle;
            return cad;
        }
    }
}
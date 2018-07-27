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
        public ActionResult Accesar(UsuariosPanel model)
        {
            if (model.NombreUsuarioPanel == null)
            {
                ViewBag.Msj = "Complete el campo Nombre de Usuario";
                return View("Login");
            }

            if (model.Clave == null)
            {
                ViewBag.Msj = "Complete el campo Contraseña";
                return View("Login");
            }

            if (model.NombreUsuarioPanel != null && model.Clave != null)
            {
                return getLogin(model);
            }
            else
            {
                ViewBag.Msj = "NombreDeUsuario/Contraseña Incorrectos";
                return View("Login");
            }
        }

        #region Método para confirmar validación
        public ActionResult getLogin(UsuariosPanel model)
        {
            SrvLogin oSrvLogin = new SrvLogin();
            EntUsuario entSession = new EntUsuario();
            UsuariosPanel sesion = oSrvLogin.IsLogin(model);
            if (sesion != null)
            {
                HttpCookie InicioSesion = new HttpCookie("InicioSesion");
                InicioSesion.Value = "1";
                System.Web.HttpContext.Current.Response.Cookies.Add(InicioSesion);
                if (sesion.NombreUsuarioPanel == model.NombreUsuarioPanel && sesion.Clave == model.Clave)
                {
                    if (ProyectoRoutingCNC.Controllers.LoginController.IniciarSesion(entSession, sesion))
                    {
                        if (entSession == null)
                        {
                            return RedirectToAction("Index", "Login");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Panel");
                        }
                    }
                }
                else
                {
                    ViewBag.Msj = "No fue posible iniciar sesion con las credenciales proporcionadas";
                }
                return View("Login");
            }
            else
            {
                ViewBag.Msj = "No fue posible iniciar sesion con las credenciales proporcionadas";
                return View("Login");
            }
        }
        #endregion

        public static bool IniciarSesion(EntUsuario entSession, Servicios.Model.UsuariosPanel sesion)
        {
            CrearCookie(sesion);
            entSession.UserPanID = sesion.UsuariosPanelID;
            entSession.NombreUsuarioPanel = sesion.NombreUsuarioPanel;
            entSession.ApellidoPaterno = sesion.ApellidoPaterno;
            entSession.ApellidoMaterno = sesion.ApellidoMaterno;
            entSession.CorreoEletronico = sesion.CorreoElectronico;
            //entSession.Nombre = sesion.NOMBRE;
            entSession.Estatus = sesion.Estatus;

            EntSession ss = new EntSession();
            ss.Usuario = entSession;
            System.Web.HttpContext.Current.Session["SesionActiva"] = ss;
            return true;
        }

        #region Método para Crear Cookie

        public static void CrearCookie(Servicios.Model.UsuariosPanel sesion)
        {
            HttpCookie myCookie = new HttpCookie("myCookie");

            myCookie.Values.Add("UsuarioPanelID", sesion.UsuariosPanelID.ToString());
            myCookie.Values.Add("NombreUsuarioPanel", sesion.NombreUsuarioPanel.ToString());
            //myCookie.Values.Add("NOMBREUSUARIO", sesion.NOMBREUSUARIO.ToString());

            //set cookie expiry date-time. Made it to last for next 12 hours.
            myCookie.Expires = DateTime.Now.AddHours(12);

            //Most important, write the cookie to client.
            System.Web.HttpContext.Current.Response.Cookies.Add(myCookie);
        }

        #endregion
    }

    //[HttpPost]
    //public ActionResult Accesar(ModelLogin model)
    //{
    //    try
    //    {
    //        if (model.usuario == null)
    //        {
    //            ViewBag.Msj = "Nombre de usuario y/o contraseña inválidos";
    //        }
    //        else if (model.clave == null)
    //        {
    //            ViewBag.MsjContraseña = "Contraseña inválida";
    //        }
    //        else if (model.clave != null && model.usuario != null)
    //        {
    //            SrvLogin srv = new SrvLogin();
    //            VisaUsuario visa = srv.DoLogin(model);

    //            if (this.IniciarSesion(visa))
    //            {
    //                return RedireccionaPrincipal(visa);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception(SrvMessages.getMessageSQL(ex));
    //    }

    //    return View("Login");
    //}

    //public bool IniciarSesion(Servicios.DTO.VisaUsuario Session)
    //{
    //    if (Session == null) return false;
    //    setCookie(Session);
    //    System.Web.HttpContext.Current.Session["SesionActiva"] = Session;
    //    return true;
    //}
    //public ActionResult RedireccionaLogin()
    //{
    //    return RedirectToAction("Index", "Login");
    //}
    //public ActionResult RedireccionaPrincipal(VisaUsuario Session)
    //{
    //    try
    //    {
    //        if (Session != null && Session.usuario != null)
    //        {
    //            if (Session.usuario.NombreUsuarioPanel == "Admin")
    //            {
    //                return RedirectToAction("Index", "Sitio");
    //            }
    //        }
    //        else
    //        {
    //            return RedirectToAction("Index", "Inicio");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception(SrvMessages.getMessageSQL(ex));
    //    }
    //    return RedireccionaLogin();
    //}

    //public HttpCookie getCookie()
    //{
    //    HttpCookie Cookie = System.Web.HttpContext.Current.Request.Cookies["_CookiedeSession"];
    //    if (Cookie != null)
    //    {
    //        return Cookie;
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}
    //public void setCookie(Servicios.DTO.VisaUsuario sesion)
    //{
    //    HttpCookie Cookie = new HttpCookie("_CookiedeSession");
    //    Cookie.Values.Add("Nombre", sesion.usuario.NombreUsuarioPanel);
    //    Cookie.Expires = DateTime.Now.AddHours(12);
    //    System.Web.HttpContext.Current.Response.Cookies.Add(Cookie);
    //}
    //public void deleteCookie()
    //{
    //    HttpCookie Cookie = new HttpCookie("_CookiedeSession");
    //    Cookie.Expires = DateTime.Now.AddDays(-1d);
    //    Response.Cookies.Add(Cookie);
    //}

    //public ActionResult Salir()
    //{
    //    dataSession.usuario = null;
    //    return RedirectToAction("Index", "Login");
    //}

    //public VisaUsuario dataSession { get { return (VisaUsuario)System.Web.HttpContext.Current.Session["SesionActiva"]; } }
}

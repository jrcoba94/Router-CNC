using ServicesCNC.DTO;
using ServicesCNC.Model;
using ServicesCNC.Servicios;
using System;
using System.Web;
using System.Web.Mvc;

namespace ConvertidorCNCv1.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            return View("Index");
        }

        [HttpPost]
        public ActionResult Accesar(Usuarios model)
        {
            if (model.usuario == null)
            {
                ViewBag.Msj = "Complete el campo Nombre de Usuario";
                return View("Index");
            }

            if (model.contrasenia == null)
            {
                ViewBag.Msj = "Complete el campo Contraseña";
                return View("Index");
            }

            if (model.usuario != null && model.contrasenia != null)
            {
                return getLogin(model);
            }
            else
            {
                ViewBag.Msj = "NombreDeUsuario/Contraseña Incorrectos";
                return View("Index");
            }
        }

        #region Método para confirmar validación
        public ActionResult getLogin(Usuarios model)
        {
            SrvLogin oSrvLogin = new SrvLogin();
            EntUsuario entSession = new EntUsuario();
            Usuarios sesion = oSrvLogin.IsLogin(model);
            if (sesion != null)
            {
                HttpCookie InicioSesion = new HttpCookie("InicioSesion");
                InicioSesion.Value = "1";
                System.Web.HttpContext.Current.Response.Cookies.Add(InicioSesion);
                if (sesion.usuario.ToLower() == model.usuario.ToLower() && sesion.contrasenia == model.contrasenia)
                {
                    if (IniciarSesion(entSession, sesion))
                    {
                        if (entSession == null)
                        {
                            return RedirectToAction("Index", "Login");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Administrador");
                        }
                    }
                }
                else
                {
                    ViewBag.Msj = "No fue posible iniciar sesion por favor intente de nuevo";
                }
                return View("Index");
            }
            else
            {
                ViewBag.Msj = "No fue posible iniciar sesion por favor intente de nuevo";
                return View("Index");
            }
        }
        #endregion

        public static bool IniciarSesion(EntUsuario entSession, Usuarios sesion)
        {
            CrearCookie(sesion);
            entSession.UsuarioID = sesion.usuarioID;
            entSession.Usuario = sesion.usuario;
            entSession.Nombre = sesion.nombre;
            entSession.Apellidos = sesion.apellidos;
            entSession.CorreoEletronico = sesion.correoelectronico;
            entSession.Estatus = sesion.estatus;

            EntSession ss = new EntSession();
            ss.Usuario = entSession;
            System.Web.HttpContext.Current.Session["SesionActiva"] = ss;
            return true;
        }

        #region Método para Crear Cookie
        public static void CrearCookie(Usuarios sesion)
        {
            HttpCookie myCookie = new HttpCookie("myCookie");

            myCookie.Values.Add("UsuarioID", sesion.usuarioID.ToString());
            myCookie.Values.Add("Usuario", sesion.usuario.ToString());
            myCookie.Expires = DateTime.Now.AddHours(12);
            System.Web.HttpContext.Current.Response.Cookies.Add(myCookie);
        }
        #endregion
    }
}
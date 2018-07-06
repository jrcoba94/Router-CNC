using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;
using Servicios.DTO;
using Servicios.Enum;
using System.Data.Entity.Validation;

namespace Servicios.Servicios
{
    public class SrvLogin
    {
        public UsuariosPanel IsLogin(UsuariosPanel usuario)
        {
            UsuariosPanel us = new UsuariosPanel();
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    if (usuario.NombreUsuarioPanel != null)
                    {
                        us = db.UsuariosPanel.FirstOrDefault(x => x.NombreUsuarioPanel.ToLower() == usuario.NombreUsuarioPanel.ToLower());
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
            }
            return us;
        }


        //public string Genera(string clave)
        //{
        //    return setPassword(clave);
        //}
        //public VisaUsuario DoLogin(ModelLogin log)
        //{
        //    VisaUsuario sesion = null;
        //    try
        //    {
        //        using (RoutingCNCEntities db = new RoutingCNCEntities())
        //        {
        //            UsuariosPanel us = new UsuariosPanel();
        //            if (log.IsValid())
        //            {

        //                us = db.UsuariosPanel.First(x => x.NombreUsuarioPanel == log.usuario.ToLower() && x.Estatus == (int)EnumEstados.Activo);
        //                if (us != null)
        //                {
        //                    if (chkPass(us.Clave, log.clave))
        //                    {
        //                        sesion = new VisaUsuario();
        //                        sesion.usuario = us;
        //                        return sesion;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.Write(ex.Message);
        //    }
        //    return sesion;
        //}

        ////Función que regresa true o false si las contraseñan coninciden, convirtiendo una de ellas codificadas
        //private bool chkPass(string passG, string passS)
        //{
        //    return string.Equals(passG, setPassword(passS));
        //}

        ////Función que regresa true o false si las contraseñan coninciden, convirtiendo una de ellas codificadas
        //private bool chkPass2(string passG, string passS)
        //{
        //    return string.Equals(passG, passS);
        //}

        ////Codificar string para contraseña
        //private string setPassword(string pass)
        //{
        //    return Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(pass)));
        //}

        //public string SetNewPass(VisaUsuario model)
        //{
        //    string PassReturn = string.Empty;
        //    string NPass = "RT" + DateTime.Now.Millisecond + DateTime.Now.Second + DateTime.Now.Year + "$";

        //    PassReturn = NPass;
        //    NPass = setPassword(NPass);
        //    UpdatePass(model.usuario.Clave, NPass);

        //    return PassReturn;
        //}

        //private void UpdatePass(string Email, string NPass)
        //{
        //    using (RoutingCNCEntities db = new RoutingCNCEntities())
        //    {
        //        var Usuarios = db;
        //        var e = db.UsuariosPanel.Single(x => x.Clave == Email);

        //        if (e != null)
        //        {
        //            e.Clave = NPass;

        //            try
        //            {
        //                db.SaveChanges();
        //            }
        //            catch (DbEntityValidationException exp)
        //            {
        //                foreach (var eve in exp.EntityValidationErrors)
        //                {
        //                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
        //                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
        //                    foreach (var ve in eve.ValidationErrors)
        //                    {
        //                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
        //                            ve.PropertyName, ve.ErrorMessage);
        //                    }
        //                }
        //                throw;
        //            }
        //        }
        //    }
        //}
    }
}

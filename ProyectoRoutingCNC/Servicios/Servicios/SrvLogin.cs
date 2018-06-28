using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;
using Servicios.DTO;

namespace Servicios.Servicios
{
    public class SrvLogin
    {
        public string Genera(string clave)
        {
            return setPassword(clave);
        }
        public VisaUsuario DoLogin(ModelLogin log)
        {
            VisaUsuario sesion = null;
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    Usuario us = new Usuario();
                    if (log.IsValid())
                    {

                        us = db.Usuario.First(x => x.NombreUsuario == log.usuario.ToLower() && x.Estatus == true);
                        if (us != null)
                        {
                            if (chkPass(us.Contrasenia, log.clave))
                            {
                                sesion = new VisaUsuario();
                                sesion.usuario = us;
                                return sesion;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
            }
            return sesion;
        }

        //Función que regresa true o false si las contraseñan coninciden, convirtiendo una de ellas codificadas
        private bool chkPass(string passG, string passS)
        {
            return string.Equals(passG, setPassword(passS));
        }

        //Función que regresa true o false si las contraseñan coninciden, convirtiendo una de ellas codificadas
        private bool chkPass2(string passG, string passS)
        {
            return string.Equals(passG, passS);
        }

        //Codificar string para contraseña
        private string setPassword(string pass)
        {
            return Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(pass)));
        }
    }
}

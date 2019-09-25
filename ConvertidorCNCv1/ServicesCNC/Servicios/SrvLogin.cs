using System;
using System.Linq;
using ServicesCNC.Model;
using System.Diagnostics;

namespace ServicesCNC.Servicios
{
    public class SrvLogin
    {
        public Usuarios IsLogin(Usuarios usuario)
        {
            Usuarios us = new Usuarios();
            try
            {
                using (EntitiesCNC db = new EntitiesCNC())
                {
                    if (usuario.usuario != null)
                    {
                        us = db.Usuarios.FirstOrDefault(x => x.usuario.ToLower() == usuario.usuario.ToLower());
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
            }
            return us;
        }
    }
}

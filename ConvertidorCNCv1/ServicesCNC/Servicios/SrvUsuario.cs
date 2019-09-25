using System;
using System.Linq;
using ServicesCNC.Model;
using System.Collections.Generic;

namespace ServicesCNC.Servicios
{
    public class SrvUsuario
    {
        public List<Usuarios> GetUsuario()
        {
            List<Usuarios> oListUsuario = new List<Usuarios>();
            try
            {
                using (EntitiesCNC db = new EntitiesCNC())
                {
                    oListUsuario = db.Usuarios.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListUsuario;
        }
        
        public void AgregarUsuario(Usuarios item)
        {
            try
            {
                using (EntitiesCNC db = new EntitiesCNC())
                {
                    db.Usuarios.Add(item);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        public void ActualizarUsuario(Usuarios item)
        {
            try
            {
                using (EntitiesCNC db = new EntitiesCNC())
                {
                    Usuarios oUsuario = db.Usuarios.Where(x => x.usuarioID == item.usuarioID).FirstOrDefault();
                    if (oUsuario != null)
                    {
                        oUsuario.usuario = item.usuario;
                        oUsuario.contrasenia = item.contrasenia;
                        oUsuario.nombre = item.nombre;
                        oUsuario.apellidos = item.apellidos;
                        oUsuario.rolID = item.rolID;
                        oUsuario.estatus = item.estatus;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        public void EliminarUsuario(int id)
        {
            try
            {
                using (EntitiesCNC db = new EntitiesCNC())
                {
                    Usuarios oUsuario = db.Usuarios.Where(x => x.usuarioID == id).FirstOrDefault();
                    oUsuario.estatus = false;
                    //oUsuario.Estatus = 0;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }
    }
}

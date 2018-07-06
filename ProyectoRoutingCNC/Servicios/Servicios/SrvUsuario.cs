using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;

namespace Servicios.Servicios
{
    public class SrvUsuario
    {
        #region Método que consulta la tabla usuario

        public List<Usuario> GetUsuario()
        {
            List<Usuario> oListUsuario = new List<Usuario>();
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    oListUsuario = db.Usuario.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListUsuario;
        }

        #endregion

        #region Método que permite dar de alta a un nuevo usuario

        public void AgregarUsuario(Usuario item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    db.Usuario.Add(item);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        #endregion

        #region Método que permite actualizar la información de un usuario

        public void ActualizarUsuario(Usuario item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    Usuario oUsuario = db.Usuario.Where(x => x.UsuarioID == item.UsuarioID).FirstOrDefault();
                    if (oUsuario != null)
                    {
                        oUsuario.NombreUsuario = item.NombreUsuario;
                        oUsuario.Contrasenia = item.Contrasenia;
                        oUsuario.Estatus = item.Estatus;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        #endregion

        #region Método que permite eliminar la información de un usuario

        public void EliminarUsuario(int id)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    Usuario oUsuario = db.Usuario.Where(x => x.UsuarioID == id).FirstOrDefault();
                    oUsuario.Estatus = false;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        #endregion
    }
}

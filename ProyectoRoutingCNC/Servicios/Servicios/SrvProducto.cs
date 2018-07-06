using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;

namespace Servicios.Servicios
{
    public class SrvProducto
    {
        #region Método que consulta la tabla Producto

        public List<Producto> GetProducto()
        {
            List<Producto> oListProducto = new List<Producto>();
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    oListProducto = db.Producto.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListProducto;
        }

        #endregion

        #region Método que permite dar de alta a un nuevo producto

        public void AgregarProducto(Producto item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    db.Producto.Add(item);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        #endregion

        #region Método que permite actualizar la información de un producto

        public void ActualizarProducto(Producto item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    Producto oProducto = db.Producto.Where(x => x.ProductoID == item.ProductoID).FirstOrDefault();
                    if (oProducto != null)
                    {
                        oProducto.Nombre = item.Nombre;
                        oProducto.Descripcion = item.Descripcion;
                        oProducto.Caracteristicas = item.Caracteristicas;
                        oProducto.ImagenPortada = item.ImagenPortada;
                        oProducto.Estatus = item.Estatus;
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

        #region Método que permite dar de baja a un producto del catálogo

        public void EliminarProducto(int id)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    Producto oProducto = db.Producto.Where(x => x.ProductoID == id).FirstOrDefault();
                    oProducto.Estatus = false;
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

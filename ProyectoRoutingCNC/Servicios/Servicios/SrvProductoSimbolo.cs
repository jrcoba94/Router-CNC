using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;

namespace Servicios.Servicios
{
    public class SrvProductoSimbolo
    {
        #region Método que permite consultar la lista de la tabla ProductoSimbolo

        public List<ProductoSimbolo> GetListProductoSimbolo()
        {
            List<ProductoSimbolo> oListProductoSimbolo = new List<ProductoSimbolo>();
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    oListProductoSimbolo = db.ProductoSimbolo.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListProductoSimbolo;
        }

        #endregion

        #region Método que permite dar de alta a un nuevo Producto de simbolo 

        public void AgregarProductoSimbolo(ProductoSimbolo item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    ProductoSimbolo oProductoSimbolo = db.ProductoSimbolo.Where(x => x.ProductoSimboloID == item.ProductoSimboloID).FirstOrDefault();
                    if (oProductoSimbolo != null)
                    {
                        oProductoSimbolo.NombreSimbolo = item.NombreSimbolo;
                        oProductoSimbolo.DescripcionSimbolo = item.DescripcionSimbolo;
                        oProductoSimbolo.CaracteristicasSimbolo = item.CaracteristicasSimbolo;
                        oProductoSimbolo.ImagenPortadaSimbolo = item.ImagenPortadaSimbolo;
                        oProductoSimbolo.Estatus = item.Estatus;
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

        public void EliminarProductoSimbolo(int id)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    ProductoSimbolo oProductoSimbolo = db.ProductoSimbolo.Where(x => x.ProductoSimboloID == id).FirstOrDefault();
                    oProductoSimbolo.Estatus = false;
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

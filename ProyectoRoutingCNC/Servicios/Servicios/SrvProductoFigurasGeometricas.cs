using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;

namespace Servicios.Servicios
{
    public class SrvProductoFigurasGeometricas
    {
        #region Método que consulta la tabla ProductoFiguras

        public List<ProductoFigurasGeometricas> GetListFigurasGeometricas()
        {
            List<ProductoFigurasGeometricas> oListProductoFigurasGeometricas = new List<ProductoFigurasGeometricas>();
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    oListProductoFigurasGeometricas = db.ProductoFigurasGeometricas.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListProductoFigurasGeometricas;
        }

        #endregion

        #region Método que permite dar de alta a una nueva figuera geométrica

        public void AgregarFiguraGeometrica(ProductoFigurasGeometricas item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    db.ProductoFigurasGeometricas.Add(item);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        #endregion

        #region Método que permite modificar la información de una figuera geométrica

        public void ActualizarFiguraGeometrica(ProductoFigurasGeometricas item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Where(x => x.ProductoFiguraID == item.ProductoFiguraID).FirstOrDefault();
                    if (oProductoFigurasGeometricas != null)
                    {
                        oProductoFigurasGeometricas.NombreFigura = item.NombreFigura;
                        oProductoFigurasGeometricas.DescripcionFigura = item.DescripcionFigura;
                        oProductoFigurasGeometricas.CaracteristicasFigura = item.CaracteristicasFigura;
                        oProductoFigurasGeometricas.ImagenPortadaFigura = item.ImagenPortadaFigura;
                        oProductoFigurasGeometricas.Estatus = item.Estatus;
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

        #region Método que permite eliminar la información de una figura geométrica

        public void EliminarFigura(int id)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    ProductoFigurasGeometricas oProductoFigurasGeometricas = db.ProductoFigurasGeometricas.Where(x => x.ProductoFiguraID == id).FirstOrDefault();
                    oProductoFigurasGeometricas.Estatus = false;
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

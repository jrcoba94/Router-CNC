using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Model;

namespace Servicios.Servicios
{
    public class SrvProductoDibujosAnimados
    {
        #region Método que consulta la tabla ProductoDibujosAnimados

        public List<ProductoDibujosAnimados> GetListProductosAnimados()
        {
            List<ProductoDibujosAnimados> oListProductosAnimados = new List<ProductoDibujosAnimados>();
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    oListProductosAnimados = db.ProductoDibujosAnimados.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListProductosAnimados;
        }

        #endregion

        #region Método que permite dar de alta a un nuevo Dibujo Animado

        public void AgregarProductoAnimado(ProductoDibujosAnimados item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    db.ProductoDibujosAnimados.Add(item);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }

        #endregion

        #region Método que permite actualizar la información de un ProductoAnimado

        public void ModificarProductoAnimado(ProductoDibujosAnimados item)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    ProductoDibujosAnimados oProductoDibujoAnimado = db.ProductoDibujosAnimados
                        .Where(x => x.ProductoDibujosAnimadosID == item.ProductoDibujosAnimadosID).FirstOrDefault();
                    if (oProductoDibujoAnimado != null)
                    {
                        oProductoDibujoAnimado.NombreDibujo = item.NombreDibujo;
                        oProductoDibujoAnimado.DescripcionDibujo = item.DescripcionDibujo;
                        oProductoDibujoAnimado.CaracteristicasDibujo = item.CaracteristicasDibujo;
                        oProductoDibujoAnimado.ImagenPortadaDibujo = item.ImagenPortadaDibujo;
                        oProductoDibujoAnimado.Estatus = item.Estatus;
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

        #region Método que permite dar de baja a un dibujo animado

        public void EliminarProductoAnimado(int id)
        {
            try
            {
                using (RoutingCNCEntities db = new RoutingCNCEntities())
                {
                    ProductoDibujosAnimados oProductoDibujosAnimados = db.ProductoDibujosAnimados
                        .Where(x => x.ProductoDibujosAnimadosID == id).FirstOrDefault();
                    oProductoDibujosAnimados.Estatus = false;
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

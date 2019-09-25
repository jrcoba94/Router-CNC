using System;
using System.Linq;
using ServicesCNC.Model;
using System.Collections.Generic;

namespace ServicesCNC.Servicios
{
    public class SrvArchivos
    {
        private EntitiesCNC db;
        public List<ProductoArchivos> _getArchivoProducto(int id)
        {
            List<ProductoArchivos> lst = new List<ProductoArchivos>();
            try
            {
                using (db = new EntitiesCNC())
                {
                    lst = db.ProductoArchivos.Include("Productos").Where(x => x.productoID == id).ToList();
                }
            }
            catch (Exception ex)
            {
                string ms = ex.Message;
            }
            return lst;
        }
        public int AgregarArchivo(ProductoArchivos item)
        {
            int st = 0;
            try
            {
                using (db = new EntitiesCNC())
                {
                    db.ProductoArchivos.Add(item);
                    db.SaveChanges();
                    st = item.archivoID;
                }
            }
            catch (Exception ex)
            {
                return st;
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return st;
        }

        public void ActualizarArchivo(ProductoArchivos item)
        {
            try
            {
                using (db = new EntitiesCNC())
                {
                    ProductoArchivos obj = db.ProductoArchivos.Where(x => x.archivoID == item.archivoID).FirstOrDefault();
                    if (obj != null)
                    {
                        obj.archivourl = item.archivourl;
                        obj.fecha = item.fecha;
                        obj.tipoarchivo = item.tipoarchivo;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }
    }
}

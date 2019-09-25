using System;
using System.Linq;
using ServicesCNC.Model;
using System.Collections.Generic;

namespace ServicesCNC.Servicios
{
    public class SrvProducto
    {
        private EntitiesCNC db;
        public List<ProductoArchivos> _getCategoriaProductos(int id)
        {
            List<ProductoArchivos> lst = new List<ProductoArchivos>();
            try
            {
                using (db = new EntitiesCNC())
                {
                    if(id != 0)
                    {
                        lst = db.ProductoArchivos.Include("Productos").Where(x => x.Productos.categoriaID == id).ToList();
                    }
                    else
                    {
                        lst = db.ProductoArchivos.Include("Productos").Where(x => x.Productos.categoriaID == 0).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                string ms = ex.Message;
            }
            return lst;
        }
        public List<Productos> GetProducto()
        {
            List<Productos> oListProducto = new List<Productos>();
            try
            {
                using (db = new EntitiesCNC())
                {
                    oListProducto = db.Productos.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return oListProducto;
        }
        
        public int AgregarProducto(Productos item)
        {
            int st = 0;
            try
            {
                using (db = new EntitiesCNC())
                {
                    db.Productos.Add(item);
                    db.SaveChanges();
                    st = item.productoID;
                }
            }
            catch (Exception ex)
            {
                return st;
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            return st;
        }
        
        public void ActualizarProducto(Productos item)
        {
            try
            {
                using (db = new EntitiesCNC())
                {
                    Productos obj = db.Productos.Where(x => x.productoID == item.productoID).FirstOrDefault();
                    if (obj != null)
                    {
                        obj.nombre = item.nombre;
                        obj.descripcion = item.descripcion;
                        obj.categoriaID = item.categoriaID;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
        }
        
        public void EliminarProducto(int id)
        {
            try
            {
                using (db = new EntitiesCNC())
                {
                    Productos oProducto = db.Productos.Where(x => x.productoID == id).FirstOrDefault();
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

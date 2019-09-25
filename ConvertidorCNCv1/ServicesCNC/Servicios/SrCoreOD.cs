using System;
using System.Linq;
using ServicesCNC.Model;
using System.Collections.Generic;

namespace ServicesCNC.Servicios
{
    public static class SrCoreOD
    {
        private static EntitiesCNC db;
        public static List<Categorias> GetCateorias()
        {
            List<Categorias> lst = new List<Categorias>();
            try
            {
                using(db = new EntitiesCNC())
                {
                    lst = db.Categorias.ToList();
                }
            }
            catch (Exception ex)
            {
                string ms = ex.Message;
            }
            return lst;
        }

        public static string Get_Categoria(int id)
        {
            string lst = "";
            try
            {
                using (db = new EntitiesCNC())
                {
                    if (id != 0)
                    {
                        lst = db.Categorias.Where(x => x.categoriaID == id).Select(x => x.nombre).First();
                    }
                    else
                    {
                        lst = "Otro";
                    }
                }
            }
            catch (Exception ex)
            {
                string ms = ex.Message;
            }
            return lst;
        }
    }
}

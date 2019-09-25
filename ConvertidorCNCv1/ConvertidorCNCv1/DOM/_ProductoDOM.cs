using System;

namespace ConvertidorCNC.DOM
{
    public class _ProductoDOM
    {
        public int productoID { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public Nullable<int> categoriaID { get; set; }
        public string archivourl { get; set; }
        public virtual _CategoriaDOM Categorias { get; set; }
    }
    public class _CategoriaDOM
    {
        public int categoriaID { get; set; }
        public string nombre { get; set; }
        public bool estatus { get; set; }
        public string stick { get; set; }
    }
}
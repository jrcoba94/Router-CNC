using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.DTO
{
    public class EntUsuario
    {
        public int UserPanID
        {
            get;
            set;
        }

        public string NombreUsuarioPanel
        {
            get;
            set;
        }

        public string ApellidoPaterno
        {
            get;
            set;
        }

        public string ApellidoMaterno
        {
            get;
            set;
        }

        public string CorreoEletronico
        {
            get;
            set;
        }

        public string Clave
        {
            get;
            set;
        }

        public short Estatus
        {
            get;
            set;
        }
    }
}

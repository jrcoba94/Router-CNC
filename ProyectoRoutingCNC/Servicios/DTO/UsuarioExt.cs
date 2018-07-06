using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Model
{
    using System;
    using System.Collections.Generic;

    public partial class Usuario
    {
        public string _NombreCompleto
        {
            get
            {
                return this.NombreUsuario + "" + this.ApellidoPaterno + "" + ApellidoMaterno;
            }
        }
    }
}

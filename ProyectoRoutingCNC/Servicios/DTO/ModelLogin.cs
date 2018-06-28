using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.DTO
{
    public class ModelLogin
    {
        public string usuario { get; set; }
        public string clave { get; set; }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(this.usuario)) return false;
            if (string.IsNullOrWhiteSpace(this.clave)) return false;
            return true;
        }
    }
}

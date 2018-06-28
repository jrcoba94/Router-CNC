using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Servicios.Servicios
{
    public abstract class SrvMessages
    {
        public static string getMessageSQL(Exception e)
        {
            string msj = "";
            if (e.InnerException != null)
            {
                SqlException em = (SqlException)e.InnerException.InnerException;
                switch (em.Number)
                {
                    case 1061:
                    case 1062:
                        //msj = "Registro Duplicado, intente de nuevo por favor.";
                        msj = "Duplicate registration, try again please.";
                        break;
                    case 547:
                        {
                            msj = "This record can not be updated because there are dependent records.";
                            //msj = "No se puede actualizar este registro porque hay registros dependientes.";
                            break;
                        }
                    default:
                        msj = e.Message;
                        break;
                }
            }
            else
            {
                msj = e.Message;
            }

            return msj;


        }
    }
}

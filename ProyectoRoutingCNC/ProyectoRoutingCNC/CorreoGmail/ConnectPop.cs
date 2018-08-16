using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Servicios.Model;
using OpenPop.Mime;
using OpenPop.Pop3;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Servicios.Servicios;

namespace ProyectoRoutingCNC.CorreoGmail
{
    public class ConnectPop
    {
        //usuario/mail de gmail
        private string username = "proyectoroutingcnc@gmail.com";
        //password
        private string password = "Proyecto123";
        //el puerto para pop de gmail es el 995
        private int port = 995;
        //el host de pop de gmail es pop.gmail.com
        private string hostname = "pop.gmail.com";
        //esta opción debe ir en true
        private bool useSsl = true;

        public List<Message> getMensajes()
        {
            try
            {

                // El cliente se desconecta al terminar el using
                using (Pop3Client client = new Pop3Client())
                {
                    // conectamos al servidor
                    client.Connect(hostname, port, useSsl);

                    // Autentificación
                    client.Authenticate(username, password);

                    // Obtenemos los Uids mensajes
                    List<string> uids = client.GetMessageUids();

                    // creamos instancia de mensajes
                    List<Message> lstMessages = new List<Message>();

                    // Recorremos para comparar
                    for (int i = 0; i < uids.Count; i++)
                    {
                        //obtenemos el uid actual, es él id del mensaje
                        string currentUidOnServer = uids[i];

                        //por medio del uid obtenemos el mensaje con el siguiente metodo
                        Message oMessage = client.GetMessage(i + 1);

                        //agregamos el mensaje a la lista que regresa el metodo
                        lstMessages.Add(oMessage);

                    }

                    // regresamos la lista
                    return lstMessages;
                }
            }

            catch (Exception ex)
            {
                //si ocurre una excepción regresamos null, es importante que cachen las excepciones, yo
                //lo hice general por modo de ejemplo
                throw new Exception(SrvMessages.getMessageSQL(ex));
                //return null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Comunicacion
{
    public class NetworkDataHelper
    {

        private readonly Socket _socket;

        public NetworkDataHelper(Socket socket)
        {
            _socket = socket;
        }

        public void Send(byte[] data) 
        {
            int offset = 0;
            int size = data.Length;

            while (offset < size) 
            {

                int enviados = _socket.Send(data, offset, size - offset, SocketFlags.None);
                if (enviados == 0) 
                {
                    throw new SocketException();
                
                }
                offset += enviados;
            
            }
        
        }

        public byte[] Receive(int largo) 
        {
            byte[] data = new byte[largo];
            int offset = 0;
            int size = largo;

            while (offset < size)
            {

                int recibidos = _socket.Receive(data, offset, size - offset, SocketFlags.None);
                if (recibidos == 0)
                {
                    throw new SocketException();

                }
                offset += recibidos;

            }

            return data;


        }

    }
}

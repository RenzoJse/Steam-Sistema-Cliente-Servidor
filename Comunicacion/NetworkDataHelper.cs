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

        private readonly TcpClient _tcpClient;

        public NetworkDataHelper(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public async Task Send(byte[] data)
        {
            var stream = _tcpClient.GetStream();
            var offset = 0;
            while (offset < data.Length)
            {
                await stream.WriteAsync(data.AsMemory(offset, data.Length - offset));
                offset += data.Length - offset;
            }
        }

        public async Task<byte[]> Receive(int length)
        {
            var stream = _tcpClient.GetStream();
            var data = new byte[length];
            var offset = 0;

            while (offset < length)
            {
                var received = await stream.ReadAsync(data.AsMemory(offset, length - offset));
                if (received == 0)
                    throw new Exception("Connection lost");
                offset += received;
            }
            return data;
        }
    }
}

using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;

namespace UnoServer
{
    public static class Utils {

        public static int BUFFER_SIZE = 1024;

        /* Install newtonsoft.json package to Visual Studio using NuGet Package Manager then add the following code:
         * ClassName ObjectName = JsonConvert.DeserializeObject < ClassName > (jsonObject);
         */

        public static String Serialize(Game gameToSerialize) {
            return JsonConvert.SerializeObject(gameToSerialize);
        }

        public static Game Deserialize(String message) {
            Console.WriteLine(message);
            Console.WriteLine();
            return JsonConvert.DeserializeObject<Game>(message);
        }

        /* 
         * Los sockets tienen un tamaño máximo de bytes que pueden enviar de una vez.
         * 
         * En este programa, se ha definido que cada paquete que envie el socket sea de 1024.
         * 
         * Al principio de cada mensaje se añade un header de 4 bytes que indica el número de bytes que
         * se espera recibir. La longitud del mensaje. 
         * 
         * La longitud del mensaje puede no ser multiplo de 1024, por lo que se rellena con 0 hasta 
         * completar el paquete.
         * 
         *   https://blog.stephencleary.com/2009/04/message-framing.html
         */
        public static byte[] WrapMessage(byte[] message) {
            // 1. Send the length of the upcoming message. Is always a 4 byte array.
            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);

            // 2. We read blocks of 1024 so all individual msg shall be 1024 or multiple
            int currentMsgLength = lengthPrefix.Length + message.Length;
            int bytesTo1024 = BUFFER_SIZE - (currentMsgLength % BUFFER_SIZE);
            int totalLength = currentMsgLength + bytesTo1024;

            // 3. Add as prefix the message length
            byte[] messageWithPrefix = new byte[totalLength];
            lengthPrefix.CopyTo(messageWithPrefix, 0);
            message.CopyTo(messageWithPrefix, lengthPrefix.Length);
            return messageWithPrefix;
        }

        /* First 4 bytes of socket message must have the length of the message */
        public static int ComputeMessageBytesLength(byte[] message) {
            return BitConverter.ToInt32(message, 0);
        }

        /*
         * Se determina un tamaño de paquete a enviar: 1024 bytes.
         * Cada lectura será de 1024 bytes, se rellena con 0 los bytes que falten hasta llegar a 1024.
         * 
         * Un mensaje está compuesto por paquetes de 1024. Los primeros 4 bytes del mensaje (primer paquete)
         * indican la longitud en bytes del mensaje completo 
         * (no de los bloques, es decir, se puede esperar 128 bytes pero se recibirán 1024)
         * 
         * El mensaje final no debe incluir el header con los 4 bytes que indican el tamaño del mensaje,
         * por ello se empieza a escribir en el memoryStream a partir del byte 4 del primer paquete.
         * 
         */
        public static byte[] ReceiveMessage(Socket socketClient, byte[] buffer) {
            buffer = new byte[BUFFER_SIZE];
            MemoryStream memoryStream = new MemoryStream();
            int bytesReceived = socketClient.Receive(buffer);
            int expectedBytes = ComputeMessageBytesLength(buffer);
            int remainingBytes = Math.Max(0, expectedBytes - (bytesReceived - 4));
            memoryStream.Write(buffer, 4, bytesReceived - 4);
            while (remainingBytes > 0) {
                bytesReceived = socketClient.Receive(buffer);
                memoryStream.Write(buffer, 0, Math.Min(bytesReceived, remainingBytes));
                remainingBytes -= bytesReceived;
            }
            var message = memoryStream.ToArray();
            memoryStream.Close();
            return message;
        }
    }
}

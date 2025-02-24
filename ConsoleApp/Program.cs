using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SerialPort sp = new SerialPort();
            sp.PortName = "COM10";
            sp.Open();

            byte[] buffer = new byte[1024];
            using (StreamWriter sw = new StreamWriter("log.txt"))
            {
                do
                {
                    int readed = sp.Read(buffer, 0, buffer.Length);
                    string hexString = ByteArrayToHexString(buffer, 0, readed);
                    sw.WriteLine(hexString);
                    sw.Flush();
                    Console.WriteLine(hexString);
                } while (true);
            }
        }

        public static string ByteArrayToHexString(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            char[] hexChars = new char[count * 2];
            for (int i = 0; i < count; i++)
            {
                byte b = buffer[offset + i];
                hexChars[i * 2] = GetHexChar(b >> 4);
                hexChars[i * 2 + 1] = GetHexChar(b & 0xF);
            }
            return new string(hexChars);
        }

        private static char GetHexChar(int value)
        {
            return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
        }
    }
}

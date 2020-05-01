using System;
using System.IO;
using System.Collections.Generic;

namespace Random_Bytes
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine("Use: exe hexCount hexLength output");
                return;
            }

            Random r = new Random();
            int hexCount = int.Parse(args[0]);
            int hexLength = int.Parse(args[1]);

            string[] hexList = new string[hexCount];

            for(int i = 0; i < hexCount; ++i)
            {
                byte[] hexString = new byte[hexLength];
                r.NextBytes(hexString);
                hexList[i] = BitConverter.ToString(hexString).Replace("-", String.Empty).ToLower();
            }

            File.WriteAllLines(args[2], hexList);
        }
    }
}

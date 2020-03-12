using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WMOReader_2
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                string path = args[0];
                byte[] buffer = File.ReadAllBytes(path);
                if (path.Contains(".wmo"))
                {

                    Reader Reader = new Reader();
                    Reader.lectureWMO(path, buffer);
                }

                Console.ReadLine();
            }
        }
    }
}

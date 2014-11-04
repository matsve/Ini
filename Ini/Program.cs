using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Ini;

namespace Ini
{
    static class Program
    {
        static void Main(string[] args)
        {
            var ini = new System.Data.Ini.Ini();
            Console.WriteLine("Welcome to the Ini test program!");
            Console.ReadKey();
        }
    }
}

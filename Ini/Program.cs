﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Ini;

namespace Ini
{
    /// <summary>
    /// This is the sample program that shows how to use the ini system
    /// </summary>
    static class Program
    {
        static void Main(string[] args)
        {
            var ini = new System.Data.Ini.Ini();
            Console.WriteLine("Welcome to the Ini test program!");
            ini.SetString("main", "asd", "asg");
            ini.SetInt("main", "prop", 12);
            ini.SetFloat("main", "propf", 213.1f);
            ini.SetBool("ab", "bval", false);
            ini.SetBool("ab", "bvalt", true);

            ini.SetString("loop#1", "key", "value1");
            ini.SetString("loop#2", "key", "value2");
            ini.SetString("loop#3", "key", "value3");
            ini.SetString("loop#4", "key", "value4");

            Console.Write("Loop[");
            foreach (var section in ini.GetIterativeSections("loop"))
            {
                Console.Write(section.Get("key") + ", ");

                // Simple, manual "deserialization" example
                var test = new
                {
                    Key = section.Get("key")
                };
            }
            Console.WriteLine("]");

            ini.Report();
            ini.SaveAs("sample.ini");
            Console.ReadKey();
        }
    }
}

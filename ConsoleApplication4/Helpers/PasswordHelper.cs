using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.Helpers
{
    class PasswordHelper
    {
        public static string inputPass()
        {
            string pass = "";
            bool protectPass = true;
            while (protectPass)
            {
                char s = Console.ReadKey(true).KeyChar;
                if (s == '\r')
                {
                    protectPass = false;
                    Console.WriteLine();
                }
                else if (s == '\b' && pass.Length > 0)
                {
                    Console.CursorLeft -= 1;
                    Console.Write(' ');
                    Console.CursorLeft -= 1;
                    pass = pass.Substring(0, pass.Length - 1);
                }
                else
                {
                    pass = pass + s.ToString();
                    Console.Write("*");
                }

            }
            return pass;
        }
    }
}

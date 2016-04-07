using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreetCallouts
{
    internal static class Common
    {
        public static Random myRand;

        static Common()
        {
            Common.myRand = new Random();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ExploringPing {
    class Program {

        static void Main(string[] args) {

            var portsToPing = new List<int> { 56736, 56787, 56813 };
            ApiAwaiter.AwaitApis(portsToPing);
            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }

    }
}

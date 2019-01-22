using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace EDennis.AspNetCore.ApiLauncher {
    public static class ApiAwaiter {

        public static void AwaitApis(List<int> ports) {

            Task.WhenAll(ports.Select(p => PingAsync(p))).Wait();
            foreach (var port in ports) {
                Console.WriteLine($"Successfully pinged port {port}");
            }

        }

        private static async Task PingAsync(int port) {
            Console.WriteLine($"Trying to ping port {port}");
            await Task.Run(() => {

                var request = WebRequest.Create($"http://localhost:{port}/ping");

                var statusCode = HttpStatusCode.Processing;
                var sw = new Stopwatch();
                sw.Start();

                while (statusCode != HttpStatusCode.OK && sw.ElapsedMilliseconds < 20000) {
                    using (var response = request.GetResponse()) {
                        statusCode = ((HttpWebResponse)response).StatusCode;
                        if (statusCode != HttpStatusCode.OK)
                            Thread.Sleep(1000);
                    }
                }

                sw.Stop();
            });
        }
    }
}

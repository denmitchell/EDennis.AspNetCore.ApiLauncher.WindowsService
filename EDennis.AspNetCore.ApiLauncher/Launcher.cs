using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EDennis.AspNetCore.ApiLauncher {
    /// <summary>
    /// This class is a testing tool -- used to launch 
    /// one or more Api dependencies when in Development.  
    /// </summary>
    public class Launcher : IDisposable {

        private static ILogger _logger;
        private readonly IConfiguration _config;
        private readonly string _defaultRepoDir;
        private readonly int _startingPort;
        private readonly DotNetProcessTerminator _terminator;

        public Launcher(IConfiguration config, ILogger<Launcher> logger, DotNetProcessTerminator terminator) {
            _config = config;
            _defaultRepoDir = config["DefaultRepoDirectory"];
            _startingPort = int.Parse(config["StartingPort"]);
            _logger = logger;
            _terminator = terminator;
        }

        //holds references to all launched APIs
        public List<HaveApi> LaunchedApis
            = new List<HaveApi>();

        //used to synchronize access to the Console's title bar.
        //private static object _lockObj = new object();


        /// <summary>
        /// Starts all APIs referenced in configuration
        /// </summary>
        /// <param name="config">Configuration holding data for APIs</param>
        public void StartApis(List<NeedApi> needApis) {

            _logger.LogInformation("Starting Apis");
            var ports = PortInspector.GetAvailablePorts(_startingPort, needApis.Count);

            _logger.LogInformation($"AvailablePorts:{JToken.FromObject(ports).ToString(Newtonsoft.Json.Formatting.None)}");

            //get the API data from the configuration
            //iterate over all API data
            for (int i=0; i<needApis.Count; i++) {
                if (LaunchedApis.Where(a => a.ProjectName == needApis[i].ProjectName && a.LaunchProfile == needApis[i].LaunchProfile).Count()>0)
                    break;
                StartApi(needApis[i],ports[i]);
            }

            ApiAwaiter.AwaitApis(ports);

        }


        /// <summary>
        /// Starts the API in a new thread
        /// </summary>
        /// <param name="api">The Api to start</param>
        private void StartApi(NeedApi api, int port) {

            _logger.LogInformation($"Starting Api: {api.ProjectName} @ {port} ");

            //if LaunchProfile has been set, create dotnet run param for it.
            var launchProfileArg = "--no-launch-profile";
            if (api.LaunchProfile != null)
                launchProfileArg = $"--launch-profile {api.LaunchProfile}";            

            //configure a background process for running dotnet,
            //ensuring that the port is set appropriately and
            //that all console output is to the same console
            var info = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = $"/c dotnet run {launchProfileArg} --no-build --server.urls http://localhost:{port}",
                //RedirectStandardInput = true,
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                //UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = api.LocalProjectDirectory
            };

            //call the dotnet run command asynchronously
            Task.Run(() => {
                Process p = new Process();
                p.StartInfo = info;
                //p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                //p.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                try {
                    _logger.LogInformation($"trying to start {api.ProjectName} @ {port}");
                    p.Start();
                } catch(Exception ex) {
                    _logger.LogInformation($"EXCEPTION: {ex.Message}");
                }
                //p.BeginOutputReadLine();
                //p.BeginErrorReadLine();

                _logger.LogInformation($"Starting {api.ProjectName} @ {port}");
                //update the console title to add the launched API
                //lock (_lockObj) {
                //    Console.Title += $", {GetLastSegment(api.ProjectName)}{port}";
                //}

                var haveApi = new HaveApi(_defaultRepoDir) {
                     ProjectName = api.ProjectName,
                     SolutionName = api.SolutionName,
                     Port = port
                };

                //add the launched Api to the dictionary of running APIs
                haveApi.Process = p;
                LaunchedApis.Add(haveApi);

                //wait for the process to be suspended.
                p.WaitForExit();
            });
        }

        /// <summary>
        /// Gets the last segment in a network/folder path
        /// </summary>
        /// <param name="path">the source path</param>
        /// <returns>the last segment of the source path</returns>
        private static string GetLastSegment(string path) {
            int index = path.LastIndexOf("\\");
            if (index == -1)
                return null;
            var proj = path.Substring(index + 1);
            index = proj.LastIndexOf(".");
            if (index == -1)
                return proj;
            else
                return proj.Substring(index + 1);
        }

        /// <summary>
        /// Writes output to the console
        /// </summary>
        /// <param name="sendingProcess">the process sending the data</param>
        /// <param name="args">the data to write to the console</param>
        static void OutputHandler(object sendingProcess, DataReceivedEventArgs args) {
            _logger.LogInformation(args.Data);
        }

        /// <summary>
        /// Stop a specific API
        /// </summary>
        public void StopApi(HaveApi api) {
            //stop the server
            api.Process.Close();
            _logger.LogInformation($"Stopping {api.ProjectName} @ {api.Port}");
            //remove the server from the dictionary of running servers
            LaunchedApis.Remove(api);
        }


        /// <summary>
        /// Stops all running APIs
        /// </summary>
        public void StopApis() {
            var ports = LaunchedApis.Select(a => a.Port).ToArray();
            //iterate over all API Startup classes
            var cnt = LaunchedApis.Count;
            for (int i = 0; i < cnt; i++) {
                var api = LaunchedApis[0];
                _logger.LogInformation($"Stopping {api.ProjectName} @ {api.Port}");
                StopApi(api);
            }
            _terminator.KillDotNetProcesses(ports);
        }



        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    StopApis(); //stop all APIs upon disposal of this class
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            Dispose(true);
        }
        #endregion

    }
}

using Newtonsoft.Json;
using System.Diagnostics;

namespace EDennis.AspNetCore.ApiLauncher {
    public class HaveApi : NeedApi {

        public int Port { get; set; }

        [JsonIgnore]
        public Process Process { get; set; }

        public HaveApi(string defaultRepoDir) :
            base(defaultRepoDir) { }

    }
}

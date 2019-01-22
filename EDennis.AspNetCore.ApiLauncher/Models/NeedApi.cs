using System;

namespace EDennis.AspNetCore.ApiLauncher {
    public class NeedApi {

        // the path to ... source/repos
        readonly string _repoDir;

        /// <summary>
        /// Construct a new Api object and initialize
        /// the _repoDir variable, taking into consideration
        /// the user profile.
        /// </summary>
        public NeedApi(string defaultRepoDir) {
            if (defaultRepoDir == null)
                _repoDir = $"C:\\Users\\{Environment.UserName}\\source\\repos\\";
            else
                _repoDir = defaultRepoDir;
        }

        public string ProjectName { get; set; }
        public string LaunchProfile { get; set; }
        public string SolutionName { get; set; }

        //c:\Users\{USER_PROFILE}\sources\repos\{SolutionName}\{ProjectName}
        public string FullProjectPath{ get; set; }

        //the path to the project on the development computer
        public string LocalProjectDirectory {
            get {
                if (FullProjectPath == null)
                    return $"{_repoDir}{SolutionName}\\{ProjectName}";
                else
                    return FullProjectPath;
            }
        }

    }
}

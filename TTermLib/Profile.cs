using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TTerm
{
    public static class ProfileExtensions
    {
        public static Profile ExpandVariables(this Profile profile)
        {
            var envHelper = new EnvironmentVariableHelper();
            var env = envHelper.GetUser();

            var profileEnv = profile.EnvironmentVariables;
            if (profileEnv != null)
            {
                envHelper.ExpandVariables(env, profileEnv);
            }

            return new Profile()
            {
                Command = envHelper.ExpandVariables(profile.Command, env),
                CurrentWorkingDirectory = envHelper.ExpandVariables(profile.CurrentWorkingDirectory, env),
                Arguments = profile.Arguments?.Select(x => envHelper.ExpandVariables(x, env)).ToArray(),
                EnvironmentVariables = env
            };
        }


    }

    public class Profile
    {
        [JsonProperty(PropertyName = "command")]
        public string Command { get; set; }

        [JsonProperty(PropertyName = "args")]
        public string[] Arguments { get; set; }

        [JsonProperty(PropertyName = "cwd")]
        public string CurrentWorkingDirectory { get; set; }

        [JsonProperty(PropertyName = "env")]
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        public static Profile CreateDefaultProfile()
        {
            return DefaultProfile.Get();
        }
    }

    internal static class DefaultProfile
    {


        public static Profile Get()
        {
            var profile = new Profile()
            {
                Command = GetDefaultCommand(),
                CurrentWorkingDirectory = GetDefaultWorkingDirectory()
            };
            return profile;
        }

        private static string GetDefaultCommand()
        {
            string result = null;
            if (Environment.GetEnvironmentVariable(EnvironmentVariables.COMSPEC) != null)
            {
                result = "%COMSPEC%";
            }
            else
            {
                result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            }
            return result;
        }

        private static string GetDefaultWorkingDirectory()
        {
            string result;
            if (Environment.GetEnvironmentVariable(EnvironmentVariables.HOMEDRIVE) != null &&
                Environment.GetEnvironmentVariable(EnvironmentVariables.HOMEPATH) != null)
            {
                result = string.Format("%{0}%%{1}%", EnvironmentVariables.HOMEDRIVE, EnvironmentVariables.HOMEPATH);
            }
            else
            {
                result = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return result;
        }
    }
}

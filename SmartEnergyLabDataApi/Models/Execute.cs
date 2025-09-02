using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.ObjectPool;

namespace SmartEnergyLabDataApi.Models
{
	public class Execute
	{
		public int Run(string exe, string args, string? workingDir=null, Dictionary<string,string>? envVariables = null)
		{
			ProcessStartInfo oInfo = new ProcessStartInfo(exe, args);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;
			if (workingDir != null) {
				oInfo.WorkingDirectory = workingDir;
			} else {
				oInfo.WorkingDirectory = GetWorkingDirectory();
			}

			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

            if ( envVariables!=null ) {
                foreach( var ev in envVariables) {
                    oInfo.EnvironmentVariables[ev.Key] = ev.Value;
                }
            }

			StreamReader srOutput = null;
			StreamReader srError = null;

			Process proc = System.Diagnostics.Process.Start(oInfo);
			proc.WaitForExit();
			srOutput = proc.StandardOutput;
			StandardOutput = srOutput.ReadToEnd();
			srError = proc.StandardError;
			StandardError = srError.ReadToEnd();
			int exitCode = proc.ExitCode;
			proc.Close();

			return exitCode;
		}

        public int Run(string exe, string args, Func<string,bool>? stdoutFunc, Func<string,bool>? stderrFunc, string? workingDir = null, Dictionary<string, string>? envVariables = null)
        {
            ProcessStartInfo oInfo = new ProcessStartInfo(exe, args);
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            if (workingDir != null) {
                oInfo.WorkingDirectory = workingDir;
            } else {
                oInfo.WorkingDirectory = GetWorkingDirectory();
            }

            oInfo.RedirectStandardOutput = true;
            oInfo.RedirectStandardError = true;

            if (envVariables != null) {
                foreach (var ev in envVariables) {
                    oInfo.EnvironmentVariables[ev.Key] = ev.Value;
                }
            }

            StreamReader srOutput = null;
            StreamReader srError = null;

            Process proc = System.Diagnostics.Process.Start(oInfo);
            srOutput = proc.StandardOutput;
            srError = proc.StandardError;
            if (stdoutFunc != null) {
                string? stdOutLine;
                while ((stdOutLine = srOutput.ReadLine()) != null) {
                    StandardOutput = stdOutLine;
                    var result = stdoutFunc(stdOutLine);
                    if (result) {
                        proc.Kill();
                    }
                }
                StandardError = srError.ReadToEnd();
            } else if (stderrFunc != null) {
                string? stdErrLine;
                while ((stdErrLine = srError.ReadLine()) != null) {
                    StandardError = stdErrLine;
                    var result = stderrFunc(stdErrLine);
                    if (result) {
                        proc.Kill();
                    }
                }
                StandardOutput = srOutput.ReadToEnd();
            }
            proc.WaitForExit();
            int exitCode = proc.ExitCode;
            proc.Close();

            return exitCode;
        }

        public string StandardOutput
        {
            get;
            private set;
        }
		public string StandardError
		{
			get;
			private set;
		}

		public static string GetWorkingDirectory()
		{
            return AppFolders.Instance.Temp;
        }
	}
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Diffology
{
    sealed class GitClient
    {
        private static readonly string GIT_FILE_NAME =
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                @"Git\cmd\git.exe");

        internal readonly string Path;
        readonly User _user;

        internal GitClient(string path, User user)
        {
            Path = path;
            _user = user;
        }

        internal static async Task<GitClient> Clone(User user, string id)
        {
            // TODO(vitor): The username is currently set as TestUser, because I
            // forgot that it should be an email. So, set this to an email in the
            // server and here, when possible.
            //
            // '%40' should be used to escape the '@' sign.
            var path = System.IO.Path.Combine(Consts.REPO_DIR, id);
            await Git($"clone http://{user.Name}:{user.Password}@{Consts.SERVER_ADDRESS}/repos/{id}.git {path}");
            return new GitClient(path, user);
        }

        internal async Task<bool> IsDirty()
        {
            var r = await AuthoredGit("status --porcelain");
            return r.StdOut.Length > 0;
        }

        internal async Task AddAll()
        {
            await AuthoredGit("add -A");
        }

        internal async Task Commit()
        {
            // If single quotes are used they become the actual commit message
            // so double quotes must be used.
            await AuthoredGit("commit --allow-empty-message -m \"\"");
        }

        internal async Task Pull()
        {
            await AuthoredGit("pull -s recursive -X ours");
        }

        internal async Task Push()
        {
            await AuthoredGit("push");
        }

        private Task<GitRet> AuthoredGit(string args, CancellationToken canceller = default)
        {
            args = $"-C {Path} -c user.name='{_user.Name}' -c user.email='{_user.Email}' " + args;
            return Git(args, canceller);
        }

        // https://github.com/jamesmanning/RunProcessAsTask/blob/master/src/RunProcessAsTask/ProcessEx.cs
        private static Task<GitRet> Git(string args, CancellationToken canceller = default)
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = GIT_FILE_NAME,
                    //FileName = "C:\\Program Files\\Git\\cmd\\git.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            var stdOut = new List<string>();
            var stdOutRes = new TaskCompletionSource<string[]>();
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    stdOut.Add(e.Data);
                }
                else
                {
                    stdOutRes.SetResult(stdOut.ToArray());
                }
            };

            var stdErr = new List<string>();
            var stdErrRes = new TaskCompletionSource<string[]>();
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    stdErr.Add(e.Data);
                }
                else
                {
                    stdErrRes.SetResult(stdErr.ToArray());
                }
            };

            var tcs = new TaskCompletionSource<GitRet>();
            process.Exited += (sender, e) =>
            {
                tcs.SetResult(
                    new GitRet(process.ExitCode, stdOutRes.Task.Result, stdErrRes.Task.Result));
                process.Dispose();
            };

            using (canceller.Register(() => tcs.TrySetCanceled()))
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return tcs.Task;
            }
        }
    }
}

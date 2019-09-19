namespace Diffology
{
    struct GitRet
    {
        internal int ExitCode;
        internal string[] StdOut;
        internal string[] StdErr;

        internal GitRet(int exitCode, string[] stdOut, string[] stdErr)
        {
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
        }

        public override string ToString()
        {
            string str = "";
            str += "StdOut:\n";
            if (StdOut != null) str += string.Join("\n", StdOut);
            str += "\nStdErr:\n";
            if (StdErr != null) str += string.Join("\n", StdErr);
            return str;
        }
    }
}

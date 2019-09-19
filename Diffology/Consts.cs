using System;
using System.IO;

namespace Diffology
{
    public static class Consts
    {
        internal const string VERSION = "0.0.1";

        public const string DIFFOLOGY_TABLE_NAME = "Diffology";

        internal const string SERVER_ADDRESS = "172.105.152.179";

        internal static readonly string APP_DATA_DIR = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Diffology\\");

        internal static readonly string REPO_DIR = Path.Combine(APP_DATA_DIR, "Repos");
    }
}

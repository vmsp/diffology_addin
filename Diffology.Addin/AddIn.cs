using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using NetOffice.AccessApi.Tools;
using NetOffice.Tools;
using Office = NetOffice.OfficeApi;
using Enums = NetOffice.AccessApi.Enums;

namespace Diffology.Addin
{
    [ComVisible(true)]
    [COMAddin("Diffology", "Diffology", LoadBehavior.LoadAtStartup)]
    [ProgId("Diffology.Diffology")]
    [Guid("FF9BB59D-3418-467B-AD13-F76975A75361")]
    [Codebase]
    [Timestamp]
    [CustomUI("RibbonUI.xml", true)]
    public class Addin : COMAddin
    {
        private static readonly Merger merger = new Merger();

        private bool _enabled = true;

        public Addin()
        {
        }

        public bool GetButtonEnabled(Office.IRibbonControl control)
        {
            return _enabled;
        }

        public async void OnSyncClick(Office.IRibbonControl control)
        {
            ToggleSyncEnabled(false);
            Application.SysCmd(Enums.AcSysCmdAction.acSysCmdInitMeter, "Syncing...", 4);
            Application.SysCmd(Enums.AcSysCmdAction.acSysCmdUpdateMeter, 3);

            try
            {
                await merger.Sync(Application.CurrentProject.FullName);

                // TODO(vitor): Move this to a better place?

                // Changes to the underlying data take a bit to be propagated to system
                // tables. The following spinlock is held until the system tables, and
                // therefore the UI, are up to speed.
                await Task.Run(() =>
                {
                    while (true)
                    {
                        var count = (int)Application.DCount(
                            "*",
                            "MSysObjects",
                            $"Name = '{Consts.DIFFOLOGY_TABLE_NAME}' AND Type = 1");
                        if (count > 0) break;
                    }
                });

                Application.SetHiddenAttribute(
                    Enums.AcObjectType.acTable,
                    Consts.DIFFOLOGY_TABLE_NAME,
                    true);
            }
            catch (AlreadyInUseException)
            {
                MessageBox.Show(
                    "No lock file was found. This usually means you are trying to " +
                    "sync a newly created database.\n\n" +
                    "Please save the database, reopen it and sync.",
                    "No Lock File Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            Application.SysCmd(Enums.AcSysCmdAction.acSysCmdUpdateMeter, 4);
            Application.SysCmd(Enums.AcSysCmdAction.acSysCmdRemoveMeter);
            ToggleSyncEnabled(true);
        }

        private void ToggleSyncEnabled(bool enabled)
        {
            _enabled = enabled;
            if (RibbonUI != null) RibbonUI.InvalidateControl("SyncButton");
        }
    }
}

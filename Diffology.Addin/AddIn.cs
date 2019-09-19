using System;
using System.Runtime.InteropServices;
using NetOffice.AccessApi.Tools;
using NetOffice.Tools;
using Office = NetOffice.OfficeApi;
using Access = NetOffice.AccessApi;

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
            Application.SysCmd(Access.Enums.AcSysCmdAction.acSysCmdInitMeter, "Diffology Syncing...", 4);
            Application.SysCmd(Access.Enums.AcSysCmdAction.acSysCmdUpdateMeter, 3);

            await merger.Sync(Application.CurrentProject.FullName);
            // TODO(vitor): Move this to a better place.
            Application.SetHiddenAttribute(Access.Enums.AcObjectType.acTable, Consts.DIFFOLOGY_TABLE_NAME, true);

            Application.SysCmd(Access.Enums.AcSysCmdAction.acSysCmdUpdateMeter, 4);
            Application.SysCmd(Access.Enums.AcSysCmdAction.acSysCmdRemoveMeter);
            ToggleSyncEnabled(true);
        }

        private void ToggleSyncEnabled(bool enabled)
        {
            _enabled = enabled;
            if (RibbonUI != null) RibbonUI.InvalidateControl("SyncButton");
        }
    }
}

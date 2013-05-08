using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gurock.SmartInspect;
using System.Diagnostics;

namespace SmartInspectHelper
{
    public static class si
    {
        public static string software_id = "";
        public static void EnableSmartInspect(string progid, Boolean clearlog)
        {
            if (IsSIRunning() == true)
            {
                SiAuto.Si.Connections = @"file(filename=c:\\tmp\displayclientlog.sil)";
                SiAuto.Si.Connections = "tcp()";
                SiAuto.Si.Enabled = true;
                if (clearlog == true) SiAuto.Main.ClearLog();
                software_id = progid;
                sii("SmartInspect Init: " + progid);
                sii("___________-_-000---___________");
            }
            else
            {
                SiAuto.Si.Connections = @"file(filename=c:\\tmp\displayclientlog.sil)";
                SiAuto.Si.Enabled = true;
                if (clearlog == true) SiAuto.Main.ClearLog();
                software_id = progid;
                sii("SmartInspect Init: " + progid);
                sii("___________-_-000---___________");
            }
        }

        public static Boolean IsSIRunning()
        {
            try
            {
                string processName = "SmartInspectConsole";
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                };
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void sii(string m) //information
        {
            SiAuto.Main.LogMessage(software_id + ": " + m);
        }

        public static void sil(string m) //leave function
        {
            SiAuto.Main.LeaveMethod(software_id + ": " + m);
        }

        public static void sie(string m) //enter function
        {
            SiAuto.Main.EnterMethod(software_id + ": " + m);
        }

        public static void six(Exception e) //exception
        {
            SiAuto.Main.LogException(software_id+": ", e);
        }

        public static void six(string info, Exception e) //exception
        {
            SiAuto.Main.LogException(software_id+"/"+info+": ", e);
        }
    }
}

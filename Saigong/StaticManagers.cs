using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Documents;
using System.Windows.Input;
using System.Windows;
using System.IO;

namespace Saigong
{
    enum DirType
    {
        Save, Back, Plan
    }

    static class DirectoryManager
    {
        static public string SaigongDir = "Saigong/";
        static public string ConfigFileDir = "Saigong/config.txt";
        static public string StyleDir = "Saigong/style/";
        static public string LangDir = "Saigong/lang/";

        static public string SaveExtension = ".txt";
        static public string SaveDir = "saves/";
        static public string BackDir = "saves/back/";
        static public string PlanDir = "saves/plan/";

        static public string GetDir(string title, DirType type)
        {
            string location = "";
            switch (type)
            {
                case DirType.Save:
                    location = SaveDir; break;
                case DirType.Back:
                    location = BackDir; break;
                case DirType.Plan:
                    location = PlanDir; break;
            }
            return location + title + SaveExtension;
        }
    }

    static class TimeStringManager
    {
        static string DateFormat = "yyyy-M-d";
        static string DateTimeFormat = "yyyy-M-d HHmm";

        static public string GetCurrentDateString()
        {
            return DateTime.Now.ToString(DateFormat);
        }

        static public string GetCurrentDateTimeString()
        {
            return DateTime.Now.ToString(DateTimeFormat);
        }
    }
}

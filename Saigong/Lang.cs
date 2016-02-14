using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO/*sys*/;

namespace Saigong
{
    /*
     * TODO: Load language settings from a language file in the future!
     */
    class Lang : Dictionary<string, string>
    {
        /*
         * 紀念在世不到一年的関越文字
         * 
         * public static string title = "関越文字";
         * 
         */
        public Lang(string name="") // Initialise language
        {
            // First initialise the values
            // Default values of the strings will be assigned
            this["title"] = "西江";
            this["startupFinished"] = "萬事俱備";
            this["saved"] = "已保存";
            this["loaded"] = "已載入";
            this["loadFail"] = "載入失敗";
            this["planSaved"] = "計劃已保存";
            this["planLoaded"] = "計劃已載入";
            this["planLoadFail"] = "未能載入計劃";
            this["nullTitle"] = "標題不可為空";
            this["nullOperation"] = "搜索內容不可為空";
            this["notFound"] = "未有找到";
            this["found"] = "已找到";
            this["autoBackupDone"] = "已自動備份";
            this["backupDone"] = "已備份";
            this["chara"] = "字";
            this["shutdown"] = "再見";

            if (name == "")
            {
                return;
            }

            Dictionary<string, string> langFile =
                ConfigLoader.LoadConfigFile("lang/" + name + ".txt");

            if (langFile == null)
            {
                return;
            }

            foreach (var p in langFile)
            {
                if (this.Keys.Contains(p.Key))
                {
                    this[p.Key] = p.Value;
                }
            }

            return;

            // Then the language file should be loaded
            if (! File.Exists("lang/" + name + ".txt"))
            {
                return;
            }

            // Only UTF-8 text can be readed
            StreamReader sr = File.OpenText("lang/" + name + ".txt");
            while (! sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] splited = line.Split(':');
                if (splited.Count() != 2)
                {
                    break;
                }
                if (! this.Keys.Contains(splited[0]))
                {
                    return;
                }

                this[splited[0]] = splited[1];
            }
        }
    }
}

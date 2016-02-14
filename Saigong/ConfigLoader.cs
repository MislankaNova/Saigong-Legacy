using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace Saigong
{
    static class ConfigLoader
    {
        public static Dictionary<string, string> LoadConfigFile(string dir)
        {
            if (! File.Exists(dir))
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            var sr = File.OpenText(dir);

            while (! sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] splited = line.Split(':');
                if (splited.Length == 2)
                {
                    result[splited[0].Trim()] = splited[1].Trim();
                }
            }
            sr.Close();

            return result;
        }
    }
}

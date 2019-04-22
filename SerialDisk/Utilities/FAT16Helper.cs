using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AtariST.SerialDisk.Utilities
{
    public static class FAT16Helper
    {
        public static string GetShortFileName(string fileName)
        {
            Regex invalidCharactersRegex = new Regex("[^\\-A-Z0-9_\\.~]");

            fileName = invalidCharactersRegex.Replace(fileName.ToUpper(), "_");

            string shortFileName;

            int dotIndex = fileName.IndexOf(".");

            if (dotIndex == -1)
                shortFileName = fileName;
            else
                shortFileName = fileName.Substring(0, dotIndex);

            if (shortFileName.Length > 8)
                shortFileName = fileName.Substring(0, 8);

            dotIndex = fileName.LastIndexOf(".");

            if (dotIndex != -1)
            {
                string Extender = fileName.Substring(dotIndex + 1);

                if (Extender.Length > 3)
                    Extender = Extender.Substring(0, 3);

                shortFileName += "." + Extender;
            }

            return shortFileName;
        }
    }
}

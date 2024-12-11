using System.IO;
using System.Linq;

namespace AstroModIntegrator
{
    public static class IniParser
    {
        public static string FindLine(string rawData, string section, string key)
        {
            string currentSectionName = null;
            using (StringReader reader = new StringReader(rawData))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 3) continue;

                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        currentSectionName = line.Substring(1, line.Length - 2);
                    }
                    else if (currentSectionName == section)
                    {
                        string[] fullLineData = line.Split('=');
                        if (fullLineData.Length >= 2)
                        {
                            string currentKey = fullLineData[0].Trim();
                            string currentValue = string.Join("=", fullLineData.Skip(1).ToArray()).Trim();
                            if (currentKey == key) return currentValue;
                        }
                    }
                }
            }

            return null;
        }
    }
}

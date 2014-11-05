using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

// TODO: Variables
// TODO: Enumerable sections

// ReSharper disable once CheckNamespace
namespace System.Data.Ini
{
    public class Ini
    {
        private List<string> _buffer = new List<string>();
        private Dictionary<string, Dictionary<string, string>> _data = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, List<string>> _dataBlocks = new Dictionary<string, List<string>>();
        private string Path { get; set; }

        public Ini()
        {
            Path = null;
        }
        public Ini(string fileName) : this()
        {
            ReadFile(fileName);
        }

        public bool ReadFile(string fileName)
        {
            _buffer.Clear();
            return AppendFile(fileName);
        }


        public bool AppendFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName, Encoding.Default);
            foreach (string line in lines)
            {
                _buffer.Add(line);
            }
            Parse();
            return false;
        }

        /// <summary>
        /// Writes the current ini data to a file on disk
        /// </summary>
        /// <param name="fileName">Path to the file</param>
        /// <returns></returns>
        public bool SaveAs(string fileName)
        {
            using (TextWriter file = File.CreateText(fileName))
            {
                Report(file);
                file.Close();
            }
            return false;
        }
        public void Report(TextWriter output = null)
        {
            if (output == null)
            {
                output = Console.Out;
                output.WriteLine(" --- Reporting contents of ini object: --- ");
            }
            foreach (KeyValuePair<string, Dictionary<string, string>> section in _data)
            {
                output.WriteLine("[" + section.Key + "]");
                foreach (KeyValuePair<string, string> property in section.Value)
                {
                    output.WriteLine(property.Key + " = " + property.Value);
                }
                output.WriteLine();
            }
        }

        public string GetString(string section, string property, string defval = null)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(property))
            {
                return _data[section][property];
            }
            return defval;
        }
        public int GetInt(string section, string property, int defval = 0)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(property))
            {
                return _data[section][property].ToInt();
            }
            return defval;
        }
        public float GetFloat(string section, string property, float defval = 0.0f)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(property))
            {
                return _data[section][property].ToFloat();
            }
            return defval;
        }
        public bool GetBool(string section, string property, bool defval = false)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(property))
            {
                return _data[section][property].ToBool();
            }
            return defval;
        }

        public void SetString(string section, string property, string value)
        {
            if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, string>();
            _data[section][property] = value;
        }
        public void SetInt(string section, string property, int value)
        {
            if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, string>();
            _data[section][property] = value.ToString(CultureInfo.InvariantCulture);
        }
        public void SetFloat(string section, string property, float value)
        {
            if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, string>();
            _data[section][property] = value.ToString(CultureInfo.InvariantCulture);
        }
        public void SetBool(string section, string property, bool value)
        {
            if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, string>();
            _data[section][property] = value.ToString();
        }
        public Dictionary<string, string> GetSection(string section)
        {
            return _data.ContainsKey(section) ? _data[section] : new Dictionary<string, string>();
        }
        private void Parse()
        {
            string currentSection = "", currentProperty = "", currentValue = "";
            bool comment = false, instring = false, indstring = false, gotsection = false, gotproperty = false, insection = false, datablock = false;

            foreach (var line in _buffer)
            {
                if (datablock)
                {
                    if (line == "[/]")
                    {
                        datablock = false;
                        gotsection = false;
                        currentSection = "";
                    }
                    else
                    {
                        Console.WriteLine(currentSection.Substring(1) + " > " + line);
                        _dataBlocks[currentSection.Substring(1)].Add(line);
                    }
                }
                else
                {
                    for (var col = 0; col < line.Length; col++)
                    {
                        var chr = line.Substring(col, 1);

                        if (instring)
                        {
                            if (chr == "'")
                            {
                                instring = false;
                            }
                            else
                            {
                                currentValue += chr;
                            }
                        }
                        else if (indstring)
                        {
                            if (chr == "\"")
                            {
                                indstring = false;
                            }
                            else
                            {
                                currentValue += chr;
                            }
                        }
                        else if (comment)
                        {
                            // do nothing
                        }
                        else if (chr == "'")
                        {
                            instring = true;
                        }
                        else if (chr == "\"")
                        {
                            indstring = true;
                        }
                        else if (chr == "#" || chr == ";")
                        {
                            comment = true;
                        }
                        else if (chr == "[" && !insection)
                        {
                            insection = true;
                            currentSection = "";
                            gotsection = false;
                        }
                        else if (chr == "]" && insection)
                        {
                            gotsection = true;
                            insection = false;
                            if (currentSection.Substring(0, 1) == "@")
                            {
                                datablock = true;
                                _dataBlocks[currentSection].Clear();
                            }
                        }
                        else if (gotsection && !gotproperty && (chr == "=" || chr == ":"))
                        {
                            gotproperty = true;
                        }
                        else
                        {
                            if (chr != " " && chr != "\t")
                            {
                                if (!gotsection)
                                {
                                    currentSection += chr;
                                }
                                else if (/*gotsection && */!gotproperty)
                                {
                                    currentProperty += chr;
                                }
                                else //if (gotsection && gotproperty)
                                {
                                    currentValue += chr;
                                }
                            }
                            else if ((gotsection && !gotproperty && currentProperty.Length > 0))
                            {
                                currentProperty += chr;
                            }
                        }
                    }
                }
                if (gotsection && gotproperty)
                {
                    if (!_data.ContainsKey(currentSection)) _data.Add(currentSection, new Dictionary<string, string>());
                    _data[currentSection][currentProperty] = currentValue;
                }
                comment = false;
                gotproperty = false;
                currentProperty = "";
                currentValue = "";
            }
        }
    }

    public static class IniUtils
    {
        public static int ToInt(this string input)
        {
            int output;
            return int.TryParse(input, out output) ? output : 0;
        }
        public static float ToFloat(this string input)
        {
            float output;
            return float.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out output) ? output : 0;
        }
        public static bool ToBool(this string input)
        {
            return (input.ToLower() == "true" || input.ToLower() == "yes");
        }
        public static string PathFromUrl(string url)
        {
            return "";
        }
    }
}

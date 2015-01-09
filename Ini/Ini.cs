/*
The MIT License (MIT)

Copyright (c) 2014 Mattias Svensson

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

// TODO: Variables
// TODO: Expressions

namespace System.Data.Ini
{
    public enum Separator
    {
        Equals, Colon, EqualsReadable, ColonReadable
    }
    public enum BooleanStyle {
        TrueFalse, OnOff, YesNo
    }

    public class Ini
    {
        private readonly List<string> _buffer = new List<string>();
        private readonly Dictionary<string, Dictionary<string, string>> _data = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, List<string>> _dataBlocks = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        private string Path { get; set; }
        public bool PostProcess { get; set; }

        public Ini()
        {
            Path = null;
            PostProcess = false;
        }
        public Ini(string fileName) : this()
        {
            ReadFile(fileName);
        }

        public void Clear(bool clearVariables = true)
        {
            _buffer.Clear();
            _data.Clear();
            _dataBlocks.Clear();
            if (clearVariables) _variables.Clear();
        }

        public bool ReadFile(string fileName)
        {
            _buffer.Clear();
			Path = fileName;
            return AppendFile(fileName);
        }

        public bool AppendFile(string fileName)
        {
            try
            {
                var lines = File.ReadAllLines(fileName, Encoding.Default);
                foreach (var line in lines)
                {
                    _buffer.Add(line);
                }
                Parse();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool ReadLines(IEnumerable<string> lines)
        {
            _buffer.Clear();
            return AppendLines(lines);
        }

        public bool AppendLines(IEnumerable<string> lines)
        {
            _buffer.AddRange(lines);
            Parse();
            return true;
        }

		public bool Save(Separator separator = Separator.EqualsReadable, BooleanStyle booleanStyle = BooleanStyle.TrueFalse) {
			if (Path != null) {
				return SaveAs (Path, separator, booleanStyle);
			}
			return false;
		}
        /// <summary>
        /// Writes the current ini data to a file on disk
        /// </summary>
        /// <param name="fileName">Path to the file</param>
        /// <returns></returns>
		public bool SaveAs(string fileName, Separator separator = Separator.EqualsReadable, BooleanStyle booleanStyle = BooleanStyle.TrueFalse)
        {
            using (TextWriter file = File.CreateText(fileName))
            {
				Report(output: file, separator: separator);
                file.Close();
				return true;
            }
            //return false;
        }
        public void Report(Separator separator = Separator.EqualsReadable, TextWriter output = null, BooleanStyle booleanStyle = BooleanStyle.TrueFalse)
        {
            if (output == null)
            {
                output = Console.Out;
                output.WriteLine(" --- Reporting contents of ini object: --- ");
            }
            var sep = "";
            switch (separator)
            {
                case Separator.Equals:
                    sep = "=";
                    break;
                case Separator.EqualsReadable:
                    sep = " = ";
                    break;
                case Separator.Colon:
                    sep = ":";
                    break;
                case Separator.ColonReadable:
                    sep = ": ";
                    break;
            }
            foreach (var section in _data)
            {
                output.WriteLine("[" + section.Key + "]");
                foreach (var property in section.Value)
                {
                    if (property.Value.IsBool()) {
						output.WriteLine(property.Key + sep + property.Value.ToBoolString(booleanStyle));
                    } else {
                        output.WriteLine(property.Key + sep + (property.Value.Contains(" ") || property.Value.Contains("\t") ? "'" + property.Value.Replace("\"", "\\\"").Replace("'", "\\'") + "'" : property.Value));
                    }
                }
                output.WriteLine();
            }
            foreach (var section in _dataBlocks)
            {
                output.WriteLine("[@"+section.Key+"]");
                foreach (var line in section.Value)
                {
                    output.WriteLine(line);
                }
                output.WriteLine("[/]");
                output.WriteLine();
            }
        }

        public bool HasSection(string section) {
            return _data.ContainsKey(section);
        }
        public bool HasProperty(string section, string property) {
            if (HasSection(section)) {
                return _data[section].ContainsKey(property);
            } else return false;
        }
		public void DeleteSection(string section) {
			_data.Remove (section);
		}
		public void DeleteProperty(string section, string property) {
			if (HasSection (section)) {
				_data [section].Remove (property);
			}
		}
        public string GetString(string section, string property, string defval = null)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(property))
            {
                return PostProcess ? PostProcessor(_data[section][property]) : _data[section][property];
            }
            return defval;
        }
        public int GetInt(string section, string property, int defval = 0)
        {
            return GetString(section, property, defval.ToString()).ToInt();
        }
        public float GetFloat(string section, string property, float defval = 0.0f)
        {
            return GetString(section, property, defval.ToString(CultureInfo.InvariantCulture)).ToFloat();
        }
        public bool GetBool(string section, string property, bool defval = false)
        {
            return GetString(section, property, defval.ToString()).ToBool();
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
        public List<string> GetDataBlock(string section)
        {
            return _dataBlocks.ContainsKey(section) ? _dataBlocks[section] : new List<string>();
        }
        public void SetDataBlock(string section, List<string> data)
        {
            _dataBlocks[section] = data;
        }
        public IEnumerable<Dictionary<string, string>> GetIterativeSections(string section)
        {
            return _data.Where(sect => sect.Key.Contains("#") && sect.Key.Length > section.Length && sect.Key.Substring(0, section.Length) == section).Select(s => s.Value);
        }
        private void Parse()
        {
            string currentSection = "", currentProperty = "", currentValue = "";
            bool comment = false, instring = false, indstring = false, gotsection = false, gotproperty = false, insection = false, datablock = false, escape = false;

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
                            if (escape)
                            {
                                escape = false;
                                currentValue += chr;
                            }
                            else if (chr == "\\")
                            {
                                escape = true;
                            }
                            else if (chr == "'")
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
                            if (escape)
                            {
                                escape = false;
                                currentValue += chr;
                            }
                            else if (chr == "\\")
                            {
                                escape = true;
                            }
                            else if (chr == "\"")
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
                        else if (!insection && (chr == "#" || chr == ";"))
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
                                _dataBlocks[currentSection.Substring(1)] = new List<string>();
                            }
                        }
                        else if (gotsection && !gotproperty && (chr == "=" || chr == ":"))
                        {
                            gotproperty = true;
                            //currentProperty = currentProperty.Trim();
                        }
                        else
                        {
                            if ((chr != " " && chr != "\t") || !gotsection)
                            {
                                if (!gotsection && chr != "\t")
                                {
                                    currentSection += chr;
                                }
                                else if (/*gotsection && */!gotproperty)
                                {
                                    currentProperty += chr;
                                }
                                else //if (gotsection && gotproperty)
                                {
                                    if (currentValue.Length == 0 && (chr == " " || chr == "\t"))
                                    {
                                    } else
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
                    _data[currentSection][currentProperty.Trim()] = currentValue;
                }
                comment = false;
                gotproperty = false;
                currentProperty = "";
                currentValue = "";
            }
        }

        private string PostProcessor(string input)
        {
            return input;
        }
    }

    public static class IniUtils
    {
        public static bool IsBool(this string input) {
            string lc = input.ToLower();
            if (lc == "true" || lc == "false" || lc == "on" || lc == "off" || lc == "yes" || lc == "no") return true;
            else return false;
        }
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
			return (input.ToLower() == "true" || input.ToLower() == "yes" || input.ToLower() == "on");
        }
        public static string ToBoolString(this string input, BooleanStyle style = BooleanStyle.TrueFalse) {
            if (input.ToBool()) {
                if (style == BooleanStyle.OnOff) return "on";
                else if (style == BooleanStyle.YesNo) return "yes";
                else return "true";
            } else {
                if (style == BooleanStyle.OnOff) return "off";
                else if (style == BooleanStyle.YesNo) return "no";
                else return "false";
            }
        }
        public static string Get(this Dictionary<string, string> dictionary, string key, string defaultValue = "")
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : defaultValue;
        }
        public static Dictionary<string, string> GetIterativeProperties(this Dictionary<string, string> dict, string property)
        {
            return dict.Where(prop => prop.Key.Contains("-") && prop.Key.Length > property.Length && prop.Key.Substring(0, property.Length) == property).ToDictionary(prop => prop.Key, prop => prop.Value);
        }
        public static string PathFromUrl(string url)
        {
            return "";
        }
    }
}

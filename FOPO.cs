using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace FOPO
{
    class Program
    {
        /// <summary>
        /// だれかもっときれいに実装してｗｗｗ
        /// </summary>
        static void Main(string[] args)
        {
            string filename = (args?.Length ?? 0) == 0 ? "main.php" : args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine("ファイル({0})が見つかりませんでした", filename);
                Console.ReadKey();
                return;
            }
            string original = File.ReadAllText(filename);
            //phase1
            string source1 = base64_decode(get_string(replace_ascii_str_call(original).Replace("\r", "").Replace("\n", ""), "base64_decode("));
            //phase2
            string[] source2 = get_string2(replace_ascii_str_call(source1), "gzinflate(base64_decode(str_rot13(")
                                    .Select(d => gzinflate_base64_decode_str_rot13(d)).ToArray();
            //phase3
            string source3 = source2.Select(src => decompress_loop(replace_ascii_str_call(src), false))
                                    .FirstOrDefault(src => src.Contains("gzinflate(base64_decode("));
            //phase4
            string source4 = decompress_loop(source3, true);
            //phase5
            string source5 = source4.Substring(2);
            //final
            string final = SimpleFormat(source5);
            File.WriteAllText(new FileInfo(filename).DirectoryName + "\\RESULT.PHP", final);
            Console.WriteLine(final);
            Console.ReadKey();
        }
        
        static string gzinflate_base64_decode_str_rot13(string input)
        {
            return gzinflate_base64_decode(str_rot13(input));
        }
        static string gzinflate_base64_decode(string input)
        {
            using (MemoryStream ms1 = new MemoryStream(Convert.FromBase64String(input)))
            using (MemoryStream ms2 = new MemoryStream())
            using (DeflateStream stream = new DeflateStream(ms1, CompressionMode.Decompress))
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int read = stream.Read(buffer, 0, 1024);
                    if (read == 0)
                        break;
                    ms2.Write(buffer, 0, read);
                }
                return Encoding.UTF8.GetString(ms2.ToArray());
            }
        }

        static string base64_decode(string input)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(input));
        }
        static string str_rot13(string value)
        {
            char[] array = value.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                int number = (int)array[i];

                if (number >= 'a' && number <= 'z')
                {
                    if (number > 'm')
                    {
                        number -= 13;
                    }
                    else
                    {
                        number += 13;
                    }
                }
                else if (number >= 'A' && number <= 'Z')
                {
                    if (number > 'M')
                    {
                        number -= 13;
                    }
                    else
                    {
                        number += 13;
                    }
                }
                array[i] = (char)number;
            }
            return new string(array);
        }
        static string gzinflate(string input)
        {
            using (MemoryStream ms1 = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (MemoryStream ms2 = new MemoryStream())
            using (DeflateStream stream = new DeflateStream(ms1, CompressionMode.Decompress))
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int read = stream.Read(buffer, 0, 1024);
                    if (read == 0)
                        break;
                    ms2.Write(buffer, 0, read);
                }
                return Encoding.UTF8.GetString(ms2.ToArray());
            }
        }

        static string replace_ascii(string input)
        {
            string result = input;
            for (int i = 32; i <= 126; i++)
            {
                result = result.Replace("\\" + Convert.ToString(i, 8), ((char)i).ToString())
                    .Replace($"\\x{i.ToString("X2")}", ((char)i).ToString())
                    .Replace($"\\x{i.ToString("x2")}", ((char)i).ToString());
            }
            return result;
        }
        static Dictionary<string, string> str_var_join_and_list(string input)
        {
            Dictionary<string, string> var_list = new Dictionary<string, string>();
            foreach (System.Text.RegularExpressions.Match v in Regex.Matches(input, @"\$" + "(?<var_name>[a-z0-9]*)=\"(?<text>[a-zA-Z0-9_]*)\";"))
            {
                string name = v.Groups["var_name"].Value;
                string value = v.Groups["text"].Value;
                if (var_list.ContainsKey(name))
                    var_list[name] = value;
                else
                    var_list.Add(name, value);
            }
            foreach (System.Text.RegularExpressions.Match v in Regex.Matches(input, @"\$" + "(?<var_name>[a-z0-9]*)" + @"\." + "=\"(?<text>[a-zA-Z0-9_]*)\";"))
            {
                string name = v.Groups["var_name"].Value;
                string value = v.Groups["text"].Value;
                var_list[name] += value;
            }
            return var_list;
            //return string.Join("\n", var_list.Select(d => string.Format("${0} = \"{1}\"", d.Key, d.Value)));
        }
        static string replace_str_call(string input, Dictionary<string, string> var_list)
        {
            string result = input;
            foreach (var item in var_list)
            {
                result = result.Replace("$" + item.Key + "(", item.Value + "(");
            }
            return result;
        }
        static string get_string(string input, string header)
        {
            int start = input.IndexOf(header + "\"");
            if (start == -1) return null;
            start += header.Length + 1;
            int end = input.IndexOf("\"", start);
            return input.Substring(start, end - start);
        }
        static string[] get_string2(string input, string header)
        {
            int start = input.IndexOf(header + "\"");
            if (start == -1) return null;
            start += header.Length + 1;
            int end = input.IndexOf("\"", start);
            string r1 = input.Substring(start, end - start);
            start = input.IndexOf(header + "\"", end + 1);
            if (start == -1) return null;
            start += header.Length + 1;
            end = input.IndexOf("\"", start);
            string r2 = input.Substring(start, end - start);
            return new string[] { r1, r2 };
        }

        static string replace_ascii_str_call(string input)
        {
            string a = replace_ascii(input);
            return replace_str_call(a, str_var_join_and_list(a));
        }
        static string decompress_loop(string input, bool use_no_str_rot13)
        {
            string tmp = input;
            while (true)
            {
                if (tmp.Contains("gzinflate(base64_decode(str_rot13("))
                    tmp = replace_ascii_str_call(gzinflate_base64_decode_str_rot13(get_string(tmp, "gzinflate(base64_decode(str_rot13(")));
                else if (use_no_str_rot13 && tmp.Contains("gzinflate(base64_decode("))
                    tmp = replace_ascii_str_call(gzinflate_base64_decode(get_string(tmp, "gzinflate(base64_decode(")));
                else
                    break;
            }
            return tmp;
        }

        static string SimpleFormat(string source)
        {
            string tmp = source.Replace("; ", ";\n");
            tmp = tmp.Replace("{ ", "{\n");
            tmp = tmp.Replace("} ", "}\n");
            tmp = tmp.Replace("class ", "\nclass ");
            tmp = Regex.Replace(tmp, " (namespace [a-zA-Z0-9_]*;)", "\n$1\n");
            tmp = Regex.Replace(tmp, "(case '([^'\\\\]|\\\\\\\\|\\\\')*?'): ", "$1:\n");
            tmp = Regex.Replace(tmp, "(case \"([^'\\\\]|\\\\\\\\|\\\\')*?\"): ", "$1:\n");
            int indent = 0;
            return string.Join("\n", tmp.Split('\n').Select(line =>
            {
                indent -= line.Count(c => c == '}');
                string result = new string('\t', indent) + line;
                indent += line.Count(c => c == '{');
                indent += Regex.IsMatch(result, "case '([^'\\\\]|\\\\\\\\|\\\\')*?':") ? 1 : 0;
                indent += Regex.IsMatch(result, "case \"([^'\\\\]|\\\\\\\\|\\\\')*?\":") ? 1 : 0;
                indent -= line.Contains("break;") ? 1 : 0;
                return result;
            }));
        }
    }
}

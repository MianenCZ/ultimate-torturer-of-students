using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UTS.Backend.Domain;

namespace UTS.Backend.Persistence
{
    public static class UtsCsv
    {
        public static void Save(UtsDocument doc, string path)
        {
            doc.Validate();
            using var w = new StreamWriter(path, false, new UTF8Encoding(false));

            w.WriteLine($"UTS,1,ClassName,{Esc(doc.ClassName)},T,{doc.T},K,{doc.K},E,{doc.E}");

            w.Write("StudentId,StudentName");
            for (int t = 0; t < doc.T; t++) w.Write($",Test{t + 1}");
            w.WriteLine();

            foreach (var s in doc.Students)
            {
                w.Write($"{Esc(s.Id)},{Esc(s.Name)}");
                for (int t = 0; t < doc.T; t++) w.Write($",{CellStateCodec.ToChar(s[t])}");
                w.WriteLine();
            }
        }

        public static UtsDocument Load(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2) throw new FormatException("Invalid .uts (too short).");

            var meta = Parse(lines[0]);
            if (meta.Count < 2 || meta[0] != "UTS" || meta[1] != "1")
                throw new FormatException("Unsupported .uts version.");

            var doc = new UtsDocument
            {
                ClassName = Get(meta, "ClassName", "Unnamed"),
                T = int.Parse(Get(meta, "T", "0")),
                K = int.Parse(Get(meta, "K", "0")),
                E = int.Parse(Get(meta, "E", "0")),
            };

            var header = Parse(lines[1]);
            int expected = 2 + doc.T;
            if (header.Count != expected) throw new FormatException("Invalid header width.");

            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var row = Parse(lines[i]);
                if (row.Count != expected) throw new FormatException($"Row {i + 1} width mismatch.");

                var s = new StudentRecord(row[0], row[1], doc.T, doc.K, doc.E);
                for (int t = 0; t < doc.T; t++)
                {
                    if (row[2 + t].Length != 1) throw new FormatException("Invalid cell token.");
                    s[t] = CellStateCodec.FromChar(row[2 + t][0]);
                }
                doc.Students.Add(s);
            }

            doc.Validate();
            return doc;
        }

        // Minimal CSV escaping/parsing (no multiline fields)
        private static string Esc(string s) =>
            s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

        /// <summary>
        /// RFC4180-like line parser:
        /// - Comma separated
        /// - Quotes allow commas
        /// - Double quotes inside quoted field are escaped as ""
        /// - No multiline fields (caller supplies a single line)
        /// </summary>
        private static List<string> Parse(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            var sb = new StringBuilder(line.Length);
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Escaped quote inside a quoted field
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++; // consume second quote
                        }
                        else
                        {
                            inQuotes = false; // end of quoted field
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"')
                    {
                        // Quotes are only valid as the first char of a field.
                        if (sb.Length != 0)
                            throw new FormatException("Invalid CSV: quote in unquoted field.");

                        inQuotes = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            if (inQuotes)
                throw new FormatException("Invalid CSV: unterminated quoted field.");

            result.Add(sb.ToString());
            return result;
        }

        /// <summary>
        /// Reads a value from a token list formatted as:
        ///   key0,value0,key1,value1,...
        /// Returns <paramref name="defaultValue"/> if key is not present.
        /// Throws if the key is present but has no following value token.
        /// </summary>
        private static string Get(IReadOnlyList<string> tokens, string key, string defaultValue)
        {
            if (tokens == null) return defaultValue;

            for (int i = 0; i < tokens.Count; i += 2)
            {
                if (string.Equals(tokens[i], key, StringComparison.Ordinal))
                {
                    if (i + 1 >= tokens.Count)
                        throw new FormatException($"Invalid metadata: missing value for key '{key}'.");
                    return tokens[i + 1];
                }
            }

            return defaultValue;
        }
    }


}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MoneyRank.Domain.V4
{
    /// <summary>RFC 4180 CSV reader (UTF-8 BOM tolerant). Pure C#, no engine dependencies.</summary>
    public static class CsvReader
    {
        public static List<Dictionary<string, string>> ReadRecords(string text)
        {
            var rows = ReadRows(text);
            var result = new List<Dictionary<string, string>>();
            if (rows.Count == 0) return result;
            var header = rows[0];
            for (var r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 1 && row[0].Length == 0) continue;
                var record = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < header.Count; c++)
                    record[header[c]] = c < row.Count ? row[c] : string.Empty;
                result.Add(record);
            }
            return result;
        }

        public static List<List<string>> ReadRows(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return rows;
            var i = 0;
            if (text[0] == '﻿') i = 1;
            var field = new StringBuilder();
            var row = new List<string>();
            var inQuotes = false;
            var any = false;
            for (; i < text.Length; i++)
            {
                var ch = text[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(ch);
                    continue;
                }
                switch (ch)
                {
                    case '"': inQuotes = true; any = true; break;
                    case ',': row.Add(field.ToString()); field.Clear(); any = true; break;
                    case '\r': break;
                    case '\n':
                        row.Add(field.ToString()); field.Clear();
                        rows.Add(row); row = new List<string>(); any = false;
                        break;
                    default: field.Append(ch); any = true; break;
                }
            }
            if (any || field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }
    }

    /// <summary>Minimal JSON parser: objects -> Dictionary, arrays -> List, numbers -> decimal.</summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new Parser(json);
            p.SkipWs();
            var v = p.ReadValue();
            p.SkipWs();
            if (!p.End) throw new FormatException("Unexpected trailing JSON content at " + p.Pos);
            return v;
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>(StringComparer.Ordinal);
            var v = Parse(json) as Dictionary<string, object>;
            if (v == null) throw new FormatException("JSON value is not an object");
            return v;
        }

        private sealed class Parser
        {
            private readonly string _s;
            public int Pos;
            public Parser(string s) { _s = s; if (_s.Length > 0 && _s[0] == '﻿') Pos = 1; }
            public bool End => Pos >= _s.Length;

            public void SkipWs() { while (Pos < _s.Length && char.IsWhiteSpace(_s[Pos])) Pos++; }

            public object ReadValue()
            {
                if (End) throw new FormatException("Unexpected end of JSON");
                var c = _s[Pos];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default: return ReadNumber();
                }
            }

            private void Expect(string lit)
            {
                if (string.CompareOrdinal(_s, Pos, lit, 0, lit.Length) != 0)
                    throw new FormatException("Invalid JSON literal at " + Pos);
                Pos += lit.Length;
            }

            private Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>(StringComparer.Ordinal);
                Pos++; SkipWs();
                if (Pos < _s.Length && _s[Pos] == '}') { Pos++; return d; }
                while (true)
                {
                    SkipWs();
                    var key = ReadString();
                    SkipWs();
                    if (End || _s[Pos] != ':') throw new FormatException("Expected ':' at " + Pos);
                    Pos++; SkipWs();
                    d[key] = ReadValue();
                    SkipWs();
                    if (End) throw new FormatException("Unterminated object");
                    if (_s[Pos] == ',') { Pos++; continue; }
                    if (_s[Pos] == '}') { Pos++; return d; }
                    throw new FormatException("Expected ',' or '}' at " + Pos);
                }
            }

            private List<object> ReadArray()
            {
                var l = new List<object>();
                Pos++; SkipWs();
                if (Pos < _s.Length && _s[Pos] == ']') { Pos++; return l; }
                while (true)
                {
                    SkipWs();
                    l.Add(ReadValue());
                    SkipWs();
                    if (End) throw new FormatException("Unterminated array");
                    if (_s[Pos] == ',') { Pos++; continue; }
                    if (_s[Pos] == ']') { Pos++; return l; }
                    throw new FormatException("Expected ',' or ']' at " + Pos);
                }
            }

            private string ReadString()
            {
                if (End || _s[Pos] != '"') throw new FormatException("Expected string at " + Pos);
                Pos++;
                var sb = new StringBuilder();
                while (Pos < _s.Length)
                {
                    var c = _s[Pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (Pos >= _s.Length) break;
                    var e = _s[Pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (Pos + 4 > _s.Length) throw new FormatException("Bad unicode escape");
                            sb.Append((char)int.Parse(_s.Substring(Pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            Pos += 4;
                            break;
                        default: throw new FormatException("Bad escape at " + Pos);
                    }
                }
                throw new FormatException("Unterminated string");
            }

            private decimal ReadNumber()
            {
                var start = Pos;
                while (Pos < _s.Length && "+-0123456789.eE".IndexOf(_s[Pos]) >= 0) Pos++;
                if (start == Pos) throw new FormatException("Unexpected character '" + _s[Pos] + "' at " + Pos);
                var token = _s.Substring(start, Pos - start);
                if (token.IndexOfAny(new[] { 'e', 'E' }) >= 0)
                    return (decimal)double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
                return decimal.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}

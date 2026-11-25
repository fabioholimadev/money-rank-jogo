using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MoneyRank.Domain.V4
{
    /// <summary>Reflective deep clone for plain state graphs (used for atomic all-or-nothing commands).</summary>
    public static class StateClone
    {
        public static T Clone<T>(T source) where T : class => (T)CloneObject(source, typeof(T));

        private static object CloneObject(object source, Type type)
        {
            if (source == null) return null;
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) return source;
            if (type.IsArray)
            {
                var src = (Array)source;
                var elem = type.GetElementType();
                var dst = Array.CreateInstance(elem, src.Length);
                for (var i = 0; i < src.Length; i++) dst.SetValue(CloneObject(src.GetValue(i), elem), i);
                return dst;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elem = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(type);
                foreach (var item in (IEnumerable)source) list.Add(CloneObject(item, elem));
                return list;
            }
            var copy = Activator.CreateInstance(type);
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                f.SetValue(copy, CloneObject(f.GetValue(source), f.FieldType));
            return copy;
        }
    }

    /// <summary>Generic JSON save format over public fields (exact decimals, no external dependencies).</summary>
    public static class StateJson
    {
        public static string Write(object state)
        {
            var sb = new StringBuilder(64 * 1024);
            WriteValue(sb, state, state.GetType());
            return sb.ToString();
        }

        public static T Read<T>(string json) where T : class
        {
            var raw = MiniJson.Parse(json);
            return (T)Build(raw, typeof(T));
        }

        private static void WriteValue(StringBuilder sb, object v, Type type)
        {
            if (v == null) { sb.Append("null"); return; }
            if (type == typeof(string)) { WriteString(sb, (string)v); return; }
            if (type == typeof(bool)) { sb.Append((bool)v ? "true" : "false"); return; }
            if (type.IsEnum) { sb.Append(Convert.ToInt32(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)); return; }
            if (type == typeof(decimal)) { sb.Append(((decimal)v).ToString(CultureInfo.InvariantCulture)); return; }
            if (type.IsPrimitive) { sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture)); return; }
            if (type.IsArray)
            {
                var arr = (Array)v; var elem = type.GetElementType();
                sb.Append('[');
                for (var i = 0; i < arr.Length; i++) { if (i > 0) sb.Append(','); WriteValue(sb, arr.GetValue(i), elem); }
                sb.Append(']'); return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elem = type.GetGenericArguments()[0]; var first = true;
                sb.Append('[');
                foreach (var item in (IEnumerable)v) { if (!first) sb.Append(','); first = false; WriteValue(sb, item, elem); }
                sb.Append(']'); return;
            }
            sb.Append('{'); var firstField = true;
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!firstField) sb.Append(','); firstField = false;
                WriteString(sb, f.Name); sb.Append(':'); WriteValue(sb, f.GetValue(v), f.FieldType);
            }
            sb.Append('}');
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        private static object Build(object raw, Type type)
        {
            if (raw == null) return type.IsValueType ? Activator.CreateInstance(type) : null;
            if (type == typeof(string)) return raw as string ?? Convert.ToString(raw, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return (bool)raw;
            if (type.IsEnum) return Enum.ToObject(type, (int)(decimal)raw);
            if (type == typeof(decimal)) return (decimal)raw;
            if (type == typeof(int)) return (int)(decimal)raw;
            if (type == typeof(long)) return (long)(decimal)raw;
            if (type == typeof(double)) return (double)(decimal)raw;
            if (type == typeof(float)) return (float)(decimal)raw;
            if (type.IsArray)
            {
                var list = (List<object>)raw; var elem = type.GetElementType();
                var arr = Array.CreateInstance(elem, list.Count);
                for (var i = 0; i < list.Count; i++) arr.SetValue(Build(list[i], elem), i);
                return arr;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elem = type.GetGenericArguments()[0];
                var res = (IList)Activator.CreateInstance(type);
                foreach (var item in (List<object>)raw) res.Add(Build(item, elem));
                return res;
            }
            var dict = (Dictionary<string, object>)raw;
            var obj = Activator.CreateInstance(type);
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object val;
                if (dict.TryGetValue(f.Name, out val)) f.SetValue(obj, Build(val, f.FieldType));
            }
            return obj;
        }
    }
}

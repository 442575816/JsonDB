// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace JsonDB;

public static partial class JSON
{
    public static Func<JsonNode, bool> Eq(string key, object value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                if (node.Get<object>(key)?.Equals(value) ?? false)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Eq<T>(string key, T value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                if (node.Get<T>(key)?.Equals(value) ?? false)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Eq(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                if (node.Get<string>(key)?.Equals(value, StringComparison.Ordinal) ?? false)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Ne(string key, object value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                if (!node.Get<object>(key)?.Equals(value) ?? true)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Ne<T>(string key, T value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                if (!node.Get<T>(key)?.Equals(value) ?? true)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Ne(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                if (!node.Get<string>(key)?.Equals(value, StringComparison.Ordinal) ?? true)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Lt(string key, object value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) < 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Lt<T>(string key, T value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<T>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) < 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Lt(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<string>(key);

                return string.CompareOrdinal(v, value) < 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Lte(string key, object value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) <= 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Lte<T>(string key, T value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<T>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) <= 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Lte(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<string>(key);

                return string.CompareOrdinal(v, value) <= 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Gt(string key, object value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) > 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Gt<T>(string key, T value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<T>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) > 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Gt(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<string>(key);

                return string.CompareOrdinal(v, value) > 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Gte(string key, object value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) >= 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Gte<T>(string key, T value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<T>(key);
                if (v is not IComparable comparable)
                {
                    return false;
                }

                return comparable.CompareTo(value) >= 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Gte(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<string>(key);

                return string.CompareOrdinal(v, value) >= 0;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Like(string key, string value)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<string>(key);
                if (v != null)
                {
                    return v.IndexOf(value, StringComparison.Ordinal) != -1;
                }
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> Null(string key)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                return v == null;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> NotNull(string key)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                return v != null;
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> In(string key, IEnumerable<object> enumerable)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<object>(key);
                return enumerable.Any(e => e.Equals(v));
            }
            return false;
        };
    }

    public static Func<JsonNode, bool> In(string key, IEnumerable<string> enumerable)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<string>(key);
                return enumerable.Any(e => e.Equals(v, StringComparison.Ordinal));
            }
            return false;
        };
    }

    public static Func<JsonNode, int> Len(string key)
    {
        return node =>
        {
            if (node.NodeType == NodeType.Object)
            {
                var v = node.Get<JsonNode>(key);
                if (v != null)
                {
                    return v.Count;
                }
            }
            return 0;
        };
    }

    public static Func<JsonNode, bool> And(params Func<JsonNode, bool>[] funcs)
    {
        return node =>
        {
            foreach (var func in funcs)
            {
                if (!func(node))
                {
                    return false;
                }
            }

            return true;
        };
    }

    public static Func<JsonNode, bool> Or(params Func<JsonNode, bool>[] funcs)
    {
        return node =>
        {
            foreach (var func in funcs)
            {
                if (func(node))
                {
                    return true;
                }
            }

            return false;
        };
    }
}

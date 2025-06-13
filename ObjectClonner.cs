using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace AngelDB
{
    public static class ObjectConverter
    {
        public static object CreateDictionaryOrListFromObject(object originalObject)
        {
            if (originalObject is System.Collections.IEnumerable enumerable && !(originalObject is string))
            {
                var list = new List<Dictionary<string, object>>();

                foreach (var item in enumerable)
                {
                    list.Add(ConvertObjectToDictionary(item));
                }

                return list;
            }
            else
            {
                return ConvertObjectToDictionary(originalObject);
            }
        }

        private static Dictionary<string, object> ConvertObjectToDictionary(object obj)
        {
            Dictionary<string, object> dictionary = new();

            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead) continue;

                object value;
                try
                {
                    value = property.GetValue(obj);
                }
                catch
                {
                    continue;
                }

                if (value == null)
                {
                    dictionary[property.Name] = null;
                    continue;
                }

                Type type = value.GetType();

                if (type == typeof(string) || type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime))
                {
                    dictionary[property.Name] = value;
                }
                else
                {
                    // Es un objeto complejo o colección
                    string serializedValue = JsonConvert.SerializeObject(value, Formatting.Indented);
                    dictionary[property.Name] = serializedValue;
                }
            }

            return dictionary;
        }
    }
}

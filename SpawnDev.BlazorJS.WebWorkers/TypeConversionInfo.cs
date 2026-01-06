using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpawnDev.BlazorJS.WebWorkers
{
    public class TypeConversionInfo
    {
        public Type ReturnType { get; private set; }
        public bool useIJSWrapperReader { get; private set; }
        public bool usePropertyReader { get; private set; }
        public bool useJSObjectReader { get; private set; }
        public bool useIterationReader { get; private set; }
        public bool useDictionaryReader { get; private set; }
        public bool useInterfaceProxy { get; private set; }
        public bool useTaskReader { get; private set; }
        public bool isDispatchProxy { get; private set; }
        public bool isIJSObject { get; private set; }
        public bool useDefaultReader { get; private set; }
        public Type? ElementType { get; set; } = null;
        public Type? DictionaryKeyType { get; set; } = null;
        public Type? DictionaryValueType { get; set; } = null;
        public PropertyInfo[] ClassProperties { get; set; } = new PropertyInfo[0];
        public Dictionary<string, PropertyInfo> returnTypeProperties { get; private set; } = new Dictionary<string, PropertyInfo>();
        public bool IsTransferable => TransferableAttribute != null;
        public bool IsTransferableOrHasTransferableDescendants => IsTransferable || HasTransferableDescendants;
        public bool IsTransferRequired => TransferableAttribute?.TransferRequired == true;
        public TransferableAttribute? TransferableAttribute { get; private set; }
        public WorkerTransferAttribute? WorkerTransferAttribute { get; private set; }
        public bool? WorkerTransferDesired => WorkerTransferAttribute?.Transfer;
        static List<Type> IgnoreInterfaces = new List<Type> {
            typeof(IJSInProcessObjectReference),
            typeof(IJSObjectReference),
            typeof(IJSStreamReference),
        };
        static JsonNamingPolicy JsonNamingPolicy = JsonNamingPolicy.CamelCase;
        static string GetPropertyJSName(PropertyInfo prop)
        {
            var jsonPropNameAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>(true);
            if (jsonPropNameAttr != null) return jsonPropNameAttr.Name;
            string propName = prop.Name;
            if (string.IsNullOrEmpty(propName)) return propName;
            return JsonNamingPolicy.ConvertName(propName);
        }
        internal TypeConversionInfo(Type returnType)
        {
#if DEBUG && false
                Console.WriteLine($"TypeConversionInfo loading: {returnType.Name}");
#endif
            if (returnType == null) throw new Exception("Invalid Return Type");
            ReturnType = returnType;
        }
        public HashSet<Type> SubTypes { get; } = new HashSet<Type>();
        internal bool ProcessStarted { get; private set; } = false;
        internal void Process()
        {
            if (ProcessStarted) return;
            ProcessStarted = true;
            var returnType = ReturnType;
            if (typeof(Type).IsAssignableFrom(returnType))
            {
                useDefaultReader = true;
                return;
            }
            else if (returnType.IsValueType || returnType == typeof(string))
            {
                useDefaultReader = true;
                return;
            }
            else if (returnType.IsInterface && !IgnoreInterfaces.Contains(returnType))
            {
                useJSObjectReader = false;
                useDefaultReader = false;
                useInterfaceProxy = true;
                return;
            }
            else if (typeof(Callback).IsAssignableFrom(returnType))
            {
                useDefaultReader = true;
                return;
            }
            else if (typeof(DotNetObjectReference).IsAssignableFrom(returnType))
            {
                useDefaultReader = true;
                return;
            }
            ElementType = returnType.GetEnumerableElementType();
            if (ElementType != null)
            {
                // IEnumerable
                SubTypes.Add(ElementType);
                var elementTypeConversionInfo = GetTypeConversionInfo(ElementType);
                useIterationReader = !elementTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(ElementType);
                if (useIterationReader) return;
            }
            else if (typeof(IDictionary).IsAssignableFrom(returnType))
            {
                Type[] arguments = returnType.GetGenericArguments();
                if (arguments.Length == 2)
                {
                    DictionaryKeyType = arguments[0];
                    DictionaryValueType = arguments[1];
                    SubTypes.Add(DictionaryKeyType);
                    SubTypes.Add(DictionaryValueType);
                    var keyTypeConversionInfo = GetTypeConversionInfo(DictionaryKeyType);
                    useDictionaryReader = !keyTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(DictionaryKeyType);
                    if (useDictionaryReader) return;
                    var valueTypeConversionInfo = GetTypeConversionInfo(DictionaryValueType);
                    useDictionaryReader = !valueTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(DictionaryValueType);
                    if (useDictionaryReader) return;
                }
            }
            else if (typeof(IJSObjectProxy).IsAssignableFrom(returnType))
            {
                useJSObjectReader = false;
                useDefaultReader = false;
                isIJSObject = true;
                return;
            }
            else if (typeof(DispatchProxy).IsAssignableFrom(returnType))
            {
                useJSObjectReader = false;
                useDefaultReader = true;
                isDispatchProxy = true;
                return;
            }
            else if (typeof(Delegate).IsAssignableFrom(returnType))
            {
                // this type will likely fail, but is a class... so drop it here
            }
            else if (returnType.IsClass)
            {
                WorkerTransferAttribute = returnType.GetCustomAttribute<WorkerTransferAttribute>();
                if (typeof(JSObject).IsAssignableFrom(returnType))
                {
                    TransferableAttribute = TransferableAttribute.GetTransferable(returnType);
                    useJSObjectReader = true;
                    if (!IsTransferable)
                    {
                        ClassProperties = returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        foreach (var prop in ClassProperties)
                        {
                            if (Attribute.IsDefined(prop, typeof(JsonIgnoreAttribute))) continue;
                            var propJSName = GetPropertyJSName(prop);
                            returnTypeProperties[propJSName] = prop;
                            SubTypes.Add(prop.PropertyType);
                        }
                    }
                    return;
                }
                else if (returnType.IsTask())
                {
                    useJSObjectReader = false;
                    useDefaultReader = false;
                    useTaskReader = true;
                    return;
                }
                else
                {
                    // class
                    // check if the class types requires per property import
                    ClassProperties = returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (var prop in ClassProperties)
                    {
                        if (Attribute.IsDefined(prop, typeof(JsonIgnoreAttribute))) continue;
                        var propJSName = GetPropertyJSName(prop);
                        returnTypeProperties[propJSName] = prop;
                        SubTypes.Add(prop.PropertyType);
                        var propertyTypeConversionInfo = GetTypeConversionInfo(prop.PropertyType);
                        if (!propertyTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(prop.PropertyType))
                        {
                            usePropertyReader = true;
                        }
                    }
                    if (usePropertyReader) return;
                }
            }
            useDefaultReader = true;
        }
        public HashSet<Type> AllSubTypes => AllSubTypesLazy!.Value;
        Lazy<HashSet<Type>>? AllSubTypesLazy = null;
        public Dictionary<Type, TransferableAttribute> TransferableSubTypes => TransferableSubTypesLazy!.Value;
        Lazy<Dictionary<Type, TransferableAttribute>>? TransferableSubTypesLazy = null;

        public bool HasTransferableDescendants => AllTransferableSubTypes.Any();

        public bool HasTransferableChildren => TransferableSubTypes.Any();

        public Dictionary<Type, TransferableAttribute> AllTransferableSubTypes => AllTransferableSubTypesLazy!.Value;
        Lazy<Dictionary<Type, TransferableAttribute>>? AllTransferableSubTypesLazy = null;
        public List<object> GetTransferablePropertyValues(object? obj, bool? includeOptionalTransferables, int maxDepth = 1)
        {
            var ret = GetTransferablePropertyValues(obj, includeOptionalTransferables, 0, maxDepth);
            return ret;
        }
        List<object> GetTransferablePropertyValues(object? obj, bool? includeOptionalTransferables, int depth, int maxDepth)
        {
            var ret = new List<object>();
            if (obj != null && IsTransferableOrHasTransferableDescendants)
            {
                if (includeOptionalTransferables == null && WorkerTransferDesired != null)
                {
                    includeOptionalTransferables = WorkerTransferDesired;
                }
                if (IsTransferable)
                {
                    if (includeOptionalTransferables == true || IsTransferRequired)
                    {
                        ret.Add(obj);
                    }
                }
                else if (depth <= maxDepth)
                {
                    if (usePropertyReader)
                    {
                        foreach (var kvp in returnTypeProperties)
                        {
                            var prop = kvp.Value;
                            if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                            {
                                continue;
                            }
                            if (!prop.PropertyType.IsClass) continue;
                            if (typeof(IJSInProcessObjectReference) == prop.PropertyType) continue;
                            var transferAttr = (WorkerTransferAttribute?)prop.GetCustomAttribute(typeof(WorkerTransferAttribute), false);
                            if (transferAttr != null && !transferAttr.Transfer)
                            {
                                // this property has been marked as non-transferable
                                continue;
                            }
                            object? propVal = null;
                            try
                            {
                                propVal = prop.GetValue(obj);
                            }
                            catch { }
                            if (propVal == null) continue;
                            var conversionInfo = GetTypeConversionInfo(prop.PropertyType);
                            var propertyTransferables = conversionInfo.GetTransferablePropertyValues(propVal, includeOptionalTransferables, depth + 1, maxDepth);
                            ret.AddRange(propertyTransferables);
                        }
                    }
                    else if (useJSObjectReader)
                    {
                        foreach (var kvp in returnTypeProperties)
                        {
                            var prop = kvp.Value;
                            if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                            {
                                continue;
                            }
                            if (!prop.PropertyType.IsClass) continue;
                            if (typeof(IJSInProcessObjectReference) == prop.PropertyType) continue;
                            var transferAttr = (WorkerTransferAttribute?)prop.GetCustomAttribute(typeof(WorkerTransferAttribute), false);
                            if (transferAttr != null && !transferAttr.Transfer)
                            {
                                // this property has been marked as non-transferable
                                continue;
                            }
                            object? propVal = null;
                            try
                            {
                                propVal = prop.GetValue(obj);
                            }
                            catch
                            {
                                continue;
                            }
                            if (propVal == null) continue;
                            var conversionInfo = GetTypeConversionInfo(prop.PropertyType);
                            var propertyTransferables = conversionInfo.GetTransferablePropertyValues(propVal, includeOptionalTransferables, depth + 1, maxDepth);
                            ret.AddRange(propertyTransferables);
                        }
                    }
                    else if (useIterationReader && ElementType != null && obj is IEnumerable enumarable)
                    {
                        var conversionInfo = GetTypeConversionInfo(ElementType);
                        foreach (var ival in enumarable)
                        {
                            if (ival == null) continue;
                            var propertyTransferables = conversionInfo.GetTransferablePropertyValues(ival, includeOptionalTransferables, depth + 1, maxDepth);
                            ret.AddRange(propertyTransferables);
                        }
                    }
                    else if (useDictionaryReader)
                    {
                        if (DictionaryKeyType != null && DictionaryValueType != null)
                        {
                            var keyTypeConversionInfo = GetTypeConversionInfo(DictionaryKeyType);
                            var valueTypeConversionInfo = GetTypeConversionInfo(DictionaryValueType);
                            if (obj is System.Collections.IDictionary dict)
                            {
                                foreach (var key in dict.Keys)
                                {
                                    var value = dict[key];
                                    if (key != null)
                                    {
                                        var propertyTransferables = keyTypeConversionInfo.GetTransferablePropertyValues(key, includeOptionalTransferables, depth + 1, maxDepth);
                                        ret.AddRange(propertyTransferables);
                                    }
                                    if (value != null)
                                    {
                                        var propertyTransferables = valueTypeConversionInfo.GetTransferablePropertyValues(value, includeOptionalTransferables, depth + 1, maxDepth);
                                        ret.AddRange(propertyTransferables);
                                    }
                                }
                            }
                        }
                    }
                    else if (useInterfaceProxy)
                    {

                    }
                }
            }
            return ret;
        }
        static Dictionary<Type, TypeConversionInfo> _conversionInfo = new Dictionary<Type, TypeConversionInfo>();
        public static TypeConversionInfo GetTypeConversionInfo(Type type)
        {
            if (_conversionInfo.TryGetValue(type, out var conversionInfo))
            {
                return conversionInfo;
            }
            conversionInfo = new TypeConversionInfo(type);
            _conversionInfo[type] = conversionInfo;
            try
            {
                conversionInfo.Process();
                conversionInfo.AllSubTypesLazy = new Lazy<HashSet<Type>>(() =>
                {
                    var cis = new Dictionary<Type, TypeConversionInfo?>();
                    var subTypes = conversionInfo.SubTypes.ToList();
                    foreach(var subType in subTypes)
                    {
                        cis[subType] = null;
                    }
                    KeyValuePair<Type, TypeConversionInfo?> nextType = default;
                    while ((nextType = cis.FirstOrDefault(o => o.Value == null)).Key != default)
                    {
                        var nType = nextType.Key;
                        var nValue = nType == type ? conversionInfo : GetTypeConversionInfo(nType);
                        cis[nType] = nValue;
                        foreach(var subType in nValue.SubTypes)
                        {
                            if (!cis.ContainsKey(subType))
                            {
                                cis[subType] = null;
                            }
                        }
                    }
                    return cis.Keys.ToHashSet();
                });
                conversionInfo.TransferableSubTypesLazy = new Lazy<Dictionary<Type, TransferableAttribute>>(() =>
                {
                    var cis = new Dictionary<Type, TypeConversionInfo?>();
                    var subTypes = conversionInfo.SubTypes.ToList();
                    foreach (var subType in subTypes)
                    {
                        cis[subType] = null;
                    }
                    KeyValuePair<Type, TypeConversionInfo?> nextType = default;
                    while ((nextType = cis.FirstOrDefault(o => o.Value == null)).Key != default)
                    {
                        var nType = nextType.Key;
                        var nValue = nType == type ? conversionInfo : GetTypeConversionInfo(nType);
                        cis[nType] = nValue;
                    }
                    return cis.Where(o => o.Value?.TransferableAttribute != null).ToDictionary(o => o.Key, o => o.Value!.TransferableAttribute!);
                });
                conversionInfo.AllTransferableSubTypesLazy = new Lazy<Dictionary<Type, TransferableAttribute>>(() =>
                {
                    var cis = new Dictionary<Type, TypeConversionInfo?>();
                    var subTypes = conversionInfo.SubTypes.ToList();
                    foreach (var subType in subTypes)
                    {
                        cis[subType] = null;
                    }
                    KeyValuePair<Type, TypeConversionInfo?> nextType = default;
                    while ((nextType = cis.FirstOrDefault(o => o.Value == null)).Key != default)
                    {
                        var nType = nextType.Key;
                        var nValue = nType == type ? conversionInfo : GetTypeConversionInfo(nType);
                        cis[nType] = nValue;
                        foreach (var subType in nValue.SubTypes)
                        {
                            if (!cis.ContainsKey(subType))
                            {
                                cis[subType] = null;
                            }
                        }
                    }
                    return cis.Where(o => o.Value?.TransferableAttribute != null).ToDictionary(o => o.Key, o => o.Value!.TransferableAttribute!);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("TypeConversionInfo error: " + ex.ToString());
            }
            return conversionInfo;
        }
    }
}

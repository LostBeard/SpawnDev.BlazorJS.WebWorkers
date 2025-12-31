using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.JSObjects.WebRTC;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpawnDev.BlazorJS.WebWorkers
{
    public class TypeConversionInfo
    {
        public static IReadOnlyCollection<Type> TransferableTypes { get; } = new List<Type> {
            typeof(ArrayBuffer),
            typeof(MessagePort),
            typeof(ReadableStream),
            typeof(WritableStream),
            typeof(TransformStream),
            typeof(AudioData),
            typeof(ImageBitmap),
            typeof(VideoFrame),
            typeof(OffscreenCanvas),
            typeof(RTCDataChannel),
        }.AsReadOnly();
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
        public bool IsTransferable { get; private set; }

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
        internal void Process()
        {
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
                IsTransferable = false;
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
            else if (returnType.IsArray && returnType.HasElementType)
            {
                // array
                // check if the element type requires per element import
                ElementType = returnType.GetElementType();
                if (ElementType != null)
                {
                    var elementTypeConversionInfo = GetTypeConversionInfo(ElementType);
                    useIterationReader = !elementTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(ElementType);
                    if (useIterationReader) return;
                }
            }
            else if (typeof(System.Collections.IDictionary).IsAssignableFrom(returnType))
            {
                Type[] arguments = returnType.GetGenericArguments();
                if (arguments.Length == 2)
                {
                    DictionaryKeyType = arguments[0];
                    DictionaryValueType = arguments[1];
                    var keyTypeConversionInfo = GetTypeConversionInfo(DictionaryKeyType);
                    useDictionaryReader = !keyTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(DictionaryKeyType);
                    var valueTypeConversionInfo = GetTypeConversionInfo(DictionaryValueType);
                    useDictionaryReader = !valueTypeConversionInfo.useDefaultReader || typeof(IJSInProcessObjectReference).IsAssignableFrom(DictionaryValueType);
                    if (useDictionaryReader) return;
                }
            }
            else if (typeof(IJSObjectProxy).IsAssignableFrom(returnType))
            {
                IsTransferable = false;
                useJSObjectReader = false;
                useDefaultReader = false;
                isIJSObject = true;
                return;
            }
            else if (typeof(DispatchProxy).IsAssignableFrom(returnType))
            {
                IsTransferable = false;
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
                if (typeof(JSObject).IsAssignableFrom(returnType))
                {
                    IsTransferable = TransferableTypes.Contains(returnType);
                    useJSObjectReader = true;
                    if (!IsTransferable)
                    {
                        ClassProperties = returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        foreach (var prop in ClassProperties)
                        {
                            if (Attribute.IsDefined(prop, typeof(JsonIgnoreAttribute))) continue;
                            var propJSName = GetPropertyJSName(prop);
                            returnTypeProperties[propJSName] = prop;
                        }
                    }
                    return;
                }
                else if (returnType.IsTask())
                {
                    IsTransferable = false;
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
        public object[] GetTransferablePropertyValues(object? obj)
        {
            var ret = new List<object>();
            if (obj != null)
            {
                if (IsTransferable)
                {
                    ret.Add(obj);
                }
                else if (obj is TypedArray typedArray)
                {
                    ret.Add(typedArray.Buffer);
                }
                else if (usePropertyReader)
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
                        var propertyTransferables = conversionInfo.GetTransferablePropertyValues(propVal);
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
                        var propertyTransferables = conversionInfo.GetTransferablePropertyValues(propVal);
                        ret.AddRange(propertyTransferables);
                    }
                }
                else if (useIterationReader && ElementType != null && obj is IEnumerable enumarable)
                {
                    var conversionInfo = GetTypeConversionInfo(ElementType);
                    foreach (var ival in enumarable)
                    {
                        if (ival == null) continue;
                        var propertyTransferables = conversionInfo.GetTransferablePropertyValues(ival);
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
                                    var propertyTransferables = keyTypeConversionInfo.GetTransferablePropertyValues(key);
                                    ret.AddRange(propertyTransferables);
                                }
                                if (value != null)
                                {
                                    var propertyTransferables = valueTypeConversionInfo.GetTransferablePropertyValues(value);
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
            return ret.ToArray();
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("TypeConversionInfo error: " + ex.ToString());
            }
            return conversionInfo;
        }
    }
}

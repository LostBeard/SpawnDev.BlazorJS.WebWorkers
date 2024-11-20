using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// On creation, SerializableMethodInfoSlim extracts the information needed to allow serialization, deserialization, and resolving of a MethodInfo.<br />
    /// </summary>
    public class SerializableMethodInfo
    {
        [JsonIgnore]
        public MethodInfo? MethodInfo
        {
            get
            {
                if (!Resolved) Resolve();
                return _MethodInfo;
            }
        }
        private MethodInfo? _MethodInfo = null;

        [JsonIgnore]
        public ConstructorInfo? ConstructorInfo
        {
            get
            {
                if (!Resolved) Resolve();
                return _ConstructorInfo;
            }
        }
        private ConstructorInfo? _ConstructorInfo = null;

        private bool Resolved = false;
        /// <summary>
        /// MethodInfo.ReflectedType type name
        /// </summary>
        public string ReflectedTypeName { get; init; } = "";
        /// <summary>
        /// MethodInfo.DeclaringType type name
        /// </summary>
        public string DeclaringTypeName { get; init; } = "";
        /// <summary>
        /// Returns true if the 
        /// </summary>
        public bool IsConstructor => MethodName == ".ctor";
        /// <summary>
        /// MethodInfo.Name
        /// </summary>
        public string MethodName { get; init; } = "";
        /// <summary>
        /// methodInfo.GetParameters() type names
        /// </summary>
        public List<string> ParameterTypes { get; init; } = new List<string>();
        /// <summary>
        /// MethodInfo.ReturnType type name
        /// </summary>
        public string ReturnType { get; init; } = "";
        /// <summary>
        /// methodInfo.GetGenericArguments() type names
        /// </summary>
        public List<string> GenericArguments { get; init; } = new List<string>();
        /// <summary>
        /// Deserialization constructor
        /// </summary>
        public SerializableMethodInfo() { }
        /// <summary>
        /// Creates a new instance of SerializableMethodInfo that represents the passed in MethodBase
        /// </summary>
        public SerializableMethodInfo(MethodBase methodBase)
        {
            if (methodBase is ConstructorInfo constructorInfo)
            {
                var mi = constructorInfo;
                if (constructorInfo.ReflectedType == null) throw new Exception("Cannot serialize ConstructorInfo without ReflectedType");
                MethodName = mi.Name;
                ReflectedTypeName = GetTypeName(constructorInfo.ReflectedType);
                DeclaringTypeName = GetTypeName(constructorInfo.DeclaringType);
                ReturnType = GetTypeName(typeof(void));
                ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
                _ConstructorInfo = constructorInfo;
                Resolved = true;
            }
            else if (methodBase is MethodInfo methodInfo)
            {
                var mi = methodInfo;
                if (methodInfo.ReflectedType == null) throw new Exception("Cannot serialize MethodInfo without ReflectedType");
                if (methodInfo.IsConstructedGenericMethod)
                {
                    GenericArguments = methodInfo.GetGenericArguments().Select(o => GetTypeName(o)).ToList();
                    mi = methodInfo.GetGenericMethodDefinition();
                }
                MethodName = mi.Name;
                ReflectedTypeName = GetTypeName(methodInfo.ReflectedType);
                DeclaringTypeName = GetTypeName(methodInfo.DeclaringType);
                ReturnType = GetTypeName(mi.ReturnType);
                ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
                _MethodInfo = methodInfo;
                Resolved = true;
            }
            else
            {
                throw new Exception($"MethodBase type not supported: {methodBase.GetType().Name}");
            }
        }
        /// <summary>
        /// Deserializes SerializableMethodInfo instance from string using System.Text.Json<br />
        /// PropertyNameCaseInsensitive = true is used in deserialization
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static SerializableMethodInfo? FromString(string json)
        {
            var ret = string.IsNullOrEmpty(json) || !json.StartsWith("{") ? null : JsonSerializer.Deserialize<SerializableMethodInfo>(json, DefaultJsonSerializerOptions);
            return ret;
        }
        /// <summary>
        /// Serializes SerializableMethodInfo to a string using System.Text.Json
        /// </summary>
        /// <returns></returns>
        public override string ToString() => JsonSerializer.Serialize(this);

        internal static JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        static string GetTypeName(Type? type)
        {
            if (type == null) return "";
            return !string.IsNullOrEmpty(type.AssemblyQualifiedName) ? type.AssemblyQualifiedName : (!string.IsNullOrEmpty(type.FullName) ? type.FullName : type.Name);
        }
        void Resolve()
        {
            if (Resolved) return;
            Resolved = true;
            if (IsConstructor)
            {
                ConstructorInfo? constructorInfo = null;
                var reflectedType = TypeExtensions.GetType(ReflectedTypeName);
                if (reflectedType == null)
                {
                    // Reflected type not found
                    return;
                }
                var parameterTypesDeserialized = ParameterTypes.Select(o => TypeExtensions.GetType(o)).ToList();
                var constructors = reflectedType.GetConstructors();
                foreach (var ctor in constructors)
                {
                    var parameterInfos = ctor.GetParameters();
                    if (parameterTypesDeserialized.Count != parameterInfos.Length) continue;
                    for (var i = 0; i < parameterInfos.Length; i++)
                    {
                        var parameterType = parameterInfos[i].ParameterType;
                        if (parameterTypesDeserialized[i] != parameterType)
                        {
                            continue;
                        }
                    }
                    constructorInfo = ctor;
                    break;
                }
                _ConstructorInfo = constructorInfo;
            }
            else
            {
                MethodInfo? methodInfo = null;
                var reflectedType = TypeExtensions.GetType(ReflectedTypeName);
                if (reflectedType == null)
                {
                    // Reflected type not found
                    return;
                }
                var methodsWithName = reflectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(o => o.Name == MethodName);
                if (!methodsWithName.Any())
                {
                    // No method found with this MethodName found in ReflectedType
                    return;
                }
                MethodInfo? mi = null;
                foreach (var method in methodsWithName)
                {
                    var msi = new SerializableMethodInfo(method);
                    if (msi.ReturnType == ReturnType && msi.ParameterTypes.SequenceEqual(ParameterTypes))
                    {
                        mi = method;
                        break;
                    }
                }
                if (mi == null)
                {
                    // No method found that matches the base method signature
                    return;
                }
                if (mi.IsGenericMethod)
                {
                    if (GenericArguments == null || !GenericArguments.Any())
                    {
                        // Generics information in GenericArguments is missing. Resolve not possible.
                        return;
                    }
                    var genericTypes = new Type[GenericArguments.Count];
                    for (var i = 0; i < genericTypes.Length; i++)
                    {
                        var gTypeName = GenericArguments[i];
                        var gType = TypeExtensions.GetType(gTypeName);
                        if (gType == null)
                        {
                            // One of the generic types needed to make the generic method was not found
                            return;
                        }
                        genericTypes[i] = gType;
                    }
                    methodInfo = mi.MakeGenericMethod(genericTypes);
                }
                else
                {
                    methodInfo = mi;
                }
                _MethodInfo = methodInfo;
            }
        }
        static Dictionary<MethodBase, string> SerializedMethodInfos = new Dictionary<MethodBase, string>();
        public static bool UseCache { get; set; } = false;
        /// <summary>
        /// Converts a MethodInfo instance into a string
        /// </summary>
        public static string SerializeMethodInfo(MethodBase methodBase)
        {
            string info;
            if (UseCache)
            {
                if (SerializedMethodInfos.TryGetValue(methodBase, out info))
                {
                    return info;
                }
                info = new SerializableMethodInfo(methodBase).ToString();
                SerializedMethodInfos[methodBase] = info;
                return info;
            }
            info = new SerializableMethodInfo(methodBase).ToString();
            return info;
        }
        /// <summary>
        /// Converts a MethodInfo that has been serialized using SerializeMethodInfo into a MethodInfo if serialization is successful or a null otherwise.
        /// </summary>
        public static MethodInfo? DeserializeMethodInfo(string serializableMethodInfoJson)
        {
            var tmp = FromString(serializableMethodInfoJson);
            return tmp == null ? null : tmp.MethodInfo;
        }
        /// <summary>
        /// Converts a MethodInfo that has been serialized using SerializeMethodInfo into a ConstructorInfo if serialization is successful or a null otherwise.
        /// </summary>
        public static ConstructorInfo? DeserializeConstructorInfoInfo(string serializableMethodInfoJson)
        {
            var tmp = FromString(serializableMethodInfoJson);
            return tmp == null ? null : tmp.ConstructorInfo;
        }
    }
}

﻿//using System.Reflection;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace SpawnDev.BlazorJS.WebWorkers
//{
//    public class SerializableMethodBase
//    {
//        [JsonIgnore]
//        public MethodBase? MethodInfo
//        {
//            get
//            {
//                if (!Resolved) Resolve();
//                return _MethodInfo;
//            }
//        }
//        private MethodBase? _MethodInfo = null;
//        private bool Resolved = false;
//        /// <summary>
//        /// MethodInfo.ReflectedType type name
//        /// </summary>
//        public string ReflectedTypeName { get; init; } = "";
//        /// <summary>
//        /// MethodInfo.DeclaringType type name
//        /// </summary>
//        public string DeclaringTypeName { get; init; } = "";
//        /// <summary>
//        /// MethodInfo.Name
//        /// </summary>
//        public string MethodName { get; init; } = "";
//        /// <summary>
//        /// methodInfo.GetParameters() type names
//        /// </summary>
//        public List<string> ParameterTypes { get; init; } = new List<string>();
//        /// <summary>
//        /// MethodInfo.ReturnType type name
//        /// </summary>
//        public string ReturnType { get; init; } = "";
//        /// <summary>
//        /// methodInfo.GetGenericArguments() type names
//        /// </summary>
//        public List<string> GenericArguments { get; init; } = new List<string>();
//        /// <summary>
//        /// Deserialization constructor
//        /// </summary>
//        public SerializableMethodBase() { }
//        /// <summary>
//        /// Creates a new instance of SerializableMethodInfo that represents
//        /// </summary>
//        /// <param name="methodInfo"></param>
//        /// <exception cref="Exception"></exception>
//        public SerializableMethodBase(MethodBase methodInfo)
//        {
//            var mi = methodInfo;
//            if (methodInfo.ReflectedType == null) throw new Exception("Cannot serialize MethodInfo without ReflectedType");
//            if (methodInfo.IsConstructedGenericMethod)
//            {
//                GenericArguments = methodInfo.GetGenericArguments().Select(o => GetTypeName(o)).ToList();
//                mi = ((MethodInfo)methodInfo).GetGenericMethodDefinition();
//            }
//            MethodName = mi.Name;
//            ReflectedTypeName = GetTypeName(methodInfo.ReflectedType);
//            DeclaringTypeName = GetTypeName(methodInfo.DeclaringType);
//            ReturnType = methodInfo is MethodInfo mit ? GetTypeName(mit.ReturnType) : "";
//            ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
//            _MethodInfo = methodInfo;
//            Resolved = true;
//        }
//        public SerializableMethodBase(Delegate methodDelegate)
//        {
//            var methodInfo = methodDelegate.Method;
//            var mi = methodInfo;
//            if (methodInfo.ReflectedType == null) throw new Exception("Cannot serialize MethodInfo without ReflectedType");
//            if (methodInfo.IsConstructedGenericMethod)
//            {
//                GenericArguments = methodInfo.GetGenericArguments().Select(o => GetTypeName(o)).ToList();
//                mi = methodInfo.GetGenericMethodDefinition();
//            }
//            MethodName = mi.Name;
//            ReflectedTypeName = GetTypeName(methodInfo.ReflectedType);
//            DeclaringTypeName = GetTypeName(methodInfo.DeclaringType);
//            ReturnType = GetTypeName(mi.ReturnType);
//            ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
//            _MethodInfo = methodInfo;
//            Resolved = true;
//        }
//        /// <summary>
//        /// Deserializes SerializableMethodInfo instance from string using System.Text.Json<br />
//        /// PropertyNameCaseInsensitive = true is used in deserialization
//        /// </summary>
//        /// <param name="json"></param>
//        /// <returns></returns>
//        public static SerializableMethodInfo? FromString(string json)
//        {
//            var ret = string.IsNullOrEmpty(json) || !json.StartsWith("{") ? null : JsonSerializer.Deserialize<SerializableMethodInfo>(json, DefaultJsonSerializerOptions);
//            return ret;
//        }
//        /// <summary>
//        /// Serializes SerializableMethodInfo to a string using System.Text.Json
//        /// </summary>
//        /// <returns></returns>
//        public override string ToString() => JsonSerializer.Serialize(this);

//        internal static JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

//        static string GetTypeName(Type? type)
//        {
//            if (type == null) return "";
//            return !string.IsNullOrEmpty(type.AssemblyQualifiedName) ? type.AssemblyQualifiedName : (!string.IsNullOrEmpty(type.FullName) ? type.FullName : type.Name);
//        }

//        void Resolve()
//        {
//            MethodInfo? methodInfo = null;
//            if (Resolved) return;
//            Resolved = true;
//            methodInfo = null;
//            var reflectedType = TypeExtensions.GetType(ReflectedTypeName);
//            if (reflectedType == null)
//            {
//                // Reflected type not found
//                return;
//            }
//            var methodsWithName = reflectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(o => o.Name == MethodName);
//            if (!methodsWithName.Any())
//            {
//                // No method found with this MethodName found in ReflectedType
//                return;
//            }
//            MethodInfo? mi = null;
//            foreach (var method in methodsWithName)
//            {
//                var msi = new SerializableMethodInfo(method);
//                if (msi.ReturnType == ReturnType && msi.ParameterTypes.SequenceEqual(ParameterTypes))
//                {
//                    mi = method;
//                    break;
//                }
//            }
//            if (mi == null)
//            {
//                // No method found that matches the base method signature
//                return;
//            }
//            if (mi.IsGenericMethod)
//            {
//                if (GenericArguments == null || !GenericArguments.Any())
//                {
//                    // Generics information in GenericArguments is missing. Resolve not possible.
//                    return;
//                }
//                var genericTypes = new Type[GenericArguments.Count];
//                for (var i = 0; i < genericTypes.Length; i++)
//                {
//                    var gTypeName = GenericArguments[i];
//                    var gType = TypeExtensions.GetType(gTypeName);
//                    if (gType == null)
//                    {
//                        // One of the generic types needed to make the generic method was not found
//                        return;
//                    }
//                    genericTypes[i] = gType;
//                }
//                methodInfo = mi.MakeGenericMethod(genericTypes);
//            }
//            else
//            {
//                methodInfo = mi;
//            }
//            _MethodInfo = methodInfo;
//        }
//        /// <summary>
//        /// Converts a MethodInfo instance into a string
//        /// </summary>
//        /// <param name="methodInfo"></param>
//        /// <returns></returns>
//        public static string SerializeMethodInfo(MethodInfo methodInfo) => new SerializableMethodInfo(methodInfo).ToString();
//        public static string SerializeMethodInfo(Delegate methodDeleate) => new SerializableMethodInfo(methodDeleate.Method).ToString();
//        /// <summary>
//        /// Converts a MethodInfo that has been serialized using SerializeMethodInfo into a MethodInfo if serialization is successful or a null otherwise.
//        /// </summary>
//        /// <param name="serializableMethodInfoJson"></param>
//        /// <returns></returns>
//        public static MethodInfo? DeserializeMethodInfo(string serializableMethodInfoJson)
//        {
//            var tmp = FromString(serializableMethodInfoJson);
//            return tmp == null ? null : tmp.MethodInfo;
//        }
//    }
//}

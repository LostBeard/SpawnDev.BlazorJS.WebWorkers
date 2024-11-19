using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// On creation, SerializableConstructorInfoSlim extracts the information needed to allow serialization, deserialization, and resolving of a ConstructorInfo.<br />
    /// </summary>
    public class SerializableConstructorInfo
    {
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
        /// ConstructorInfo.ReflectedType type name
        /// </summary>
        public string ReflectedTypeName { get; init; } = "";
        public List<string> ReflectedTypeGenericTypes { get; init; } = new List<string>();
        /// <summary>
        /// ConstructorInfo.DeclaringType type name
        /// </summary>
        public string DeclaringTypeName { get; init; } = "";
        /// <summary>
        /// constructorInfo.GetParameters() type names
        /// </summary>
        public List<string> ParameterTypes { get; init; } = new List<string>();
        /// <summary>
        /// constructorInfo.GetGenericArguments() type names
        /// </summary>
        public List<string> GenericArguments { get; init; } = new List<string>();
        /// <summary>
        /// Deserialization constructor
        /// </summary>
        public SerializableConstructorInfo() { }
        /// <summary>
        /// Creates a new instance of SerializableConstructorInfo that represents
        /// </summary>
        /// <param name="constructorInfo"></param>
        /// <exception cref="Exception"></exception>
        public SerializableConstructorInfo(ConstructorInfo constructorInfo)
        {
            var mi = constructorInfo;
            if (constructorInfo.ReflectedType == null) throw new Exception("Cannot serialize ConstructorInfo without ReflectedType");
            if (constructorInfo.ReflectedType.IsGenericType)
            {
                var reflectedTypeGenericType = constructorInfo.ReflectedType.GetGenericTypeDefinition();
                var reflectedTypeGenericTypes = constructorInfo.ReflectedType.GetGenericArguments();
                ReflectedTypeGenericTypes = new List<string>();
                ////////
                var art = true;
            }
            if (constructorInfo.IsConstructedGenericMethod)
            {
                GenericArguments = constructorInfo.GetGenericArguments().Select(o => GetTypeName(o)).ToList();
                if (constructorInfo.ReflectedType!.IsGenericType)
                {
                    var genericType = constructorInfo.ReflectedType.GetGenericTypeDefinition();
                    // TODO
                    var nmt = true;
                }
            }
            ReflectedTypeName = GetTypeName(constructorInfo.ReflectedType);
            DeclaringTypeName = GetTypeName(constructorInfo.DeclaringType);
            ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
            _ConstructorInfo = constructorInfo;
            Resolved = true;
        }
        /// <summary>
        /// Deserializes SerializableConstructorInfo instance from string using System.Text.Json<br />
        /// PropertyNameCaseInsensitive = true is used in deserialization
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static SerializableConstructorInfo? FromString(string json)
        {
            var ret = string.IsNullOrEmpty(json) || !json.StartsWith("{") ? null : JsonSerializer.Deserialize<SerializableConstructorInfo>(json, DefaultJsonSerializerOptions);
            return ret;
        }
        /// <summary>
        /// Serializes SerializableConstructorInfo to a string using System.Text.Json
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
            ConstructorInfo? constructorInfo = null;
            if (Resolved) return;
            Resolved = true;
            constructorInfo = null;
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
                for(var i = 0; i < parameterInfos.Length; i++)
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
        /// <summary>
        /// Converts a ConstructorInfo instance into a string
        /// </summary>
        /// <param name="constructorInfo"></param>
        /// <returns></returns>
        public static string SerializeConstructorInfo(ConstructorInfo constructorInfo) => new SerializableConstructorInfo(constructorInfo).ToString();
        /// <summary>
        /// Converts a ConstructorInfo that has been serialized using SerializeConstructorInfo into a ConstructorInfo if serialization is successful or a null otherwise.
        /// </summary>
        /// <param name="serializableConstructorInfoJson"></param>
        /// <returns></returns>
        public static ConstructorInfo? DeserializeConstructorInfo(string serializableConstructorInfoJson)
        {
            var tmp = FromString(serializableConstructorInfoJson);
            return tmp == null ? null : tmp.ConstructorInfo;
        }
    }
}

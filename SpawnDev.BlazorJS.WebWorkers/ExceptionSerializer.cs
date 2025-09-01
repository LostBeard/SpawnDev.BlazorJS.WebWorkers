using System.Reflection;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// A simple serializer for exceptions that can be used to pass exceptions between window, service worker, dedicated worker, and shared worker contexts.<br/>
    /// </summary>
    public static class ExceptionSerializer
    {
        /// <summary>
        /// Exception type cache
        /// </summary>
        static Dictionary<string, Type?> ExceptionTypes = new Dictionary<string, Type?>();
        /// <summary>
        /// Serializes an exception to a string.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static string? Serialize(Exception? exception)
        {
            if (exception == null) return null;
            // most of the time, Exceptions prefix the Exception type's FullName to the ToString() return value but it is not required
            // if it is not already there, add it
            var exceptionString = exception.ToString();
            var typeNamePart = $"{exception.GetType().FullName}: ";
            return exceptionString.StartsWith(typeNamePart) ? exceptionString : $"{typeNamePart}{exceptionString}";
        }
        /// <summary>
        /// Deserializes an exception from a serialized string.
        /// </summary>
        /// <param name="serializedException"></param>
        /// <returns></returns>
        public static Exception? Deserialize(string? serializedException)
        {
            if (string.IsNullOrEmpty(serializedException)) return null;
            var parts = serializedException.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length < 2) return null;
            var typeName = parts[0];
            var message = parts[1];
            if (!ExceptionTypes.TryGetValue(typeName, out var exTypeCached))
            {
                exTypeCached = Type.GetType(typeName);
                ExceptionTypes[typeName] = exTypeCached;
            }
            if (exTypeCached == null)
            {
                return new Exception(serializedException);
            }
            try
            {
                return (Exception)Activator.CreateInstance(exTypeCached, new object?[] { message })!;
            }
            catch { }
            try
            {
                return (Exception)Activator.CreateInstance(exTypeCached)!;
            }
            catch (Exception ex)
            {
                ExceptionTypes[typeName] = null;
                return new Exception(serializedException);
            }
        }
    }
}

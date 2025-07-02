namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// A simple serializer for exceptions that can be used to pass exceptions between window, service worker, dedicated worker, and shared worker contexts.<br/>
    /// </summary>
    public static class ExceptionSerializer
    {
        /// <summary>
        /// Contains types of exceptions that cannot be created via Activator.CreateInstance.<br/>
        /// Helps to speed up deserialization by avoiding repeated attempts to create these types.
        /// </summary>
        static List<string> UncreatableExceptionTypes = new List<string>();
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
            if (UncreatableExceptionTypes.Contains(typeName))
            {
                return new Exception(serializedException);
            }
            var exType = Type.GetType(typeName);
            if (exType == null)
            {
                UncreatableExceptionTypes.Add(typeName);
                return new Exception(serializedException);
            }
            try
            {
                return (Exception)Activator.CreateInstance(exType, message)!;
            }
            catch
            {
                UncreatableExceptionTypes.Add(typeName);
                return new Exception(serializedException);
            }
        }
    }
}

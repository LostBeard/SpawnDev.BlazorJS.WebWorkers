using System.Linq.Expressions;
using System.Reflection;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// AsyncCallDispatcher converts a call into a MethodInfo and an object[] containing the arguments used for the call and asynchronously returns the value<br />
    /// The call is handled by a virtual Method that must be overridden by the inheriting class to be useful.<br />
    /// Usually this is done to allow serializing a call to be carried out elsewhere with the result being returned.<br />
    /// Task&lt;object?&gt; DispatchCall(MethodInfo methodInfo, object?[]? args = null)<br />
    /// Supported calling conventions:<br />
    /// Expressions - Supports generics, property get and set, asynchronous and synchronous method calls. (Recommended)<br />
    /// - Run, Set<br />
    /// Delegates - Supports generics, asynchronous and synchronous method calls.<br />
    /// - Invoke<br />
    /// Interface proxy - Supports generics, and asynchronous method calls. (uses DispatchProxy)<br />
    /// - GetService<br />
    /// Type and Method name - Supports property get and set (using special names), and asynchronous and synchronous method calls. Overloaded methods can cause errors.<br />
    /// - Call<br />
    /// MethodInfo - All calls funnel to this call. Supports generics, property get and set, asynchronous and synchronous method calls.<br />
    /// - Call<br />
    /// </summary>
    public abstract class AsyncCallDispatcher
    {
        /// <summary>
        /// The binding flags to use when searching for methods
        /// </summary>
        protected BindingFlags MethodBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
        /// <summary>
        /// Must be overridden by inheriting class
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="methodInfo"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract Task<object?> Call(Type serviceType, MethodInfo methodInfo, object?[]? args = null);
        /// <summary>
        /// Must be overridden by inheriting class
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="serviceKey"></param>
        /// <param name="methodInfo"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract Task<object?> CallKeyed(Type serviceType, object serviceKey, MethodInfo methodInfo, object?[]? args = null);
        /// <summary>
        /// Must be implemented by the inheriting class to function
        /// </summary>
        /// <param name="constructorInfo"></param>
        /// <param name="serviceType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract Task CreateService(ConstructorInfo constructorInfo, Type? serviceType, object[]? args);
        /// <summary>
        /// Must be implemented by the inheriting class to function
        /// </summary>
        /// <param name="constructorInfo"></param>
        /// <param name="serviceType"></param>
        /// <param name="serviceKey">If this is null, a non-keyed service will be created</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract Task CreateKeyedService(ConstructorInfo constructorInfo, Type? serviceType, object serviceKey, object[]? args);
        /// <summary>
        /// Remove the specified RuntimeService
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public Task<bool> RemoveService<TService>() => RemoveService(typeof(TService));
        /// <summary>
        /// Remove the specified RuntimeService with the given key
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> RemoveKeyedService<TService>(object key) => RemoveKeyedService(typeof(TService), key);
        /// <summary>
        /// Remove the specified RuntimeService
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public abstract Task<bool> RemoveService(Type serviceType);
        /// <summary>
        /// Remove the specified RuntimeService with the given key
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract Task<bool> RemoveKeyedService(Type serviceType, object key);
        /// <summary>
        /// Returns true if the specified service is found
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public Task<bool> ServiceExists<TService>() => ServiceExists(typeof(TService));
        /// <summary>
        /// Returns true if the specified keyed service is found
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> KeyedServiceExists<TService>(object key) => KeyedServiceExists(typeof(TService), key);
        /// <summary>
        /// Returns true if the specified service is found
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public abstract Task<bool> ServiceExists(Type serviceType);
        /// <summary>
        /// Returns true if the specified keyed service is found
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract Task<bool> KeyedServiceExists(Type serviceType, object key);
        /// <summary>
        /// Remove the specified RuntimeService
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual Task<bool> AddService(Type serviceType)
        {
            return AddService(serviceType, serviceType);
        }
        /// <summary>
        /// Remove the specified RuntimeService with the given key
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Task<bool> AddKeyedService(Type serviceType, object key)
        {
            return AddKeyedService(serviceType, serviceType, key);
        }
        /// <summary>
        /// Add a keyed service at runtime
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract Task<bool> AddKeyedService(Type serviceType, Type implementationType, object key);
        /// <summary>
        /// Add a service at runtime
        /// </summary>
        public abstract Task<bool> AddService(Type serviceType, Type implementationType);
        /// <summary>
        /// Add a service at runtime
        /// </summary>
        public virtual Task<bool> AddService<TService>()
        {
            return AddService(typeof(TService));
        }
        /// <summary>
        /// Add a service at runtime
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        public virtual Task<bool> AddService<TService, TImplementation>() 
        {
            return AddService(typeof(TService), typeof(TImplementation));
        }
        /// <summary>
        /// Add a service at runtime
        /// </summary>
        public virtual Task<bool> AddKeyedService<TService>(object key)
        {
            return AddKeyedService(typeof(TService), key);
        }
        /// <summary>
        /// Add a service at runtime
        /// </summary>
        public virtual Task<bool> AddKeyedService<TService, TImplementation>(object key)
        {
            return AddKeyedService(typeof(TService), typeof(TImplementation), key);
        }

        #region DispatchProxy
        Dictionary<Type, object> ServiceInterfaces = new Dictionary<Type, object>();
        /// <summary>
        /// Returns a service call dispatcher that can call async methods using the returned interface
        /// </summary>
        /// <typeparam name="TServiceInterface"></typeparam>
        /// <returns></returns>
        public TServiceInterface GetService<TServiceInterface>() where TServiceInterface : class
        {
            var typeofT = typeof(TServiceInterface);
            if (ServiceInterfaces.TryGetValue(typeofT, out var serviceWorker)) return (TServiceInterface)serviceWorker;
            var ret = InterfaceCallDispatcher<TServiceInterface>.CreateInterfaceDispatcher(Call);
            ServiceInterfaces[typeofT] = ret;
            return ret;
        }
        public TServiceInterface GetKeyedService<TServiceInterface>(object key) where TServiceInterface : class
        {
            return InterfaceCallDispatcher<TServiceInterface>.CreateInterfaceDispatcher(key, CallKeyed);
        }
        #endregion

        #region Type, Method Name, Argument count
        /// <summary>
        /// Call a service method by name
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task<object?> Call(Type classType, string methodName, object?[]? args = null)
        {
            var parameterCount = args == null ? 0 : args.Length;
            var instanceMethods = classType.FindAllMethods(methodName, parameterCount, MethodBindingFlags, false);
            if (instanceMethods.Count > 1)
            {
                throw new Exception($"More than one method with the same name and compatible parameter count found: {classType.Name} {methodName} {parameterCount}");
            }
            var methodInfo = instanceMethods.FirstOrDefault();
            if (methodInfo == null)
            {
                throw new Exception($"Method not found: {classType.Name} {methodName} {parameterCount}");
            }
            return Call(classType, methodInfo, args);
        }
        /// <summary>
        /// Call a service method using service class name and method name
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task<object?> Call(string className, string methodName, object?[]? args = null)
        {
            var classType = TypeExtensions.GetType(className);
            if (classType == null) throw new Exception($"Class Type not found: {className}");
            return Call(classType, methodName, args);
        }
        /// <summary>
        /// Call the method with the specified name on service type TClass
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task<object?> Call<TClass>(string methodName, object?[]? args = null) => Call(typeof(TClass), methodName, args);
        public async Task<TResult> Call<TClass, TResult>(string methodName, object?[]? args = null) => (TResult)await Call(typeof(TClass), methodName, args);
        #endregion

        #region Expressions
        /// <summary>
        /// Call call a keyed service method usign an expression
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <param name="argsExt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected Task<object?> CallKeyed(Type serviceType, object key, Expression expr, object?[]? argsExt = null)
        {
            if (expr is MethodCallExpression methodCallExpression)
            {
                var methodInfo = methodCallExpression.Method;
                var args = methodCallExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                return CallKeyed(serviceType, key, methodInfo, args);
            }
            else if (expr is MemberExpression memberExpression)
            {
                if (argsExt == null || argsExt.Length == 0)
                {
                    // get call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.GetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property getter does not exist.");
                        }
                        return CallKeyed(serviceType, key, methodInfo);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property getter does not exist.");
                }
                else
                {
                    // set call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.SetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property setter does not exist.");
                        }
                        return CallKeyed(serviceType, key, methodInfo, argsExt);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property setter does not exist.");
                }
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }
        /// <summary>
        /// Converts an Expression into a MethodInfo and a call arguments array<br />
        /// Then calls DispatchCall with them
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="argsExt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected Task<object?> CallStatic(Expression expr, object?[]? argsExt = null)
        {
            if (expr is MethodCallExpression methodCallExpression)
            {
                var methodInfo = methodCallExpression.Method;
                var serviceType = methodInfo.ReflectedType;
                var args = methodCallExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                return Call(serviceType, methodInfo, args);
            }
            else if (expr is MemberExpression memberExpression)
            {
                if (argsExt == null || argsExt.Length == 0)
                {
                    // get call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.GetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property getter does not exist.");
                        }
                        var serviceType = methodInfo.ReflectedType;
                        return Call(serviceType, methodInfo);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property getter does not exist.");
                }
                else
                {
                    // set call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.SetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property setter does not exist.");
                        }
                        var serviceType = methodInfo.ReflectedType;
                        return Call(serviceType, methodInfo, argsExt);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property setter does not exist.");
                }
            }
            else if (expr is NewExpression newExpression)
            {
                throw new Exception("Run does not support constructors. Use New()");
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }
        /// <summary>
        /// Converts an Expression into a MethodInfo and a call arguments array<br />
        /// Then calls DispatchCall with them
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="argsExt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected Task<object?> Call(Type serviceType, Expression expr, object?[]? argsExt = null)
        {
            if (expr is MethodCallExpression methodCallExpression)
            {
                var methodInfo = methodCallExpression.Method;
                var args = methodCallExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                return Call(serviceType, methodInfo, args);
            }
            else if (expr is MemberExpression memberExpression)
            {
                if (argsExt == null || argsExt.Length == 0)
                {
                    // get call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.GetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property getter does not exist.");
                        }
                        return Call(serviceType, methodInfo);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property getter does not exist.");
                }
                else
                {
                    // set call
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        var methodInfo = propertyInfo.SetMethod;
                        if (methodInfo == null)
                        {
                            throw new Exception("Property setter does not exist.");
                        }
                        return Call(serviceType, methodInfo, argsExt);
                    }
                    else if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        throw new Exception("Fields are not supported. Properties are supported.");
                    }
                    throw new Exception("Property setter does not exist.");
                }
            }
            else if (expr is NewExpression newExpression)
            {
                throw new Exception("Run does not support constructors. Use New()");
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }
        protected Task Create(Expression expr, Type? serviceType)
        {
            if (expr is NewExpression newExpression)
            {
                if (newExpression.Constructor != null && newExpression.Constructor?.ReflectedType != null)
                {
                    var constructorInfo = newExpression.Constructor;
                    var implementationType = constructorInfo.ReflectedType;
                    serviceType ??= implementationType;
                    var args = newExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                    return CreateService(constructorInfo, serviceType, args);
                }
                throw new Exception("Constructor does not exist.");
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }
        protected Task CreateKeyed(Expression expr, object serviceKey, Type? serviceType)
        {
            if (expr is NewExpression newExpression)
            {
                if (newExpression.Constructor != null && newExpression.Constructor?.ReflectedType != null)
                {
                    var constructorInfo = newExpression.Constructor;
                    var implementationType = constructorInfo.ReflectedType;
                    serviceType ??= implementationType;
                    var args = newExpression.Arguments.Select(arg => Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)), null).Compile()()).ToArray();
                    return CreateKeyedService(constructorInfo, serviceType, serviceKey, args);
                }
                throw new Exception("Constructor does not exist.");
            }
            else
            {
                throw new Exception($"Unsupported dispatch call: {expr.GetType().Name}");
            }
        }

        // Create instance
        /// <summary>
        /// Create a new runtime keyed service
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public Task New(object serviceKey, Expression<Func<object>> expr) => CreateKeyed(expr.Body, serviceKey, null);
        /// <summary>
        /// Create a new runtime keyed service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="serviceKey"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public Task New<TService>(object serviceKey, Expression<Func<TService>> expr) => CreateKeyed(expr.Body, serviceKey, typeof(TService));
        /// <summary>
        /// Create a new runtime service
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public Task New(Expression<Func<object>> expr) => Create(expr.Body, null);
        /// <summary>
        /// Create a new runtime service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public Task New<TService>(Expression<Func<TService>> expr) => Create(expr.Body, typeof(TService));
        #region Non-Keyed

        // Static
        // Method Calls and Property Getters
        // Action
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run(Expression<Action> expr) => await CallStatic(expr.Body);
        // Func<Task>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run(Expression<Func<Task>> expr) => await CallStatic(expr.Body);
        // Func<ValueTask>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run(Expression<Func<ValueTask>> expr) => await CallStatic(expr.Body);
        // Func<...,TResult>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TResult>(Expression<Func<TResult>> expr) => (TResult)await CallStatic(expr.Body);
        // Func<...,Task<TResult>>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TResult>(Expression<Func<Task<TResult>>> expr) => (TResult)await CallStatic(expr.Body);
        // Func<...,ValueTask<TResult>>
        /// <summary>
        /// Call a method or get the value of a property
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TResult>(Expression<Func<ValueTask<TResult>>> expr) => (TResult)await CallStatic(expr.Body);
        // Property set
        /// <summary>
        /// Set a property value
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="expr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task Set<TProperty>(Expression<Func<TProperty>> expr, TProperty value) => await CallStatic(expr.Body, new object[] { value });

        // Instance
        // Method Calls and Property Getters
        // Action
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(Expression<Action<TInstance>> expr) => await Call(typeof(TInstance), expr.Body);
        // Func<Task>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(Expression<Func<TInstance, Task>> expr) => await Call(typeof(TInstance), expr.Body);
        // Func<ValueTask>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(Expression<Func<TInstance, ValueTask>> expr) => await Call(typeof(TInstance), expr.Body);
        // Func<...,TResult>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(Expression<Func<TInstance, TResult>> expr) => (TResult)await Call(typeof(TInstance), expr.Body);
        // Func<...,Task<TResult>>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(Expression<Func<TInstance, Task<TResult>>> expr) => (TResult)await Call(typeof(TInstance), expr.Body);
        // Func<...,ValueTask<TResult>>
        /// <summary>
        /// Call a service method or get the value of a service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(Expression<Func<TInstance, ValueTask<TResult>>> expr) => (TResult)await Call(typeof(TInstance), expr.Body);
        // Property set
        /// <summary>
        /// Set a service property value
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="expr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task Set<TInstance, TProperty>(Expression<Func<TInstance, TProperty>> expr, TProperty value) => await Call(typeof(TInstance), expr.Body, new object[] { value });
        #endregion
        #region Keyed

        // Instance
        // Method Calls and Property Getters
        // Action
        /// <summary>
        /// Call a keyed service method or get the value of a keyed service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(object key, Expression<Action<TInstance>> expr) => await CallKeyed(typeof(TInstance), key, expr.Body);
        // Func<Task>
        /// <summary>
        /// Call a keyed service method or get the value of a keyed service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(object key, Expression<Func<TInstance, Task>> expr) => await CallKeyed(typeof(TInstance), key, expr.Body);
        // Func<ValueTask>
        /// <summary>
        /// Call a keyed service method or get the value of a keyed service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task Run<TInstance>(object key, Expression<Func<TInstance, ValueTask>> expr) => await CallKeyed(typeof(TInstance), key, expr.Body);
        // Func<...,TResult>
        /// <summary>
        /// Call a keyed service method or get the value of a keyed service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(object key, Expression<Func<TInstance, TResult>> expr) => (TResult)await CallKeyed(typeof(TInstance), key, expr.Body);
        // Func<...,Task<TResult>>
        /// <summary>
        /// Call a keyed service method or get the value of a keyed service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(object key, Expression<Func<TInstance, Task<TResult>>> expr) => (TResult)await CallKeyed(typeof(TInstance), key, expr.Body);
        // Func<...,ValueTask<TResult>>
        /// <summary>
        /// Call a keyed service method or get the value of a keyed service property
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public async Task<TResult> Run<TInstance, TResult>(object key, Expression<Func<TInstance, ValueTask<TResult>>> expr) => (TResult)await CallKeyed(typeof(TInstance), key, expr.Body);
        // Property set
        /// <summary>
        /// Set a keyed service property value
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="key"></param>
        /// <param name="expr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task Set<TInstance, TProperty>(object key, Expression<Func<TInstance, TProperty>> expr, TProperty value) => await CallKeyed(typeof(TInstance), key, expr.Body, new object[] { value });
        #endregion
        #endregion

        #region Lock
        public event Action<AsyncCallDispatcher> OnUnlocked;
        public event Action<AsyncCallDispatcher> OnLocked;
        public bool IsLocked { get; private set; }
        public bool AcquireLock()
        {
            if (IsLocked) return false;
            IsLocked = true;
            //Console.WriteLine("............ AcquireLock");
            OnLocked?.Invoke(this);
            return true;
        }
        public bool ReleaseLock()
        {
            if (!IsLocked) return false;
            IsLocked = false;
            //Console.WriteLine("............ ReleaseLock");
            OnUnlocked?.Invoke(this);
            return true;
        }
        #endregion

        #region Delegates
        protected virtual Task<object?> DispatchCall(Delegate methodDelegate, object?[]? args = null)
        {
            var methodInfo = methodDelegate.Method;
            var serviceType = methodInfo.ReflectedType;
            return Call(serviceType, methodInfo, args);
        }

        // Action
        public async Task Invoke(Action methodDelegate)
            => await DispatchCall(methodDelegate, null);
        public Task Invoke<T0>(Action<T0> methodDelegate, T0 arg0)
            => DispatchCall(methodDelegate, new object[] { arg0 });
        public Task Invoke<T0, T1>(Action<T0, T1> methodDelegate, T0 arg0, T1 arg1)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1 });
        public Task Invoke<T0, T1, T2>(Action<T0, T1, T2> methodDelegate, T0 arg0, T1 arg1, T2 arg2)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2 });
        public Task Invoke<T0, T1, T2, T3>(Action<T0, T1, T2, T3> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3 });
        public Task Invoke<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4 });
        public Task Invoke<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        public Task Invoke<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 });
        public Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        public Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        public Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        public Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            => DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        // <TResult>
        public async Task<TResult> Invoke<TResult>(Func<TResult> methodDelegate)
            => (TResult)await DispatchCall(methodDelegate, null);
        public async Task<TResult> Invoke<T0, TResult>(Func<T0, TResult> methodDelegate, T0 arg0)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0 });
        public async Task<TResult> Invoke<T0, T1, TResult>(Func<T0, T1, TResult> methodDelegate, T0 arg0, T1 arg1)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1 });
        public async Task<TResult> Invoke<T0, T1, T2, TResult>(Func<T0, T1, T2, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, TResult>(Func<T0, T1, T2, T3, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, TResult>(Func<T0, T1, T2, T3, T4, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, TResult>(Func<T0, T1, T2, T3, T4, T5, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        // Task<TResult>
        public async Task<TResult> Invoke<TResult>(Func<Task<TResult>> methodDelegate)
            => (TResult)await DispatchCall(methodDelegate, null);
        public async Task<TResult> Invoke<T0, TResult>(Func<T0, Task<TResult>> methodDelegate, T0 arg0)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0 });
        public async Task<TResult> Invoke<T0, T1, TResult>(Func<T0, T1, Task<TResult>> methodDelegate, T0 arg0, T1 arg1)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1 });
        public async Task<TResult> Invoke<T0, T1, T2, TResult>(Func<T0, T1, T2, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, TResult>(Func<T0, T1, T2, T3, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, TResult>(Func<T0, T1, T2, T3, T4, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, TResult>(Func<T0, T1, T2, T3, T4, T5, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        public async Task<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Task<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        // ValueTask<TResult>
        public async ValueTask<TResult> Invoke<TResult>(Func<ValueTask<TResult>> methodDelegate)
            => (TResult)await DispatchCall(methodDelegate, null);
        public async ValueTask<TResult> Invoke<T0, TResult>(Func<T0, ValueTask<TResult>> methodDelegate, T0 arg0)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0 });
        public async ValueTask<TResult> Invoke<T0, T1, TResult>(Func<T0, T1, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, TResult>(Func<T0, T1, T2, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, TResult>(Func<T0, T1, T2, T3, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, TResult>(Func<T0, T1, T2, T3, T4, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, T5, TResult>(Func<T0, T1, T2, T3, T4, T5, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        public async ValueTask<TResult> Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, ValueTask<TResult>> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            => (TResult)await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        // Task
        public async Task Invoke(Func<Task> methodDelegate)
            => await DispatchCall(methodDelegate, null);
        public async Task Invoke<T0, TResult>(Func<T0, Task> methodDelegate, T0 arg0)
            => await DispatchCall(methodDelegate, new object[] { arg0 });
        public async Task Invoke<T0, T1, TResult>(Func<T0, T1, Task> methodDelegate, T0 arg0, T1 arg1)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1 });
        public async Task Invoke<T0, T1, T2, TResult>(Func<T0, T1, T2, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2 });
        public async Task Invoke<T0, T1, T2, T3, TResult>(Func<T0, T1, T2, T3, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3 });
        public async Task Invoke<T0, T1, T2, T3, T4, TResult>(Func<T0, T1, T2, T3, T4, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4 });
        public async Task Invoke<T0, T1, T2, T3, T4, T5, TResult>(Func<T0, T1, T2, T3, T4, T5, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        public async Task Invoke<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 });
        public async Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        public async Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        public async Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        public async Task Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Task> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        // ValueTask
        public async ValueTask Invoke(Func<ValueTask> methodDelegate)
            => await DispatchCall(methodDelegate, null);
        public async ValueTask Invoke<T0>(Func<T0, ValueTask> methodDelegate, T0 arg0)
            => await DispatchCall(methodDelegate, new object[] { arg0 });
        public async ValueTask Invoke<T0, T1>(Func<T0, T1, ValueTask> methodDelegate, T0 arg0, T1 arg1)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1 });
        public async ValueTask Invoke<T0, T1, T2>(Func<T0, T1, T2, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2 });
        public async ValueTask Invoke<T0, T1, T2, T3>(Func<T0, T1, T2, T3, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4>(Func<T0, T1, T2, T3, T4, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4, T5>(Func<T0, T1, T2, T3, T4, T5, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4, T5, T6>(Func<T0, T1, T2, T3, T4, T5, T6, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4, T5, T6, T7>(Func<T0, T1, T2, T3, T4, T5, T6, T7, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        public async ValueTask Invoke<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, ValueTask> methodDelegate, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            => await DispatchCall(methodDelegate, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });
        #endregion
    }
}

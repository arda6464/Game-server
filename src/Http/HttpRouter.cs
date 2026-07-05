using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

public static class HttpRouter
{
    private class RouteEntry
    {
        public object Controller;
        public MethodInfo Method;
        public string HttpMethod;
        public bool RequiresAuth;
    }

    private static readonly ConcurrentDictionary<string, RouteEntry> _routes = new();

    public static void RegisterControllers()
    {
        _routes.Clear();

        var controllerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<HttpControllerAttribute>() != null);

        foreach (var type in controllerTypes)
        {
            var controllerInstance = Activator.CreateInstance(type);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var routeAttr = method.GetCustomAttribute<HttpRouteAttribute>();
                if (routeAttr == null) continue;

                string fullPath = NormalizePath(routeAttr.Path);
                string key = $"{routeAttr.Method.ToUpper()}|{fullPath}";

                if (_routes.ContainsKey(key))
                {
                    Logger.errorslog($"[HttpRouter] Duplicate route: {key}");
                    continue;
                }

                _routes[key] = new RouteEntry
                {
                    Controller = controllerInstance!,
                    Method = method,
                    HttpMethod = routeAttr.Method.ToUpper(),
                    RequiresAuth = routeAttr.RequiresAuth
                };

                Logger.genellog($"[HttpRouter] Registered: {routeAttr.Method.ToUpper()} {fullPath}");
            }
        }

        Logger.successlog($"[HttpRouter] {_routes.Count} endpoint registered.");
    }

    public static bool Handle(SimpleHttpContext context)
    {
        string path = NormalizePath(context.Request.Path);
        string method = context.Request.Method.ToUpper();

        // OPTIONS preflight
        if (method == "OPTIONS")
        {
            context.Response.StatusCode = 200;
            return true;
        }

        string key = $"{method}|{path}";

        if (!_routes.TryGetValue(key, out var route))
        {
            SendError(context.Response, 404, "Endpoint not found");
            return true;
        }

        if (route.RequiresAuth && !AdminAuth.IsAuthorized(context))
        {
            SendError(context.Response, 401, "Unauthorized");
            return true;
        }

        try
        {
            object? result = InvokeRoute(route, context);
            WriteJson(context.Response, result);
            return true;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[HttpRouter] Route error ({path}): {ex.Message}");
            SendError(context.Response, 500, ex.Message);
            return true;
        }
    }

    private static object? InvokeRoute(RouteEntry route, SimpleHttpContext context)
    {
        if (route.Controller is BaseController baseController)
        {
            baseController.Context = context;
        }

        var parameters = route.Method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.ParameterType == typeof(SimpleHttpContext))
            {
                args[i] = context;
            }
            else if (param.GetCustomAttribute<FromBodyAttribute>() != null)
            {
                args[i] = ParseBody(context, param.ParameterType);
            }
            else if (param.GetCustomAttribute<FromQueryAttribute>() != null)
            {
                args[i] = ParseQuery(context, param.Name!, param.ParameterType);
            }
            else
            {
                args[i] = Type.Missing;
            }
        }

        return route.Method.Invoke(route.Controller, args);
    }

    private static object? ParseBody(SimpleHttpContext context, Type targetType)
    {
        if (context.Request.Body.Length == 0)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        string body = Encoding.UTF8.GetString(context.Request.Body);

        if (targetType == typeof(Dictionary<string, string>))
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            return dict ?? new Dictionary<string, string>();
        }

        if (targetType == typeof(Dictionary<string, object>))
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            return dict ?? new Dictionary<string, object>();
        }

        return JsonConvert.DeserializeObject(body, targetType);
    }

    private static object? ParseQuery(SimpleHttpContext context, string name, Type targetType)
    {
        if (!context.Request.QueryString.TryGetValue(name, out string? value))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (targetType == typeof(int) || targetType == typeof(int?))
            return int.TryParse(value, out int i) ? i : (int?)null;
        if (targetType == typeof(bool) || targetType == typeof(bool?))
            return bool.TryParse(value, out bool b) ? b : (bool?)null;
        return value;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        path = path.ToLowerInvariant();
        if (!path.StartsWith("/")) path = "/" + path;
        if (path.EndsWith("/") && path.Length > 1) path = path.TrimEnd('/');
        return path;
    }

    private static void WriteJson(SimpleHttpResponse response, object? data)
    {
        string json = JsonConvert.SerializeObject(data ?? new { success = true });
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private static void SendError(SimpleHttpResponse response, int code, string message)
    {
        response.StatusCode = code;
        byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { error = message }));
        response.ContentType = "application/json";
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }
}

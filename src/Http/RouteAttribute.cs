using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class HttpControllerAttribute : Attribute
{
    public string Prefix { get; }
    public HttpControllerAttribute(string prefix = "") { Prefix = prefix ?? ""; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HttpRouteAttribute : Attribute
{
    public string Method { get; }
    public string Path { get; }
    public bool RequiresAuth { get; set; } = true;

    public HttpRouteAttribute(string method, string path = "")
    {
        Method = method ?? "GET";
        Path = path ?? "";
    }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class FromBodyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class FromQueryAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AllowAnonymousAttribute : Attribute { }

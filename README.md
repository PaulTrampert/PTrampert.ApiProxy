# PTrampert.ApiProxy
Provides an api proxy route to an ASP.NET Core app.

## Basic Usage
In `Startup.ConfigureServices`, use `AddApiProxy`. You can either pass in an `IConfiguration` that maps to `ApiProxyConfig`, or use configuration action.
Then, in `Startup.Configure`, use `app.UseApiProxy` to register the proxy route. If you want to proxy api's that use web sockets, make sure to call `app.UseWebSockets()`
before `app.UseApiProxy()`.

#### `IConfiguration` Example
`appConfig.json`
```json
{
  "ApiProxyConfig": {
    "simple": {
      "BaseUrl": "https://example1.com"
    },
    "simple-with-websockets": {
      "BaseUrl": "https://example1.com",
      "WsBaseUrl": "wss://example1.com"
    },
    "basic-auth": {
      "BaseUrl": "https://example2.com/protected-resources",
      "AuthType": "PTrampert.ApiProxy.Authentication.BasicAuthentication",
      "AuthProps": {
          "Id": "myId",
          "Secret": "super-secret-api-key"
      }
    },
    "user-bearer": {
      "BaseUrl": "https://example3.com/protected-resources",
      "AuthType": "PTrampert.ApiProxy.Authentication.UserBearerAuthentication",
      "AuthProps": {
          "Mode": "AuthProps",
          "TokenKey": "access_token"
      }
    },
    "proxy-headers": {
      "BaseUrl": "https://example4.com/",
      "RequestHeaders": [ "X-Some-Header", "User-Agent" ],
      "ResponseHeaders": [ "Links", "X-Some-Header" ]
    }
  }
}
```
`Program.cs`
```csharp
services.AddApiProxy(config.GetSection("ApiProxyConfig"));
```

#### Configuration Action Example
```csharp
services.AddApiProxy(cfg =>
{
    cfg.Add("simple", new ApiConfig{BaseUrl = "https://example1.com"});
    cfg.Add("basic-auth", new ApiConfig
    {
        BaseUrl = "https://example2.com/protected-resources",
        AuthType = typeof(BasicAuthentication).FullName,
        AuthProps = new Dictionary<string, string>
        {
            { "Id", "myId" },
            { "Secret", "super-secret-api-key" }
        }
    });
    cfg.Add("user-bearer", new ApiConfig
    {
        BaseUrl = "https://example3.com/protected-resources",
        AuthType = typeof(UserBearerAuthentication).FullName,
        AuthProps = new Dictionary<string, string>
        {
            { "Mode", "AuthProps" },
            { "TokenKey", "access_token" }
        }
    });
    cfg.Add("proxy-headers", new ApiConfig
    {
        BaseUrl = "https://example4.com/",
        RequestHeaders = new [] { "X-Some-Header", "User-Agent" },
        ResponseHeaders = new [] { "Links", "X-Some-Header" }
    });
});
```

#### `Startup.Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)`
```csharp
  // If proxying WebSockets, call this before UseApiProxy().
  app.UseWebSockets();
  // Pipeline steps before the proxy
  app.UseApiProxy("apiproxy");
  // Pipeline steps after the proxy
```

The above examples configure an api proxy that proxies requests for 4 different apis. If the app root exists at `https://myapp.com/root`,
then a client can call `https://example1.com/some/route` by calling `https://myapp.com/root/apiproxy/simple/some/route`.

## Reserved Headers

`RequestHeaders` and `ResponseHeaders` name the headers the proxy passes through. A few headers cannot
be listed there. Configuring one of them fails validation when the app starts, with a message naming the
api and the header, instead of failing later on a request.

| Header | Why it is reserved |
| --- | --- |
| `Allow`, `Content-Disposition`, `Content-Encoding`, `Content-Language`, `Content-Length`, `Content-Location`, `Content-MD5`, `Content-Range`, `Content-Type`, `Expires`, `Last-Modified` | These are *content headers*: `System.Net.Http` keeps them on a message's content rather than on the message, and this list is exactly the set `HttpContentHeaders` exposes. The proxy cannot carry one in either direction — adding one to the upstream request is a "misused header name" that fails every request to the api, and on an upstream response they arrive on the response's content, which the proxy does not read, so listing one forwards nothing. Reserved in both `RequestHeaders` and `ResponseHeaders`. |
| `Authorization` | Set from the api's configured `AuthType`, which would discard a forwarded value. Reserved in `RequestHeaders`, but only for an api that configures an `AuthType`. An api without one sets no `Authorization` of its own, so it may list the header to pass the client's through. |

Reserved does not mean preserved: apart from `Content-Type`, which the proxy passes on with the response
body, the content headers are dropped rather than forwarded. Forwarding them from the content they arrive
on is tracked in [#240](https://github.com/PaulTrampert/PTrampert.ApiProxy/issues/240).

#### Running the Sample App
A small sample app is included in this project. To run it, simply run `docker compose up`.
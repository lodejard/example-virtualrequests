

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace example_virtualrequests
{
    public class FakeRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PathString _path = new PathString("/probe");

        public FakeRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // requests arriving at "/probe" 
            // will get a response of {"/weatherforecast":200,"/bad-page":404}
            // and a status code of 503

            if (context.Request.Path == _path)
            {
                var paths = new[]
                {
                    "/weatherforecast",
                    "/bad-page",
                };
                
                var results = new Dictionary<string,int>();

                foreach (var path in paths)
                {
                    var fakeContext = new DefaultHttpContext
                    {
                        Request =
                        {
                            Method = "GET",
                            PathBase = context.Request.PathBase,
                            Path = path,
                        },
                        RequestServices = context.RequestServices,
                    };

                    await _next(fakeContext);

                    var statusCode = fakeContext.Response.StatusCode;

                    results[path] = statusCode;
                    if (statusCode != 200)
                    {
                        context.Response.StatusCode = 503;
                    }
                }

                await context.Response.WriteAsync(JsonSerializer.Serialize(results));
            }
            else
            {
                await _next(context);
            }
        }
    }
}

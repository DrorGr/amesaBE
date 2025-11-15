using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace AmesaBackend.Shared.Tracing
{
    public static class XRayExtensions
    {
        public static IApplicationBuilder UseAmesaXRay(this IApplicationBuilder app, string serviceName)
        {
            try
            {
                AWSXRayRecorder.InitializeInstance();
                AWSXRayRecorder.Instance.AddSegmentMetadata("service", serviceName);
                
                return app.UseXRay(serviceName);
            }
            catch
            {
                // If X-Ray initialization fails, continue without tracing
                return app;
            }
        }
    }
}


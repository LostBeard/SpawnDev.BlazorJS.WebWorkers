using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers.Demo.Services
{
    public class AppServiceWorker : ServiceWorkerEventHandler
    {
        public AppServiceWorker(BlazorJSRuntime js) : base(js)
        {

        }
        // called before any ServiceWorker events are handled
        protected override async Task OnInitializedAsync()
        {
            // This service may start in any scope. This will be called before the app runs.
            // If JS.IsWindow == true be careful not stall here.
            // you can do initialization based on the scope that is running
            Log("GlobalThisTypeName", JS.GlobalThisTypeName);
        }

        protected override async Task ServiceWorker_OnInstallAsync(ExtendableEvent e)
        {
            Log($"ServiceWorker_OnInstallAsync", JS.GlobalThisTypeName);
            _ = ServiceWorkerThis!.SkipWaiting();   // returned task can be ignored
        }

        protected override async Task ServiceWorker_OnActivateAsync(ExtendableEvent e)
        {
            Log($"ServiceWorker_OnActivateAsync");
            await ServiceWorkerThis!.Clients.Claim();
        }

        protected override async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
        {
            Log($"ServiceWorker_OnFetchAsync", e.Request.Method, e.Request.Url);
            Response ret;
            try
            {
                ret = await JS.Fetch(e.Request);
            }
            catch (Exception ex)
            {
                ret = new Response(ex.Message, new ResponseOptions { Status = 500, StatusText = ex.Message, Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } } });
                Log($"ServiceWorker_OnFetchAsync failed: {ex.Message}");
            }
            return ret;
        }

        protected override async Task ServiceWorker_OnMessageAsync(ExtendableMessageEvent e)
        {
            Log($"ServiceWorker_OnMessageAsync");
        }

        protected override async Task ServiceWorker_OnPushAsync(PushEvent e)
        {
            Log($"ServiceWorker_OnPushAsync");
        }

        protected override async Task ServiceWorker_OnPushSubscriptionChangeAsync(PushSubscriptionChangeEvent e)
        {
            Log($"ServiceWorker_OnPushSubscriptionChangeAsync");
        }

        protected override async Task ServiceWorker_OnSyncAsync(SyncEvent e)
        {
            Log($"ServiceWorker_OnSyncAsync");
        }

        protected override async Task ServiceWorker_OnNotificationCloseAsync(NotificationEvent e)
        {
            Log($"ServiceWorker_OnNotificationCloseAsync");
        }

        protected override async Task ServiceWorker_OnNotificationClickAsync(NotificationEvent e)
        {
            Log($"ServiceWorker_OnNotificationClickAsync");
        }
        void Log(params object[] msg) => JS.Log(msg);
    }
}

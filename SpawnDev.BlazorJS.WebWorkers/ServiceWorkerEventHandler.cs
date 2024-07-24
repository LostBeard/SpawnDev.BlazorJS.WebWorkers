using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// ServiceWorkerEventHandler base class<br/>
    /// Inherit this class and override the virtual event handler methods as needed
    /// </summary>
    public class ServiceWorkerEventHandler : IAsyncBackgroundService, IDisposable
    {
        /// <summary>
        /// Returns when the service is Ready
        /// </summary>
        public Task Ready => _Ready ??= InitAsync();
        private Task? _Ready = null;
        /// <summary>
        /// BlazorJSRuntime
        /// </summary>
        protected BlazorJSRuntime JS;
        /// <summary>
        /// If running in a ServiceWorkerGlobalScope this will be globalThis
        /// </summary>
        protected ServiceWorkerGlobalScope? ServiceWorkerThis = null;
        /// <summary>
        /// ServiceWorkerEventHandler default constructor
        /// </summary>
        public ServiceWorkerEventHandler(BlazorJSRuntime js)
        {
            JS = js;
            ServiceWorkerThis = JS.ServiceWorkerThis;
        }
        async Task InitAsync()
        {
            await OnInitializedAsync();
            if (ServiceWorkerThis != null)
            {
                // the service is running a ServiceWorker
                ServiceWorkerThis.OnFetch += ServiceWorker_OnFetch;
                ServiceWorkerThis.OnInstall += ServiceWorker_OnInstall;
                ServiceWorkerThis.OnActivate += ServiceWorker_OnActivate;
                ServiceWorkerThis.OnMessage += ServiceWorker_OnMessage;
                ServiceWorkerThis.OnPush += ServiceWorker_OnPush;
                ServiceWorkerThis.OnPushSubscriptionChange += ServiceWorker_OnPushSubscriptionChange;
                ServiceWorkerThis.OnSync += ServiceWorker_OnSync;
                ServiceWorkerThis.OnNotificationClose += ServiceWorker_OnNotificationClose;
                ServiceWorkerThis.OnNotificationClick += ServiceWorker_OnNotificationClick;
                GetMissedServiceWorkerEvents();
            }
        }
        /// <summary>
        /// This method retrieves events that were fired while Blazor was still loading.<br/>
        /// The events have been put on hold by calling their respondWith or respondWith methods
        /// </summary>
        void GetMissedServiceWorkerEvents()
        {
            var missedEvents = JS.Call<Event[]>("GetMissedServiceWorkerEvents");
            foreach (var e in missedEvents)
            {
                var type = e.Type;
                switch (type)
                {
                    case "activate":
                        ServiceWorker_OnActivate(e.JSRefMove<MissedExtendableEvent>());
                        break;
                    case "fetch":
                        ServiceWorker_OnFetch(e.JSRefMove<MissedFetchEvent>());
                        break;
                    case "install":
                        ServiceWorker_OnInstall(e.JSRefMove<MissedExtendableEvent>());
                        break;
                    case "message":
                        ServiceWorker_OnMessage(e.JSRefMove<MissedExtendableMessageEvent>());
                        break;
                    case "notificationclick":
                        ServiceWorker_OnNotificationClose(e.JSRefMove<MissedNotificationEvent>());
                        break;
                    case "notificationclose":
                        ServiceWorker_OnNotificationClose(e.JSRefMove<MissedNotificationEvent>());
                        break;
                    case "push":
                        ServiceWorker_OnPush(e.JSRefMove<MissedPushEvent>());
                        break;
                    case "pushsubscriptionchange":
                        ServiceWorker_OnPushSubscriptionChange(e.JSRefMove<Event>());
                        break;
                    case "sync":
                        ServiceWorker_OnSync(e.JSRefMove<MissedSyncEvent>());
                        break;
                }
            }
        }
        void ServiceWorker_OnNotificationClose(NotificationEvent e)
        {
            if (e is MissedNotificationEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnNotificationCloseAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnNotificationCloseAsync(e));
            }
        }
        void ServiceWorker_OnNotificationClick(NotificationEvent e)
        {
            if (e is MissedNotificationEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnNotificationClickAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnNotificationClickAsync(e));
            }
        }
        /// <summary>
        /// Occurs on app startup
        /// </summary>
        /// <returns></returns>
        protected virtual Task OnInitializedAsync() => Task.CompletedTask;
        /// <summary>
        /// Occurs when a ServiceWorkerRegistration acquires a new ServiceWorkerRegistration.installing worker.
        /// </summary>
        protected virtual Task ServiceWorker_OnInstallAsync(ExtendableEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a ServiceWorkerRegistration acquires a new ServiceWorkerRegistration.active worker.
        /// </summary>
        protected virtual Task ServiceWorker_OnActivateAsync(ExtendableEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when incoming messages are received. Controlled pages can use the MessagePort.postMessage() method to send messages to service workers.
        /// </summary>
        protected virtual Task ServiceWorker_OnMessageAsync(ExtendableMessageEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a server push notification is received.
        /// </summary>
        protected virtual Task ServiceWorker_OnPushAsync(PushEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a push subscription has been invalidated, or is about to be invalidated (e.g. when a push service sets an expiration time).
        /// </summary>
        protected virtual void ServiceWorker_OnPushSubscriptionChange(Event e) { }
        /// <summary>
        /// Triggered when a call to SyncManager.register is made from a service worker client page. The attempt to sync is made either immediately if the network is available or as soon as the network becomes available.
        /// </summary>
        protected virtual Task ServiceWorker_OnSyncAsync(SyncEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a user closes a displayed notification.
        /// </summary>
        protected virtual Task ServiceWorker_OnNotificationCloseAsync(NotificationEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a user clicks on a displayed notification.
        /// </summary>
        protected virtual Task ServiceWorker_OnNotificationClickAsync(NotificationEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a fetch() is called.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
        {
            Response ret;
            try
            {
                ret = await JS.Fetch(e.Request);
            }
            catch (Exception ex)
            {
                ret = new Response(ex.Message, new ResponseOptions { Status = 500, StatusText = ex.Message, Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } } });
            }
            return ret;
        }
        void ServiceWorker_OnInstall(ExtendableEvent e)
        {
            if (e is MissedExtendableEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnInstallAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnInstallAsync(e));
            }
        }
        void ServiceWorker_OnActivate(ExtendableEvent e)
        {
            if (e is MissedExtendableEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnActivateAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnActivateAsync(e));
            }
        }
        void ServiceWorker_OnFetch(FetchEvent e)
        {
            if (e is MissedFetchEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        var response = await ServiceWorker_OnFetchAsync(e);
                        missedEvent.ResponseResolve(response);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.ResponseReject();
                    }
                });
            }
            else
            {
                e.RespondWith(ServiceWorker_OnFetchAsync(e));
            }
        }
        void ServiceWorker_OnMessage(ExtendableMessageEvent e)
        {
            if (e is MissedExtendableMessageEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnMessageAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnMessageAsync(e));
            }
        }
        void ServiceWorker_OnPush(PushEvent e)
        {
            if (e is MissedPushEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnPushAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnPushAsync(e));
            }
        }
        void ServiceWorker_OnSync(SyncEvent e)
        {
            if (e is MissedSyncEvent missedEvent)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnSyncAsync(e);
                        missedEvent.WaitResolve();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                        missedEvent.WaitReject();
                    }
                });
            }
            else
            {
                e.WaitUntil(ServiceWorker_OnSyncAsync(e));
            }
        }
        /// <summary>
        /// Release resources
        /// </summary>
        public virtual void Dispose()
        {
            if (ServiceWorkerThis != null)
            {

            }
        }
    }
}


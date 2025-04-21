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
                ServiceWorkerThis.OnActivate += ServiceWorker_OnActivate;
                ServiceWorkerThis.OnBackgroundFetchAbort += ServiceWorker_OnBackgroundFetchAbort;
                ServiceWorkerThis.OnBackgroundFetchClick += ServiceWorker_OnBackgroundFetchClick;
                ServiceWorkerThis.OnBackgroundFetchFail += ServiceWorker_OnBackgroundFetchFail;
                ServiceWorkerThis.OnBackgroundFetchSuccess += ServiceWorker_OnBackgroundFetchSuccess;
                ServiceWorkerThis.OnCanMakePayment += ServiceWorker_OnCanMakePayment;
                ServiceWorkerThis.OnContentDelete += ServiceWorker_OnContentDelete;
                ServiceWorkerThis.OnCookieChange += ServiceWorker_OnCookieChange;
                ServiceWorkerThis.OnFetch += ServiceWorker_OnFetch;
                ServiceWorkerThis.OnInstall += ServiceWorker_OnInstall;
                ServiceWorkerThis.OnMessage += ServiceWorker_OnMessage;
                ServiceWorkerThis.OnMessageError += ServiceWorker_OnMessageError;
                ServiceWorkerThis.OnNotificationClick += ServiceWorker_OnNotificationClick;
                ServiceWorkerThis.OnNotificationClose += ServiceWorker_OnNotificationClose;
                ServiceWorkerThis.OnPaymentRequest += ServiceWorker_OnPaymentRequest;
                ServiceWorkerThis.OnPeriodicSync += ServiceWorker_OnPeriodicSync;
                ServiceWorkerThis.OnPush += ServiceWorker_OnPush;
                ServiceWorkerThis.OnPushSubscriptionChange += ServiceWorker_OnPushSubscriptionChange;
                ServiceWorkerThis.OnSync += ServiceWorker_OnSync;
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
                    case "backgroundfetchabort":
                        ServiceWorker_OnBackgroundFetchAbort(e.JSRefMove<MissedBackgroundFetchEvent>());
                        break;
                    case "backgroundfetchclick":
                        ServiceWorker_OnBackgroundFetchClick(e.JSRefMove<MissedBackgroundFetchEvent>());
                        break;
                    case "backgroundfetchfail":
                        ServiceWorker_OnBackgroundFetchFail(e.JSRefMove<MissedBackgroundFetchUpdateUIEvent>());
                        break;
                    case "backgroundfetchsuccess":
                        ServiceWorker_OnBackgroundFetchSuccess(e.JSRefMove<MissedBackgroundFetchUpdateUIEvent>());
                        break;
                    case "canmakepayment":
                        ServiceWorker_OnCanMakePayment(e.JSRefMove<MissedCanMakePaymentEvent>());
                        break;
                    case "contentdelete":
                        ServiceWorker_OnContentDelete(e.JSRefMove<MissedContentIndexEvent>());
                        break;
                    case "cookiechange":
                        ServiceWorker_OnCookieChange(e.JSRefMove<MissedExtendableCookieChangeEvent>());
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
                    case "messageerror":
                        ServiceWorker_OnMessageError(e.JSRefMove<MissedExtendableMessageEvent>());
                        break;
                    case "notificationclick":
                        ServiceWorker_OnNotificationClose(e.JSRefMove<MissedNotificationEvent>());
                        break;
                    case "notificationclose":
                        ServiceWorker_OnNotificationClose(e.JSRefMove<MissedNotificationEvent>());
                        break;
                    case "paymentrequest":
                        ServiceWorker_OnPaymentRequest(e.JSRefMove<MissedPaymentRequestEvent>());
                        break;
                    case "periodicsync":
                        ServiceWorker_OnPeriodicSync(e.JSRefMove<MissedPeriodicSyncEvent>());
                        break;
                    case "push":
                        ServiceWorker_OnPush(e.JSRefMove<MissedPushEvent>());
                        break;
                    case "pushsubscriptionchange":
                        ServiceWorker_OnPushSubscriptionChange(e.JSRefMove<MissedPushSubscriptionChangeEvent>());
                        break;
                    case "sync":
                        ServiceWorker_OnSync(e.JSRefMove<MissedSyncEvent>());
                        break;
                }
            }
        }
        #region Service Worker overridable events
        /// <summary>
        /// Occurs on app startup
        /// </summary>
        /// <returns></returns>
        protected virtual Task OnInitializedAsync() => Task.CompletedTask;
        /// <summary>
        /// Occurs when a ServiceWorkerRegistration acquires a new ServiceWorkerRegistration.active worker.
        /// </summary>
        protected virtual Task ServiceWorker_OnActivateAsync(ExtendableEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired when a background fetch operation has been canceled by the user or the app.
        /// </summary>
        protected virtual Task ServiceWorker_OnBackgroundFetchAbortAsync(BackgroundFetchEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired when the user has clicked on the UI for a background fetch operation.
        /// </summary>
        protected virtual Task ServiceWorker_OnBackgroundFetchClickAsync(BackgroundFetchEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired when at least one of the requests in a background fetch operation has failed.
        /// </summary>
        protected virtual Task ServiceWorker_OnBackgroundFetchFailAsync(BackgroundFetchUpdateUIEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired when all of the requests in a background fetch operation have succeeded.
        /// </summary>
        protected virtual Task ServiceWorker_OnBackgroundFetchSuccessAsync(BackgroundFetchUpdateUIEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired on a payment app's service worker to check whether it is ready to handle a payment. Specifically, it is fired when the merchant website calls the PaymentRequest() constructor.
        /// </summary>
        protected virtual Task<bool> ServiceWorker_OnCanMakePaymentAsync(CanMakePaymentEvent e) => Task.FromResult<bool>(false);
        /// <summary>
        /// Occurs when an item is removed from the ContentIndex.
        /// </summary>
        protected virtual Task ServiceWorker_OnContentDeleteAsync(ContentIndexEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired when a cookie change has occurred that matches the service worker's cookie change subscription list.
        /// </summary>
        protected virtual Task ServiceWorker_OnCookieChangeAsync(ExtendableCookieChangeEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a fetch() is called.
        /// </summary>
        protected virtual async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
        {
            Response ret;
            try
            {
                ret = await JS.Fetch(e.Request);
            }
            catch
            {
                // Exceptions must be caught or they will be 'uncaught'
                // Respond with an errored Response
                ret = Response.Error();
            }
            return ret;
        }
        /// <summary>
        /// Occurs when a ServiceWorkerRegistration acquires a new ServiceWorkerRegistration.installing worker.
        /// </summary>
        protected virtual Task ServiceWorker_OnInstallAsync(ExtendableEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when incoming messages are received. Controlled pages can use the MessagePort.postMessage() method to send messages to service workers.
        /// </summary>
        protected virtual Task ServiceWorker_OnMessageAsync(ExtendableMessageEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when incoming messages can't be deserialized.
        /// </summary>
        protected virtual Task ServiceWorker_OnMessageErrorAsync(ExtendableMessageEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a user clicks on a displayed notification.
        /// </summary>
        protected virtual Task ServiceWorker_OnNotificationClickAsync(NotificationEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a user closes a displayed notification.
        /// </summary>
        protected virtual Task ServiceWorker_OnNotificationCloseAsync(NotificationEvent e) => Task.CompletedTask;
        /// <summary>
        /// Fired on a payment app when a payment flow has been initiated on the merchant website via the PaymentRequest.show() method.
        /// </summary>
        protected virtual Task ServiceWorker_OnPaymentRequestAsync(PaymentRequestEvent e) => Task.CompletedTask;
        /// <summary>
        /// The periodicsync event of the ServiceWorkerGlobalScope interface is fired at timed intervals, specified when registering a PeriodicSyncManager.
        /// </summary>
        protected virtual Task ServiceWorker_OnPeriodicSyncAsync(PeriodicSyncEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a server push notification is received.
        /// </summary>
        protected virtual Task ServiceWorker_OnPushAsync(PushEvent e) => Task.CompletedTask;
        /// <summary>
        /// Occurs when a push subscription has been invalidated, or is about to be invalidated (e.g. when a push service sets an expiration time).
        /// </summary>
        protected virtual Task ServiceWorker_OnPushSubscriptionChangeAsync(PushSubscriptionChangeEvent e) => Task.CompletedTask;
        /// <summary>
        /// Triggered when a call to SyncManager.register is made from a service worker client page. The attempt to sync is made either immediately if the network is available or as soon as the network becomes available.
        /// </summary>
        protected virtual Task ServiceWorker_OnSyncAsync(SyncEvent e) => Task.CompletedTask;
        #endregion
        #region Missed event intermediate handlers
        void ServiceWorker_OnActivate(ExtendableEvent e)
        {
            if (e is MissedExtendableEvent missedEvent && missedEvent.IsExtended)
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
        void ServiceWorker_OnBackgroundFetchAbort(BackgroundFetchEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnBackgroundFetchAbortAsync(e);
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
                e.WaitUntil(ServiceWorker_OnBackgroundFetchAbortAsync(e));
            }
        }
        void ServiceWorker_OnBackgroundFetchClick(BackgroundFetchEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnBackgroundFetchClickAsync(e);
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
                e.WaitUntil(ServiceWorker_OnBackgroundFetchClickAsync(e));
            }
        }
        void ServiceWorker_OnBackgroundFetchFail(BackgroundFetchUpdateUIEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnBackgroundFetchFailAsync(e);
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
                e.WaitUntil(ServiceWorker_OnBackgroundFetchFailAsync(e));
            }
        }
        void ServiceWorker_OnBackgroundFetchSuccess(BackgroundFetchUpdateUIEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnBackgroundFetchSuccessAsync(e);
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
                e.WaitUntil(ServiceWorker_OnBackgroundFetchSuccessAsync(e));
            }
        }
        void ServiceWorker_OnCanMakePayment(CanMakePaymentEvent e)
        {
            if (e is MissedCanMakePaymentEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        var response = await ServiceWorker_OnCanMakePaymentAsync(e);
                        missedEvent.ResponseResolve(response);
                    }
                    catch
                    {
                        missedEvent.ResponseReject();
                    }
                });
            }
            else
            {
                e.RespondWith(ServiceWorker_OnCanMakePaymentAsync(e));
            }
        }
        void ServiceWorker_OnContentDelete(ContentIndexEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnContentDeleteAsync(e);
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
                e.WaitUntil(ServiceWorker_OnContentDeleteAsync(e));
            }
        }
        void ServiceWorker_OnCookieChange(ExtendableCookieChangeEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnCookieChangeAsync(e);
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
                e.WaitUntil(ServiceWorker_OnCookieChangeAsync(e));
            }
        }
        void ServiceWorker_OnFetch(FetchEvent e)
        {
            if (e is MissedFetchEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        var response = await ServiceWorker_OnFetchAsync(e);
                        missedEvent.ResponseResolve(response);
                    }
                    catch
                    {
                        missedEvent.ResponseResolve(Response.Error());
                    }
                });
            }
            else
            {
                e.RespondWith(ServiceWorker_OnFetchAsync(e));
            }
        }
        void ServiceWorker_OnInstall(ExtendableEvent e)
        {
            if (e is MissedExtendableEvent missedEvent && missedEvent.IsExtended)
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
        void ServiceWorker_OnMessage(ExtendableMessageEvent e)
        {
            if (e is MissedExtendableMessageEvent missedEvent && missedEvent.IsExtended)
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
        void ServiceWorker_OnMessageError(ExtendableMessageEvent e)
        {
            if (e is MissedExtendableMessageEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnMessageErrorAsync(e);
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
                e.WaitUntil(ServiceWorker_OnMessageErrorAsync(e));
            }
        }
        void ServiceWorker_OnNotificationClick(NotificationEvent e)
        {
            if (e is MissedNotificationEvent missedEvent && missedEvent.IsExtended)
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
        void ServiceWorker_OnNotificationClose(NotificationEvent e)
        {
            if (e is MissedNotificationEvent missedEvent && missedEvent.IsExtended)
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
        void ServiceWorker_OnPaymentRequest(PaymentRequestEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnPaymentRequestAsync(e);
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
                e.WaitUntil(ServiceWorker_OnPaymentRequestAsync(e));
            }
        }
        void ServiceWorker_OnPeriodicSync(PeriodicSyncEvent e)
        {
            if (e is MissedPeriodicSyncEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnPeriodicSyncAsync(e);
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
                e.WaitUntil(ServiceWorker_OnPeriodicSyncAsync(e));
            }
        }
        void ServiceWorker_OnPush(PushEvent e)
        {
            if (e is MissedPushEvent missedEvent && missedEvent.IsExtended)
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
        void ServiceWorker_OnPushSubscriptionChange(PushSubscriptionChangeEvent e)
        {
            if (e is IMissedEvent missedEvent && missedEvent.IsExtended)
            {
                Async.Run(async () =>
                {
                    try
                    {
                        await ServiceWorker_OnPushSubscriptionChangeAsync(e);
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
                e.WaitUntil(ServiceWorker_OnPushSubscriptionChangeAsync(e));
            }
        }
        void ServiceWorker_OnSync(SyncEvent e)
        {
            if (e is MissedSyncEvent missedEvent && missedEvent.IsExtended)
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
        #endregion
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


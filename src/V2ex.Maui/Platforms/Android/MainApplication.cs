﻿using Android.App;
using Android.Runtime;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using Microsoft.Extensions.Options;
using V2ex.Maui.Platforms.Android;

namespace V2ex.Maui
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                Microsoft.Maui.Handlers.WebViewHandler.Mapper.ModifyMapping(
                    nameof(Android.Webkit.WebViewClient),
                    (handler, view, args) =>
                    {
                        handler.PlatformView.Settings.JavaScriptEnabled = true;
                        handler.PlatformView.Settings.AllowContentAccess = true;
                        handler.PlatformView.Settings.DomStorageEnabled = true;
                        handler.PlatformView.AddJavascriptInterface(new InteropInterface(handler.VirtualView), "interop");
                        handler.PlatformView.SetWebViewClient(new WebViewClient());
                    });
            }

            var app = MauiProgram.CreateMauiApp(FilesDir!.AbsolutePath);

            var appCenterOptions = app.Services.GetRequiredService<IOptions<AppCenterOptions>>();
            AppCenter.Start(appCenterOptions.Value.Secret,
                typeof(Analytics), typeof(Crashes), typeof(Distribute));
            Distribute.SetEnabledForDebuggableBuild(true);
            return app;
        }
    }
}

public class WebViewClient : Android.Webkit.WebViewClient
{
    public override void OnPageFinished(Android.Webkit.WebView? view, string? url)
    {
        base.OnPageFinished(view, url);
        var script = @"var height = document.body.scrollHeight;
                      interop.setHeight(height);";
        view?.EvaluateJavascript(script, null);
    }
}
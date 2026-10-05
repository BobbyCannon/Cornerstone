#import <WebKit/WebKit.h>
#include <stdlib.h>
#include <string.h>

static char* CsnCopyUtf8(NSString* value)
{
    if (value == nil)
        return strdup("");
    const char* utf = [value UTF8String];
    return utf ? strdup(utf) : strdup("");
}

extern "C" void CsnFree(void* pointer)
{
    free(pointer);
}

extern "C" void* CsnWkWebViewCreate(void)
{
    WKWebViewConfiguration* configuration = [WKWebViewConfiguration new];
    WKWebView* view = [[WKWebView alloc] initWithFrame:NSZeroRect configuration:configuration];
    [view setWantsLayer:YES];
    return (__bridge_retained void*)view;
}

extern "C" void CsnWkWebViewRelease(void* view)
{
    if (view == nullptr)
        return;
    (void)(__bridge_transfer WKWebView*)view;
}

extern "C" void CsnWkWebViewLoadUrl(void* view, const char* url)
{
    if (view == nullptr || url == nullptr)
        return;
    WKWebView* webView = (__bridge WKWebView*)view;
    NSString* text = [NSString stringWithUTF8String:url];
    NSURL* nsUrl = [NSURL URLWithString:text];
    if (nsUrl == nil)
        return;
    [webView loadRequest:[NSURLRequest requestWithURL:nsUrl]];
}

extern "C" void CsnWkWebViewLoadHtml(void* view, const char* html)
{
    if (view == nullptr)
        return;
    WKWebView* webView = (__bridge WKWebView*)view;
    NSString* text = [NSString stringWithUTF8String:html != nullptr ? html : ""];
    [webView loadHTMLString:text baseURL:[NSURL URLWithString:@"about:blank"]];
}

extern "C" void CsnWkWebViewReload(void* view)
{
    if (view == nullptr)
        return;
    [(__bridge WKWebView*)view reload];
}

extern "C" void CsnWkWebViewStop(void* view)
{
    if (view == nullptr)
        return;
    [(__bridge WKWebView*)view stopLoading];
}

extern "C" int CsnWkWebViewGoBack(void* view)
{
    WKWebView* webView = (__bridge WKWebView*)view;
    if (webView == nil || !webView.canGoBack)
        return 0;
    [webView goBack];
    return 1;
}

extern "C" int CsnWkWebViewGoForward(void* view)
{
    WKWebView* webView = (__bridge WKWebView*)view;
    if (webView == nil || !webView.canGoForward)
        return 0;
    [webView goForward];
    return 1;
}

extern "C" int CsnWkWebViewCanGoBack(void* view)
{
    WKWebView* webView = (__bridge WKWebView*)view;
    return webView != nil && webView.canGoBack ? 1 : 0;
}

extern "C" int CsnWkWebViewCanGoForward(void* view)
{
    WKWebView* webView = (__bridge WKWebView*)view;
    return webView != nil && webView.canGoForward ? 1 : 0;
}

extern "C" void CsnWkWebViewSetHidden(void* view, int hidden)
{
    if (view == nullptr)
        return;
    [(__bridge WKWebView*)view setHidden:hidden != 0];
}

extern "C" char* CsnWkWebViewCopyTitle(void* view)
{
    WKWebView* webView = (__bridge WKWebView*)view;
    return CsnCopyUtf8(webView.title);
}

extern "C" char* CsnWkWebViewCopyUrl(void* view)
{
    WKWebView* webView = (__bridge WKWebView*)view;
    return CsnCopyUtf8(webView.URL.absoluteString);
}

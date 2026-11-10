package com.dawnchronicle.game;

import android.app.Activity;
import android.annotation.SuppressLint;
import android.app.AlertDialog;
import android.graphics.Color;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.util.Collections;

/** Offline asset origin: no network permission, remote navigation, or JavaScript bridge. */
public final class MainActivity extends Activity {
    private static final String HOST = "appassets.androidplatform.net";
    private WebView webView;

    // JavaScript is the bundled game engine; only the intercepted local HTTPS origin can load.
    @SuppressLint("SetJavaScriptEnabled")
    @Override public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        FrameLayout root = new FrameLayout(this);
        root.setBackgroundColor(Color.rgb(20, 43, 36));
        webView = new WebView(this);
        webView.setBackgroundColor(Color.rgb(246, 243, 233));
        root.addView(webView, new FrameLayout.LayoutParams(-1, -1));
        setContentView(root);

        // Target 35 enables edge-to-edge; keep all controls clear of system bars and keyboard.
        if (Build.VERSION.SDK_INT >= 30) {
            getWindow().setDecorFitsSystemWindows(false);
            root.setOnApplyWindowInsetsListener((view, insets) -> {
                android.graphics.Insets bars = insets.getInsets(
                    android.view.WindowInsets.Type.systemBars()
                    | android.view.WindowInsets.Type.displayCutout()
                    | android.view.WindowInsets.Type.ime());
                view.setPadding(bars.left, bars.top, bars.right, bars.bottom);
                return insets;
            });
        }

        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);
        settings.setBlockNetworkLoads(true);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        settings.setGeolocationEnabled(false);
        settings.setMediaPlaybackRequiresUserGesture(true);
        settings.setSupportZoom(false);
        settings.setTextZoom(100);
        webView.setWebViewClient(new WebViewClient() {
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                return !isLocal(request.getUrl());
            }
            @Override public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
                Uri url = request.getUrl();
                if (!isLocal(url)) return missing();
                String path = url.getPath();
                if (path == null || path.equals("/")) path = "/index.html";
                path = path.substring(1);
                if (path.contains("..") || path.contains("\\")) return missing();
                String mime = path.endsWith(".html") ? "text/html"
                    : path.endsWith(".mjs") || path.endsWith(".js") ? "text/javascript"
                    : path.endsWith(".css") ? "text/css"
                    : path.endsWith(".png") ? "image/png"
                    : path.endsWith(".svg") ? "image/svg+xml"
                    : path.endsWith(".woff2") ? "font/woff2"
                    : path.endsWith(".ttf") ? "font/ttf" : "text/plain";
                try {
                    return new WebResourceResponse(mime, "UTF-8", 200, "OK",
                        Collections.singletonMap("Cache-Control", "no-cache"), getAssets().open(path));
                } catch (IOException error) { return missing(); }
            }
        });
        webView.loadUrl("https://" + HOST + "/index.html");
        if (Build.VERSION.SDK_INT >= 33) {
            getOnBackInvokedDispatcher().registerOnBackInvokedCallback(
                android.window.OnBackInvokedDispatcher.PRIORITY_DEFAULT, this::handleBack);
        }
    }
    private static boolean isLocal(Uri uri) {
        return "https".equals(uri.getScheme()) && HOST.equals(uri.getHost());
    }
    private static WebResourceResponse missing() {
        return new WebResourceResponse("text/plain", "UTF-8", 404, "Not Found",
            Collections.emptyMap(), new ByteArrayInputStream(new byte[0]));
    }
    @Override public void onBackPressed() {
        handleBack();
    }
    private void handleBack() {
        webView.evaluateJavascript("window.onNativeBack ? window.onNativeBack() : false", handled -> {
            if (!"true".equals(handled)) {
                new AlertDialog.Builder(this).setTitle("잠시 책을 덮을까요?")
                    .setMessage("진행은 자동 저장됩니다. 같은 순간에서 다시 이어갈 수 있어요.")
                    .setNegativeButton("계속 모험하기", null)
                    .setPositiveButton("닫기", (dialog, which) -> finish()).show();
            }
        });
    }
    @Override protected void onPause() { super.onPause(); webView.onPause(); }
    @Override protected void onResume() { super.onResume(); if (webView != null) webView.onResume(); }
    @Override protected void onDestroy() { webView.destroy(); super.onDestroy(); }
}

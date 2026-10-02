using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Text;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace ImageProcessing.SurveyCef.Views;

public partial class WebViewer : UserControl, IDisposable
{
	const string AboutBlank = "about:blank";

	private readonly AvaloniaCefBrowser _browser;
	private string? pendingHtml;

	public WebViewer()
	{
		InitializeComponent();

		_browser = new AvaloniaCefBrowser();
		_browser.BrowserInitialized += () => SetStatus("CEF browser initialized");
		_browser.LoadStart += OnLoadStart;
		_browser.LoadEnd += OnLoadEnd;
		_browser.LoadError += OnLoadError;
		_browser.Address = AboutBlank;

		BrowserContainer.Content = _browser;
		SetStatus("creating CEF browser...");
	}

	private void OpenGoogle_Click(object? sender, RoutedEventArgs e)
		=> Navigate("https://google.com");

	private void OpenDevTools_Click(object? sender, RoutedEventArgs e)
		=> _browser.ShowDeveloperTools();

	private void AddressBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;
		string? url = AddressBox.Text?.Trim();
		if (!string.IsNullOrEmpty(url)) Navigate(url);
	}

	private void LoadCustomHtml_Click(object? sender, RoutedEventArgs e)
	{
		string html = "<html><body style='font-family:sans-serif;'><h1>Привет!</h1>"
			+ "<p>Эта HTML страница отображается в Avalonia 11 через CefGlue.</p>"
			+ "</body></html>";

		// Chromium blocks top level data: URL navigations, so the markup is written into the
		// already loaded document instead of navigating to a data URI.
		if (_browser.IsBrowserInitialized && _browser.Address == AboutBlank)
		{
			WriteHtml(html);
			return;
		}

		pendingHtml = html;
		_browser.Address = AboutBlank;
	}

	void Navigate(string url)
	{
		pendingHtml = null;
		_browser.Address = url;
	}

	private void OnLoadStart(object? sender, LoadStartEventArgs e)
	{
		if (!e.Frame.IsMain) return;
		string url = e.Frame.Url;
		SetStatus($"loading: {url}");
	}

	private void OnLoadEnd(object? sender, LoadEndEventArgs e)
	{
		if (!e.Frame.IsMain) return;

		string url = e.Frame.Url;
		int status = e.HttpStatusCode;
		SetStatus($"loaded: {url} ({status})");

		string? html = pendingHtml;
		if (html == null) return;
		pendingHtml = null;
		Dispatcher.UIThread.Post(() => WriteHtml(html));
	}

	private void OnLoadError(object? sender, LoadErrorEventArgs e)
	{
		if (!e.Frame.IsMain || e.ErrorCode == CefErrorCode.Aborted) return;

		string url = e.FailedUrl;
		string text = e.ErrorText;
		CefErrorCode code = e.ErrorCode;
		SetStatus($"load error: {code} {text} ({url})");
	}

	void WriteHtml(string html)
	{
		string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
		_browser.ExecuteJavaScript(
			"document.open();"
			+ $"document.write(new TextDecoder().decode(Uint8Array.from(atob(\"{base64}\"), c => c.charCodeAt(0))));"
			+ "document.close();");
	}

	void SetStatus(string message)
	{
		if (Dispatcher.UIThread.CheckAccess()) StatusText.Text = message;
		else Dispatcher.UIThread.Post(() => StatusText.Text = message);
	}

	public void Dispose()
	{
		_browser.Dispose();
		GC.SuppressFinalize(this);
	}
}

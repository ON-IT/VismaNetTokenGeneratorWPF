using ONIT.VismaNetApi;
using System;
using System.Linq;
using System.Windows.Controls;

namespace VismanetWPF
{
    /// <summary>
    /// Interaction logic for VismaNetAuthControl.xaml
    /// </summary>
    public partial class AuthControl : UserControl
    {
        private string callbackUrl;
        private string clientId;

        public string CallbackUrl { get => callbackUrl; set { callbackUrl = value; LoadUrl(); } }
        public string ClientId { get => clientId; set { clientId = value; LoadUrl(); } }
        public string ClientSecret { get; set; }

        public bool ShowTokenIfSuccessful { get; set; }

        public event EventHandler<string> OnTokenReceived;
        public event EventHandler<(Exception, string)> OnError;
        public AuthControl()
        {
            InitializeComponent();
            Loaded += AuthControl_Loaded;
        }

        private void LoadUrl()
        {
            if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(CallbackUrl))
                vnBrowser.Source = new Uri(VismaNet.GetOAuthUrl(ClientId, CallbackUrl));
        }

        private void AuthControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadUrl();
            vnBrowser.NavigationStarting += VnBrowser_NavigationStarting;
        }

        private async void VnBrowser_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            try
            {
                var query = new Uri(e.Uri).Query.TrimStart('?')
                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Split('='))
                    .Where(k => k.Length == 2)
                    .ToLookup(a => a[0], a => Uri.UnescapeDataString(a[1])
                      , StringComparer.OrdinalIgnoreCase);

                var code = query["code"].FirstOrDefault();
                if (query != null && !string.IsNullOrEmpty(code))
                {
                    vnBrowser.NavigateToString(CreateTemplatedResult("Fetching token from Visma.net...", "Fetching token from Visma.net. Please wait..."));
                    var token = await VismaNet.GetTokenUsingOAuth(ClientId, ClientSecret, code, CallbackUrl);
                    if (token != null)
                    {
                        vnBrowser.NavigateToString(CreateTemplatedResult("Token fetched from Visma.net", "The token was successfully fetched from Visma.net." + (ShowTokenIfSuccessful == true ? $"<br/><br/>Token: {token}" : string.Empty), "green darken-2"));
                        OnTokenReceived?.Invoke(this, token);
                        return;
                    }
                    else
                    {
                        vnBrowser.NavigateToString(CreateTemplatedResult("Token fetch failed...", "We have no idea why...", "red darken-2"));
                        OnError?.Invoke(this, (null, "Token fetch failed"));
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                vnBrowser.NavigateToString(CreateTemplatedResult("Token fetch failed...", ex.Message, "red darken-2"));
                OnError?.Invoke(this, (ex, ex.Message));
            }
        }

        private static string CreateTemplatedResult(string title, string content,
          string background = "blue-grey darken-1")
        {
            return "<html>" +
                    "<head>" +
                    "<link href=\"https://fonts.googleapis.com/icon?family=Material+Icons\" rel=\"stylesheet\">" +
                    "<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/materialize/1.0.0/css/materialize.min.css\" integrity=\"sha256-OweaP/Ic6rsV+lysfyS4h+LM6sRwuO3euTYfr6M124g=\" crossorigin=\"anonymous\" />" +
                    "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"/>" +
                    "<title>Visma.net Integrations Token Generator</title>" +
                    "</head>" +
                    "<body>" +
                    "<div class=\'container\'>" +
                    "" +
                    "<div class=\'row\'>" +
                    "<div class=\'col s12 m8 offset-m2\'>" +
                    $"<div class='card {background} z-depth-5'>" +
                    "<div class=\'card-content white-text\'>" +
                    $"<span class='card-title'>{title}</span>" +
                    $"{content}" +
                    "</div>" +
                    "</div>" +
                    "<a href=\'https://on-it.no\' target=\'_blank\'>" +
                    "<img src=\"https://www.on-it.no/wp-content/themes/on-it/style/images/on_it_logo.png\" width=\"100px\" class=\'right\' />" +
                    "</a>" +
                    "</div>" +
                    "</div>" +
                    "" +
                    "</div>" +
                    "</body>" +
                    "</html>";
        }
    }
}

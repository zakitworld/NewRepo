using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Controls
{
    /// <summary>
    /// A thin red banner shown at the top of any page when there is no internet connection.
    /// Usage in XAML:
    ///   xmlns:controls="clr-namespace:OnlineVoting_and_Ticketing_app.Controls"
    ///   &lt;controls:ConnectivityBanner /&gt;
    /// </summary>
    public partial class ConnectivityBanner : ContentView
    {
        private IConnectivityService? _connectivity;

        public ConnectivityBanner()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            _connectivity = Handler?.MauiContext?.Services
                .GetService(typeof(IConnectivityService)) as IConnectivityService;

            if (_connectivity is null) return;

            UpdateVisibility(_connectivity.IsConnected);
            _connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object? sender, bool isConnected) =>
            MainThread.BeginInvokeOnMainThread(() => UpdateVisibility(isConnected));

        private void UpdateVisibility(bool isConnected)
        {
            if (!isConnected)
            {
                BannerFrame.IsVisible = true;
                // Slide in from above using ViewExtensions.TranslateTo
                BannerFrame.TranslationY = -40;
                _ = BannerFrame.TranslateToAsync(0, 0, 300, Easing.CubicOut);
            }
            else
            {
                BannerFrame.IsVisible = false;
                BannerFrame.TranslationY = 0;
            }
        }
    }
}

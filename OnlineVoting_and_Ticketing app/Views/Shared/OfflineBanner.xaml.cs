using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Shared
{
    public partial class OfflineBanner : ContentView
    {
        public OfflineBanner()
        {
            InitializeComponent();

            // Show immediately if already offline
            BannerRoot.IsVisible = !Connectivity.Current.NetworkAccess.Equals(NetworkAccess.Internet);

            // React to connectivity changes for the lifetime of this view
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BannerRoot.IsVisible = e.NetworkAccess != NetworkAccess.Internet;
            });
        }

        // Clean up the event subscription when the view is unloaded to prevent leaks
        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);
            if (args.NewHandler == null)
                Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }
    }
}

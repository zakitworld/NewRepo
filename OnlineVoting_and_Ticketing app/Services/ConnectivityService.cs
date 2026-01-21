namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IConnectivityService
    {
        bool IsConnected { get; }
        event EventHandler<bool> ConnectivityChanged;
    }

    public class ConnectivityService : IConnectivityService
    {
        public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        public event EventHandler<bool>? ConnectivityChanged;

        public ConnectivityService()
        {
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
        }
    }
}

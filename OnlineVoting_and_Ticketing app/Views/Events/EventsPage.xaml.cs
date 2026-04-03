using OnlineVoting_and_Ticketing_app.ViewModels.Events;

namespace OnlineVoting_and_Ticketing_app.Views.Events
{
    public partial class EventsPage : ContentPage
    {
        private readonly EventsViewModel _viewModel;

        public EventsPage(EventsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadEventsCommand.ExecuteAsync(null);
        }
    }
}

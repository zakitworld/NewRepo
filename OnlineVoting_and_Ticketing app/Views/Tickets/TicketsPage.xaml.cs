using OnlineVoting_and_Ticketing_app.ViewModels.Tickets;

namespace OnlineVoting_and_Ticketing_app.Views.Tickets
{
    public partial class TicketsPage : ContentPage
    {
        private readonly TicketsViewModel _viewModel;

        public TicketsPage(TicketsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTicketsCommand.ExecuteAsync(null);
        }
    }
}

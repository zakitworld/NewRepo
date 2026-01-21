using OnlineVoting_and_Ticketing_app.ViewModels.Auth;

namespace OnlineVoting_and_Ticketing_app.Views.Auth
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

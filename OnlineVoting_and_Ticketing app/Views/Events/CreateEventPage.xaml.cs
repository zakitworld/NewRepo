using Microsoft.Maui.Controls.Shapes;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Events
{
    public partial class CreateEventPage : ContentPage
    {
        private readonly IEventService _eventService;
        private string _selectedImageUrl = string.Empty;
        private List<TicketTypeView> _ticketTypeViews = new();

        public CreateEventPage(IEventService eventService)
        {
            InitializeComponent();
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

            try
            {
                // Set minimum and default dates
                StartDatePicker.MinimumDate = DateTime.Now;
                EndDatePicker.MinimumDate = DateTime.Now;
                StartDatePicker.Date = DateTime.Now.AddDays(7);
                EndDatePicker.Date = DateTime.Now.AddDays(7);
                StartTimePicker.Time = new TimeSpan(10, 0, 0);
                EndTimePicker.Time = new TimeSpan(18, 0, 0);

                // Add first ticket type by default
                AddTicketTypeView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateEventPage initialization error: {ex.Message}");
            }
        }

        private async void OnSelectImageTapped(object? sender, EventArgs e)
        {
            try
            {
                var results = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
                {
                    Title = "Select Event Image"
                });

                var result = results?.FirstOrDefault();

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    EventImagePreview.Source = ImageSource.FromStream(() => stream);
                    EventImagePreview.IsVisible = true;
                    ImagePlaceholder.IsVisible = false;
                    _selectedImageUrl = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to select image: {ex.Message}", "OK");
            }
        }

        private void OnImageUrlChanged(object? sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                EventImagePreview.Source = e.NewTextValue;
                EventImagePreview.IsVisible = true;
                ImagePlaceholder.IsVisible = false;
                _selectedImageUrl = e.NewTextValue;
            }
            else if (string.IsNullOrWhiteSpace(_selectedImageUrl))
            {
                EventImagePreview.IsVisible = false;
                ImagePlaceholder.IsVisible = true;
            }
        }

        private void OnAddTicketTypeClicked(object? sender, EventArgs e)
        {
            AddTicketTypeView();
        }

        private void AddTicketTypeView()
        {
            var ticketTypeView = new TicketTypeView();
            ticketTypeView.RemoveRequested += (s, e) => RemoveTicketTypeView(s as TicketTypeView);
            _ticketTypeViews.Add(ticketTypeView);
            TicketTypesContainer.Children.Add(ticketTypeView);
        }

        private async void RemoveTicketTypeView(TicketTypeView? view)
        {
            if (view != null && _ticketTypeViews.Count > 1)
            {
                _ticketTypeViews.Remove(view);
                TicketTypesContainer.Children.Remove(view);
            }
            else if (_ticketTypeViews.Count == 1)
            {
                await DisplayAlertAsync("Error", "At least one ticket type is required", "OK");
            }
        }

        private async void OnCreateEventClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                ShowError("Event title is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionEditor.Text))
            {
                ShowError("Event description is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(LocationEntry.Text))
            {
                ShowError("Event location is required");
                return;
            }

            if (CategoryPicker.SelectedIndex < 0)
            {
                ShowError("Please select an event category");
                return;
            }

            if (EventTypePicker.SelectedIndex < 0)
            {
                ShowError("Please select an event type");
                return;
            }

            // Combine date and time
            var startDateTime = StartDatePicker.Date + StartTimePicker.Time;
            var endDateTime = EndDatePicker.Date + EndTimePicker.Time;

            if (endDateTime <= startDateTime)
            {
                ShowError("End date must be after start date");
                return;
            }

            // Validate ticket types
            var ticketTypes = new List<TicketType>();
            foreach (var ticketView in _ticketTypeViews)
            {
                var ticketType = ticketView.GetTicketType();
                if (ticketType == null)
                {
                    ShowError("Please fill in all ticket type details");
                    return;
                }
                ticketTypes.Add(ticketType);
            }

            if (ticketTypes.Count == 0)
            {
                ShowError("At least one ticket type is required");
                return;
            }

            CreateButton.IsEnabled = false;
            CreateButton.Text = "Creating...";

            try
            {
                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                var userName = await SecureStorage.GetAsync(AppConstants.Preferences.UserName) ?? "Organizer";

                var newEvent = new Event
                {
                    Title = TitleEntry.Text.Trim(),
                    Description = DescriptionEditor.Text.Trim(),
                    Location = LocationEntry.Text.Trim(),
                    ImageUrl = _selectedImageUrl,
                    OrganizerId = userId ?? string.Empty,
                    OrganizerName = userName,
                    StartDate = startDateTime ?? DateTime.UtcNow,
                    EndDate = endDateTime ?? DateTime.UtcNow.AddDays(1),
                    Category = Enum.Parse<EventCategory>(CategoryPicker.SelectedItem.ToString()!),
                    Type = Enum.Parse<EventType>(EventTypePicker.SelectedItem.ToString()!),
                    Status = EventStatus.Published,
                    TicketTypes = ticketTypes,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Calculate total tickets
                newEvent.TotalTickets = ticketTypes.Sum(t => t.TotalQuantity);
                newEvent.AvailableTickets = newEvent.TotalTickets;

                var (success, error, eventId) = await _eventService.CreateEventAsync(newEvent);

                if (success && !string.IsNullOrEmpty(eventId))
                {
                    await DisplayAlertAsync("Success", AppConstants.Messages.EventCreatedSuccess, "OK");
                    await Shell.Current.GoToAsync("//events");
                }
                else
                {
                    ShowError(error ?? "Failed to create event");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                CreateButton.IsEnabled = true;
                CreateButton.Text = "Create Event";
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            var confirm = await DisplayAlertAsync("Cancel", "Are you sure you want to cancel? All changes will be lost.", "Yes", "No");
            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }
    }

    // Custom view for ticket type input
    public class TicketTypeView : Border
    {
        private Entry _nameEntry;
        private Editor _descriptionEditor;
        private Entry _priceEntry;
        private Entry _quantityEntry;

        public event EventHandler? RemoveRequested;

        public TicketTypeView()
        {
            StrokeShape = new RoundRectangle { CornerRadius = 15 };
            Stroke = (Color)(Application.Current?.Resources["GlassBorderBrush"] ?? Colors.Transparent);
            StrokeThickness = 1;
            BackgroundColor = (Color)(Application.Current?.Resources["GlassBrush"] ?? Colors.Transparent);
            Padding = 20;

            var layout = new VerticalStackLayout { Spacing = 15 };

            // Header with remove button
            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var headerLabel = new Label
            {
                Text = "TICKET TIER",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 1,
                TextColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray)
            };

            var removeButton = new Label
            {
                Text = "Remove",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)(Application.Current?.Resources["Error"] ?? Colors.Red),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            removeButton.GestureRecognizers.Add(tapGesture);

            header.Add(headerLabel, 0);
            header.Add(removeButton, 1);
            layout.Children.Add(header);

            // Name
            var nameBorder = new Border { Padding = new Thickness(15, 2), HeightRequest = 48 };
            _nameEntry = new Entry
            {
                Placeholder = "Tier Name (e.g. VIP)",
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 14
            };
            nameBorder.Content = _nameEntry;
            layout.Children.Add(nameBorder);

            // Description
            var descBorder = new Border { Padding = new Thickness(15, 8) };
            _descriptionEditor = new Editor
            {
                Placeholder = "Optionally describe what this tier includes...",
                HeightRequest = 70,
                AutoSize = EditorAutoSizeOption.TextChanges,
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 14,
                BackgroundColor = Colors.Transparent
            };
            descBorder.Content = _descriptionEditor;
            layout.Children.Add(descBorder);

            // Price and Quantity
            var priceQuantityGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 15
            };

            var priceBorder = new Border { Padding = new Thickness(15, 2), HeightRequest = 48 };
            _priceEntry = new Entry
            {
                Placeholder = "Price (GH₵)",
                Keyboard = Keyboard.Numeric,
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 14
            };
            priceBorder.Content = _priceEntry;

            var qtyBorder = new Border { Padding = new Thickness(15, 2), HeightRequest = 48 };
            _quantityEntry = new Entry
            {
                Placeholder = "Quantity",
                Keyboard = Keyboard.Numeric,
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 14
            };
            qtyBorder.Content = _quantityEntry;

            priceQuantityGrid.Add(priceBorder, 0);
            priceQuantityGrid.Add(qtyBorder, 1);
            layout.Children.Add(priceQuantityGrid);

            Content = layout;
        }

        public TicketType? GetTicketType()
        {
            if (string.IsNullOrWhiteSpace(_nameEntry.Text) ||
                string.IsNullOrWhiteSpace(_priceEntry.Text) ||
                string.IsNullOrWhiteSpace(_quantityEntry.Text))
            {
                return null;
            }

            if (!decimal.TryParse(_priceEntry.Text, out var price) || price < 0)
            {
                return null;
            }

            if (!int.TryParse(_quantityEntry.Text, out var quantity) || quantity <= 0)
            {
                return null;
            }

            return new TicketType
            {
                Id = Guid.NewGuid().ToString(),
                Name = _nameEntry.Text.Trim(),
                Description = _descriptionEditor.Text?.Trim() ?? string.Empty,
                Price = price,
                TotalQuantity = quantity,
                AvailableQuantity = quantity
            };
        }
    }
}

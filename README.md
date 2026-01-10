# EventHub - Online Voting & Ticketing App

A modern, feature-rich .NET MAUI mobile application for event management, ticket sales, and voting/polling - designed for Android and iOS.

## Features

### 🎫 Event Management
- Browse and discover upcoming events
- View detailed event information with images
- Filter events by category (Music, Sports, Conference, Workshop, etc.)
- Search events by title, description, or location
- Real-time availability tracking

### 💳 Ticketing System
- Secure ticket purchasing with Paystack payment integration
- Multiple ticket types per event
- QR code generation for each ticket
- Ticket validation and check-in system
- View ticket history and status
- Share tickets with friends

### 📊 Voting & Polls
- Create and participate in polls
- Single and multiple-choice voting
- Real-time vote counting
- Anonymous voting options
- Poll results visualization
- Event-specific or standalone polls

### 🔐 Authentication
- Email/Password authentication
- Google Sign-In
- Apple Sign-In
- Facebook Login
- Password reset functionality
- Secure session management

### 👤 User Profile
- Personalized user dashboard
- View statistics (events, tickets, votes)
- Manage account settings
- Logout functionality

## Technology Stack

### Framework & Platform
- **.NET MAUI** (Multi-platform App UI) - .NET 10.0
- **C#** - Primary programming language
- **XAML** - UI markup

### Backend Services
- **Firebase Authentication** - User authentication
- **Firebase Realtime Database** - Data storage
- **Firebase Storage** - Image and file storage

### Payment Integration
- **Paystack** - Payment processing for ticket purchases
- Supports multiple currencies (GHS, NGN, etc.)

### Third-Party Libraries
- **CommunityToolkit.Mvvm** - MVVM helpers
- **QRCoder** - QR code generation
- **SkiaSharp** - Graphics rendering
- **FFImageLoading.Maui** - Image caching and loading
- **Newtonsoft.Json** - JSON serialization

## Architecture

### MVVM Pattern
The app follows the Model-View-ViewModel (MVVM) architectural pattern:

```
OnlineVoting_and_Ticketing app/
├── Models/               # Data models
│   ├── User.cs
│   ├── Event.cs
│   ├── Ticket.cs
│   ├── Poll.cs
│   └── Vote.cs
├── Views/                # UI pages
│   ├── Auth/
│   │   ├── LoginPage.xaml
│   │   └── RegisterPage.xaml
│   ├── Events/
│   │   ├── EventsPage.xaml
│   │   └── EventDetailsPage.xaml
│   ├── Tickets/
│   │   ├── TicketsPage.xaml
│   │   └── TicketDetailsPage.xaml
│   ├── Polls/
│   │   └── PollsPage.xaml
│   └── Profile/
│       └── ProfilePage.xaml
├── ViewModels/           # View models
│   └── BaseViewModel.cs
├── Services/             # Business logic & data access
│   ├── IAuthenticationService.cs
│   ├── IEventService.cs
│   ├── ITicketService.cs
│   ├── IPollService.cs
│   ├── IPaymentService.cs
│   ├── FirebaseAuthenticationService.cs
│   ├── FirebaseEventService.cs
│   ├── FirebaseTicketService.cs
│   ├── FirebasePollService.cs
│   └── PaystackPaymentService.cs
├── Helpers/              # Utility classes
│   ├── ValidationHelper.cs
│   └── DateTimeHelper.cs
└── Constants/            # App constants
    └── AppConstants.cs
```

## Setup Instructions

### Prerequisites

1. **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **Visual Studio 2022** (17.8 or later) with MAUI workload
3. **Android SDK** (for Android development)
4. **Xcode** (for iOS development on macOS)

### Firebase Configuration

1. Create a Firebase project at [Firebase Console](https://console.firebase.google.com)

2. Enable the following services:
   - Authentication (Email/Password, Google, Apple, Facebook)
   - Realtime Database
   - Storage

3. Get your Firebase configuration:
   - Go to Project Settings
   - Copy your Web API Key, Database URL, Project ID, etc.

4. Update `FirebaseConfig.cs` (OnlineVoting_and_Ticketing app/Services/FirebaseConfig.cs):
```csharp
public static class FirebaseConfig
{
    public const string ApiKey = "YOUR_FIREBASE_API_KEY";
    public const string AuthDomain = "YOUR_PROJECT_ID.firebaseapp.com";
    public const string DatabaseUrl = "https://YOUR_PROJECT_ID.firebaseio.com";
    public const string ProjectId = "YOUR_PROJECT_ID";
    public const string StorageBucket = "YOUR_PROJECT_ID.appspot.com";
    public const string MessagingSenderId = "YOUR_MESSAGING_SENDER_ID";
    public const string AppId = "YOUR_APP_ID";
}
```

### Paystack Configuration

1. Create a Paystack account at [Paystack](https://paystack.com)

2. Get your API keys from the dashboard

3. Update `PaystackPaymentService.cs` (OnlineVoting_and_Ticketing app/Services/PaystackPaymentService.cs:9):
```csharp
private const string SecretKey = "YOUR_PAYSTACK_SECRET_KEY";
```

4. Update the currency if needed (default is GHS for Ghana Cedis):
```csharp
currency = "GHS", // Change to NGN for Naira, USD for US Dollar, etc.
```

### Building and Running

#### Android

1. Open the solution in Visual Studio 2022
2. Select an Android device/emulator from the debug target
3. Press F5 or click "Run"

#### iOS (macOS only)

1. Open the solution in Visual Studio for Mac or VS Code
2. Select an iOS device/simulator from the debug target
3. Press F5 or click "Run"

### Database Structure

#### Firebase Realtime Database Rules (example):
```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "events": {
      ".read": true,
      "$eventId": {
        ".write": "auth != null"
      }
    },
    "tickets": {
      "$ticketId": {
        ".read": "auth != null && data.child('userId').val() === auth.uid",
        ".write": "auth != null"
      }
    },
    "polls": {
      ".read": true,
      "$pollId": {
        ".write": "auth != null"
      }
    },
    "votes": {
      "$voteId": {
        ".read": "auth != null",
        ".write": "auth != null"
      }
    }
  }
}
```

## Key Features Explained

### QR Code Ticketing
- Each purchased ticket generates a unique QR code
- QR codes contain the ticket ID for validation
- Event organizers can scan QR codes to check-in attendees
- Tickets show status (Active, Used, Cancelled, Expired)

### Payment Flow
1. User selects event and ticket type
2. Paystack payment window opens in browser
3. User completes payment
4. Payment is verified
5. Ticket is generated and stored
6. User receives ticket with QR code

### Voting System
- Poll creators can set start/end dates
- Support for single or multiple-choice questions
- Vote validation (one vote per user per poll)
- Real-time vote counting
- Results visualization

## App Configuration

### App Identity
- **App Name**: EventHub
- **Bundle ID**: com.eventhub.votingandticketing
- **Version**: 1.0

### Supported Platforms
- Android 5.0 (API 21) and above
- iOS 15.0 and above

## Future Enhancements

- [ ] Event creation and management for organizers
- [ ] Poll creation UI
- [ ] Advanced poll analytics
- [ ] Push notifications
- [ ] In-app messaging
- [ ] Social sharing integration
- [ ] Event recommendations based on user preferences
- [ ] Multi-language support
- [ ] Dark mode customization
- [ ] Offline mode support
- [ ] Ticket transfer between users
- [ ] Event favorites/bookmarks

## Troubleshooting

### Common Issues

**Issue**: Firebase authentication not working
- **Solution**: Ensure you've enabled the authentication methods in Firebase Console and updated the configuration keys

**Issue**: Images not loading
- **Solution**: Check your internet connection and Firebase Storage rules

**Issue**: Payment failing
- **Solution**: Verify your Paystack secret key and ensure test/live mode is correctly set

**Issue**: QR codes not generating
- **Solution**: Ensure the QRCoder and SkiaSharp packages are properly installed

## Contributing

This is a personal project, but suggestions and improvements are welcome!

## License

This project is for educational and portfolio purposes.

## Contact

Developer: Abdul Razak
Email: zakitworld@gmail.com

---

**Built with ❤️ using .NET MAUI**

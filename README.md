# CharityRabbit 🥕

CharityRabbit is an open-source platform for posting and finding volunteer opportunities (Good Works) in your community. Connect with local organizations, discover meaningful ways to serve, and track your impact.

**Live Site:** [CharityRabbit.com](https://charityrabbit.com/)

## 🌟 Features

### For Volunteers
- **🔍 Smart Search & Discovery**
  - Interactive map view with color-coded category markers
  - Advanced filtering by category, location, effort level, and more
  - Search by address with radius-based location filtering
  - Personalized recommendations based on your interests

- **📅 Event Management**
  - Sign up for events and mark interest
  - View your commitments and interested events
  - Track upcoming events in your personalized dashboard
  - Recurring event support (daily, weekly, monthly, yearly)

- **🥕 Gamification**
  - Earn "carrots" for participation (3 per signup, 5 per created event)
  - Track your volunteer impact on your profile
  - See community leaderboards of active do-gooders

- **👥 Social Features**
  - See who else is interested or signed up for events
  - Connect with other volunteers
  - Share events with your network

### For Organizers
- **📝 Event Creation**
  - Easy-to-use form with Google Maps integration
  - Address autocomplete and location picker
  - Support for virtual and in-person events
  - Capacity management (max participants)
  - Rich event details (effort level, accessibility, family-friendly)

- **🔄 Recurring Events**
  - Create events that repeat on a schedule
  - Flexible patterns: daily, weekly (specific days), monthly, yearly
  - Set end dates or repeat indefinitely
  - View upcoming occurrences

- **👨‍👩‍👧‍👦 Participant Management**
  - View all participants who signed up or expressed interest
  - Access contact information (name, email, phone)
  - Export participant lists for communication
  - Track attendance and engagement

- **📊 Event Analytics**
  - See sign-up counts and interest levels
  - Track event capacity and availability
  - View creation date and last modified info

## 🏗️ Architecture

### Technology Stack
- **Frontend:** Blazor Server (.NET 10)
- **UI Framework:** MudBlazor
- **Database:** Neo4j Graph Database
- **Maps:** Google Maps API
- **Authentication:** OpenID Connect (Auth0)
- **Deployment:** Docker, AWS ECS

### Key Components

#### Graph Database Schema
CharityRabbit uses Neo4j to model complex relationships between volunteers, events, and organizations:

```plaintext
(:User)-[:SIGNED_UP_FOR]->(:GoodWork)
(:User)-[:INTERESTED_IN]->(:GoodWork)
(:GoodWork)-[:HAS_CONTACT]->(:Contact)
(:GoodWork)-[:BELONGS_TO]->(:Category)
(:GoodWork)-[:REQUIRES_SKILL]->(:Skill)
(:GoodWork)-[:LOCATED_IN]->(:Location)
(:GoodWork)-[:TAGGED_WITH]->(:Tag)
(:GoodWork)-[:HAS_SUBCATEGORY]->(:SubCategory)
```

#### Node Types

**GoodWork** - Volunteer opportunities with properties:
- Basic: `name`, `description`, `detailedDescription`, `category`, `subCategory`
- Location: `location` (Point), `latitude`, `longitude`, `address`, `isVirtual`
- Timing: `startTime`, `endTime`, `estimatedDuration`, `isRecurring`, `recurrencePattern`
- Logistics: `effortLevel`, `maxParticipants`, `currentParticipants`, `minimumAge`
- Features: `isAccessible`, `familyFriendly`, `parkingAvailable`, `publicTransitAccessible`
- Organization: `organizationName`, `organizationWebsite`
- Impact: `impactDescription`, `estimatedPeopleHelped`
- Status: `status` (Active, Cancelled, Completed, Full), `createdDate`, `createdBy`
- Weather: `outdoorActivity`, `weatherDependent`
- Advanced: `requiredSkills`, `tags`, `specialInstructions`, `whatToBring`

**User** - Volunteer profiles:
- `userId` (unique identifier from auth provider)
- `name`, `email`, `phone`
- Relationships: `SIGNED_UP_FOR`, `INTERESTED_IN`

**Contact** - Event organizer contact information:
- `name`, `email`, `phone`

**Category** - Event categories (Food Bank, Education, Healthcare, etc.)
- `name`, `description`

**Location** - Geographic information:
- `city`, `state`, `country`, `zip`

**Skill** - Required skills for participation:
- `name`, `description`

**Tag** - Flexible categorization:
- `name`

### Services

#### Neo4jService
Core data service managing all database operations:
- Event CRUD operations
- User engagement tracking (sign-ups, interests)
- Search and filtering with complex criteria
- Participant management
- Recommendations and similar events
- Geospatial queries (using spatial POINT indexes for high performance)

#### RecurringEventService
Handles recurring event logic:
- Pattern parsing (DAILY:1, WEEKLY:1:MON,WED,FRI, MONTHLY:1:15, YEARLY:1:3:15)
- Instance generation
- Upcoming occurrence calculation
- Pattern formatting for display

#### GeocodingService
Google Maps integration:
- Address to coordinates conversion
- Reverse geocoding (coordinates to location details)
- City, state, country, zip code extraction

#### GooglePlacesService
Address autocomplete functionality:
- Real-time address suggestions
- Location search integration

## 🚀 Deployment

### Docker Build & Deploy
```bash
# Login to AWS ECR
aws ecr get-login-password --region us-east-2 | docker login --username AWS --password-stdin 555467380508.dkr.ecr.us-east-2.amazonaws.com

# Build Docker image
docker build -t charityrabbit .

# Tag with timestamp
docker tag charityrabbit 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:638759256124735501

# Push to ECR
docker push 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:638759256124735501
```

### Environment Configuration

#### Required Settings (appsettings.json / Environment Variables)
```json
{
  "Neo4jSettings": {
    "Uri": "neo4j+s://your-instance.databases.neo4j.io",
    "Username": "neo4j",
    "Password": "your-password"
  },
  "GoogleMaps": {
    "ApiKey": "your-google-maps-api-key"
  },
  "Oidc": {
    "Authority": "https://your-auth0-domain",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

#### User Secrets (Development)
Store sensitive configuration in user secrets:
```bash
dotnet user-secrets set "Neo4jSettings:Password" "your-password"
dotnet user-secrets set "GoogleMaps:ApiKey" "your-api-key"
dotnet user-secrets set "Oidc:ClientSecret" "your-secret"
```

## 📚 Graph Database Queries

### Example Cypher Queries

**Find all events in a category:**
```cypher
MATCH (g:GoodWork)-[:BELONGS_TO]->(c:Category {name: 'Environmental'})
WHERE g.status = 'Active'
RETURN g
ORDER BY g.startTime
```

**Find events by location:**
```cypher
MATCH (g:GoodWork)-[:LOCATED_IN]->(l:Location {city: 'Huntsville', state: 'Alabama'})
WHERE g.status = 'Active'
RETURN g
```

**Get user's signed up events:**
```cypher
MATCH (u:User {userId: $userId})-[:SIGNED_UP_FOR]->(g:GoodWork)
RETURN g
ORDER BY g.startTime
```

**Find participants for an event:**
```cypher
MATCH (g:GoodWork)
WHERE id(g) = $goodWorkId
OPTIONAL MATCH (u:User)-[r:SIGNED_UP_FOR|INTERESTED_IN]->(g)
RETURN u.userId, u.name, u.email, u.phone, type(r) as relationshipType
ORDER BY type(r) DESC
```

**Get recommended events:**
```cypher
MATCH (u:User {userId: $userId})-[:SIGNED_UP_FOR|INTERESTED_IN]->(myWork:GoodWork)
OPTIONAL MATCH (myWork)-[:BELONGS_TO]->(cat:Category)
MATCH (recommended:GoodWork)-[:BELONGS_TO]->(cat)
WHERE NOT (u)-[:SIGNED_UP_FOR|INTERESTED_IN]->(recommended)
  AND recommended.status = 'Active'
  AND recommended.startTime >= datetime()
RETURN recommended
ORDER BY recommended.startTime
LIMIT 10
```

**Find events within radius:**
```cypher
MATCH (g:GoodWork)
WHERE point.distance(g.location, point({latitude: $centerLat, longitude: $centerLng})) < $radiusMeters
  AND g.status = 'Active'
RETURN g
ORDER BY g.startTime
```

## 🔐 Authentication

CharityRabbit uses OpenID Connect (OIDC) with Auth0 for secure authentication:
- Social login support (Google, GitHub, etc.)
- Secure token management
- Cookie-based sessions with refresh
- Claims-based authorization

### Supported Claims
- `nameidentifier` - Unique user ID
- `name` - Full name
- `givenname` - First name
- `surname` - Last name
- `emailaddress` - Email address
- `email_verified` - Email verification status

## 🛠️ Development

### Prerequisites
- .NET 10 SDK
- Neo4j Database (local or cloud)
- Google Maps API key
- Auth0 account (or compatible OIDC provider)

### Running Locally
1. Clone the repository
```bash
git clone https://github.com/kingjuk/CharityRabbit
cd CharityRabbit
```

2. Set up user secrets
```bash
dotnet user-secrets init
dotnet user-secrets set "Neo4jSettings:Uri" "your-neo4j-uri"
dotnet user-secrets set "Neo4jSettings:Password" "your-password"
dotnet user-secrets set "GoogleMaps:ApiKey" "your-api-key"
dotnet user-secrets set "Oidc:ClientSecret" "your-client-secret"
```

3. Run the application
```bash
dotnet run
```

4. Navigate to `https://localhost:7205`

### Project Structure
```
CharityRabbit/
├── Components/
│   ├── Account/           # Authentication components
│   ├── Layout/            # Layout components (NavMenu, MainLayout)
│   └── Pages/             # Page components
│       ├── Home.razor
│       ├── Profile.razor
│       ├── SearchGoodWorks.razor
│       ├── GoodWorkDetails.razor
│       ├── AddGoodWork.razor
│       ├── EditGoodWork.razor
│       └── Admin/         # Admin tools
├── Data/                  # Services and data access
│   ├── Neo4jService.cs
│   ├── RecurringEventService.cs
│   ├── GeocodingService.cs
│   ├── GooglePlacesService.cs
│   └── TestDataService.cs
├── Models/                # Data models
│   ├── GoodWorksModel.cs
│   ├── ParticipantModel.cs
│   └── DoGooderModel.cs
├── Auth/                  # Authentication helpers
├── docs/                  # Documentation
└── wwwroot/              # Static assets
```

## 🎯 Roadmap

### Planned Features
- [ ] Calendar export (iCal/Google Calendar)
- [ ] Email notifications for upcoming events
- [ ] Mobile app (Blazor Hybrid)
- [ ] Advanced analytics dashboard
- [ ] Organization profiles
- [ ] Volunteer hour tracking and certificates
- [ ] Social sharing and invitations
- [ ] Event reviews and ratings
- [ ] Skills-based matching
- [ ] Team/group sign-ups
- [ ] Recurring event instance management
- [ ] In-app messaging between organizers and volunteers

### Performance & Scaling
- [ ] Redis caching layer
- [ ] CDN for static assets
- [ ] Implement pagination for large result sets
- [ ] Add database indices for common queries
- [ ] Background job processing for notifications

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Guidelines
1. Follow existing code style and patterns
2. Use meaningful commit messages
3. Update documentation for new features
4. Test thoroughly before submitting PR
5. Ensure Neo4j queries are optimized

### Areas for Contribution
- UI/UX improvements
- Performance optimizations
- New event categories and filters
- Documentation and examples
- Bug fixes and testing
- Accessibility enhancements

## 📄 License

This project is open source and available under the MIT License.

## 🙏 Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- UI powered by [MudBlazor](https://mudblazor.com/)
- Graph database by [Neo4j](https://neo4j.com/)
- Maps by [Google Maps Platform](https://developers.google.com/maps)
- Authentication by [Auth0](https://auth0.com/)

## 📞 Support

For questions, issues, or suggestions:
- Open an issue on [GitHub](https://github.com/kingjuk/CharityRabbit/issues)
- Visit [CharityRabbit.com](https://charityrabbit.com/)

---

**Made with ❤️ for the community** 🥕

## 📈 Monitoring & Analytics

### Recommended Tools to Add:
1. **Google Search Console**
   - Submit sitemap
   - Monitor indexing status
   - Track search performance

2. **Google Analytics 4**
   - Track page views
   - Monitor user behavior
   - Conversion tracking

3. **Bing Webmaster Tools**
   - Submit sitemap
   - Monitor Bing rankings

### Social Media Images
CharityRabbit uses optimized images for social media sharing:
- **Open Graph Image:** `/images/charityrabbit.png` (1200 x 630 px)
- **Twitter Card:** `/images/charityrabbit.png`
- All pages include proper `og:image` and `twitter:image` tags

See [docs/SOCIAL_MEDIA_IMAGES.md](docs/SOCIAL_MEDIA_IMAGES.md) for image optimization guidelines.

### Key Metrics to Track:
- Organic search traffic
- Click-through rate (CTR)
- Average position in search results
- Indexed pages count
- Core Web Vitals scores
- Social media sharing engagement- Social media sharing engagement
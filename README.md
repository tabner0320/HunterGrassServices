# Hunter Grass Services

### Professional Lawn Care Website & Quote Management API

A responsive lawn care business website built with **C#, .NET 10, ASP.NET Core Minimal API, HTML, CSS, JavaScript, Entity Framework Core, and SQLite**.

Hunter Grass Services allows customers to explore lawn care services and submit online quote requests. The application uses an ASP.NET Core backend to validate and process customer submissions and Entity Framework Core to store requests in a SQLite database.

This project demonstrates full-stack web development, REST API integration, database management, form validation, and Git/GitHub version control.

---

## Features

### Responsive Business Website
- Modern lawn care website with mobile-friendly styling
- Homepage with lawn care imagery and business information
- Service cards highlighting available lawn care services
- About section describing the business
- Contact and free quote request form

### Customer Quote Request System
- Customers can submit quote requests online
- JavaScript uses the Fetch API to send requests to the backend
- ASP.NET Core Minimal API processes submissions
- Server-side validation using Data Annotations
- Success and error messages displayed to customers
- Customer quote requests stored in SQLite

### Database Integration
- Entity Framework Core for database access
- SQLite for persistent data storage
- Code-first database migrations
- Automatically generated quote request IDs
- UTC timestamps for submitted requests

---

## Technologies Used

| Category | Technology |
|---|---|
| Programming Language | C# |
| Framework | .NET 10 |
| Backend | ASP.NET Core Minimal API |
| Frontend | HTML5, CSS3, JavaScript |
| API Communication | Fetch API |
| Database | SQLite |
| ORM | Entity Framework Core 10 |
| Data Format | JSON |
| Version Control | Git & GitHub |
| Development Environment | Visual Studio Code |

---

## Project Structure

```text
HunterGrassServices.Api/
├── Data/
│   └── AppDbContext.cs
├── Migrations/
│   └── Entity Framework Core migration files
├── Models/
│   └── QuoteRequest.cs
├── Properties/
│   └── launchSettings.json
├── Services/
├── wwwroot/
│   ├── index.html
│   ├── style.css
│   └── js/
│       └── app.js
├── .gitignore
├── appsettings.json
├── appsettings.Development.json
├── HunterGrassServices.Api.csproj
├── Program.cs
└── README.md
```

The local SQLite database file, `huntergrass.db`, is excluded from Git to protect stored customer information.

---

## Lawn Care Services

The website currently highlights:

- **Lawn Mowing** — Regular mowing to maintain a clean and healthy lawn.
- **Edging & Trimming** — Detailed trimming around sidewalks, driveways, and landscaping.
- **Mulching** — Mulch installation to improve landscaping appearance.
- **Seasonal Cleanup** — Seasonal yard cleanup and debris removal.

---

## API Endpoint

### Submit a Quote Request

**POST** `/api/quotes`

Accepts customer quote information in JSON format.

Example request:

```json
{
  "customerName": "John Test",
  "email": "john.test@example.com",
  "phone": "555-0100",
  "address": "123 Test Street",
  "service": "Lawn Mowing",
  "message": "I would like a quote for weekly lawn mowing."
}
```

### Successful Response

**HTTP 201 Created**

```json
{
  "id": 1,
  "message": "Quote request saved successfully."
}
```

The API validates the request, saves it to the SQLite database, and returns the generated request ID.

Customer records are not exposed through a public retrieval endpoint.

---

## How the Application Works

```text
Customer
   |
   v
HTML Quote Request Form
   |
   v
JavaScript Fetch API
   |
   v
POST /api/quotes
   |
   v
ASP.NET Core Minimal API
   |
   v
Server-Side Validation
   |
   v
Entity Framework Core
   |
   v
SQLite Database
```

The frontend and backend work together to process quote requests without requiring customers to interact directly with the database.

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio Code or another compatible editor
- Git
- Entity Framework Core CLI tools, version 10

### 1. Clone the Repository

```bash
git clone https://github.com/tabner0320/HunterGrassServices.git
cd HunterGrassServices
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Application

```bash
dotnet build
```

### 4. Install Entity Framework Core CLI Tools (If Needed)

```bash
dotnet tool install --global dotnet-ef --version 10.0.15
```

If already installed, update using `dotnet tool update --global dotnet-ef --version 10.0.15`.

### 5. Create the SQLite Database

```bash
dotnet ef database update
```

This applies the existing migrations and creates the database structure.

### 6. Run the Application

```bash
dotnet run
```

Open the local URL displayed in the terminal. In the current development configuration, the website runs at:

`http://localhost:5154`

The port may vary depending on the local environment.

---

## Database Model

The `QuoteRequest` model includes:

| Property | Description |
|---|---|
| Id | Unique request identifier |
| CustomerName | Customer's full name |
| Email | Customer's email address |
| Phone | Customer's phone number |
| Address | Requested service location |
| Service | Selected lawn care service |
| Message | Additional customer information |
| CreatedAt | UTC submission timestamp |

Entity Framework Core manages database persistence and schema migrations.

---

## Security Considerations

The project is currently a development and portfolio application, not a production-ready customer management system.

Before public deployment, additional safeguards are needed, including authentication and authorization for administrative access, anti-spam protection, rate limiting, input constraints, HTTPS enforcement, and appropriate privacy and data retention practices.

SQLite database files are excluded from Git using `.gitignore`. Real customer information should never be committed to the repository.

---

## Future Enhancements

- [ ] Secure administrator login
- [ ] Private dashboard for reviewing customer quote requests
- [ ] Quote status tracking (New, Contacted, Scheduled, Completed)
- [ ] Email notifications for new submissions
- [ ] Appointment scheduling
- [ ] Additional validation and spam protection
- [ ] Automated testing with xUnit
- [ ] Production deployment and hosting
- [ ] Website screenshots and project demonstrations

---

## Skills Demonstrated

- C# and .NET development
- ASP.NET Core Minimal APIs
- REST API development
- JavaScript Fetch API
- Asynchronous programming with `async` and `await`
- Entity Framework Core
- SQLite database integration
- Code-first migrations
- Server-side validation
- Responsive web design
- Git and GitHub version control

---

## Developer

**Theophilus M. Abner Jr.**

Software Developer | IT Professional | U.S. Army Veteran

- **GitHub:** https://github.com/tabner0320
- **Portfolio:** https://tabner0320.github.io/theo-abner-resume/

---

## Project Status

**Active Development**

The responsive website, quote submission API, and SQLite persistence are implemented. Additional security, administration, and deployment features are planned.
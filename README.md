# Zoomies

Zoomies is a full-stack web application for browsing, listing, and managing premium pre-owned cars. It includes user accounts, car listings, image uploads, wishlists, seller messaging, realtime chat notifications, and finance tools.

## Features

- Browse car listings with search, filters, sorting, and pagination.
- Switch between grid and list inventory views.
- View detailed car information with image galleries.
- Register, log in, and manage account phone number.
- Create, edit, and delete personal car listings.
- Upload multiple images for each listing.
- Save cars to a personal wishlist.
- Contact sellers about specific listings.
- Receive realtime message notifications with SignalR.
- Estimate monthly payments.
- Estimate a fair car price based on age, mileage, condition, and defects.
- Admin role support for managing listings.

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | HTML, CSS, JavaScript |
| Backend | ASP.NET Core Web API |
| Database | SQL Server LocalDB |
| ORM | Entity Framework Core |
| Authentication | JWT Bearer authentication |
| Password hashing | BCrypt |
| Realtime | SignalR |
| API documentation | Swagger/OpenAPI |
| Testing | MSTest, EF Core InMemory |

## Prerequisites

- .NET SDK compatible with `net10.0`
- SQL Server LocalDB
- Visual Studio, Visual Studio Code, or another editor
- A static file server for the frontend, such as VS Code Live Server

## Getting Started

Clone the repository:

```powershell
git clone <repository-url>
cd zoomies
```

Restore backend dependencies:

```powershell
dotnet restore backend\Zoomies\Zoomies.slnx
```

Apply database migrations:

```powershell
dotnet ef database update --project backend\Zoomies\Zoomies.csproj
```

If `dotnet ef` is not installed:

```powershell
dotnet tool install --global dotnet-ef
```

Start the backend:

```powershell
dotnet run --project backend\Zoomies\Zoomies.csproj --launch-profile https
```

The backend runs at:

- `https://localhost:7031`
- `http://localhost:5062`

Swagger is available at:

```text
https://localhost:7031/swagger
```

Start the frontend by serving the `frontend` folder. With VS Code Live Server, open:

```text
http://localhost:5501/frontend/index.html
```

## Configuration

The frontend API URL is configured in `frontend/app.js`:

```javascript
const API_BASE_URL = "https://localhost:7031/api";
```

The backend connection string and JWT secret are configured in `backend/Zoomies/appsettings.json`.

For production, move secrets out of `appsettings.json` and restrict CORS to trusted origins.

## Demo Accounts

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@zoomies.com` | `Admin123!` |
| Seller/User | `seller@zoomies.com` | `Admin123!` |

## Running Tests

Run the backend test suite:

```powershell
dotnet test backend\Zoomies\Zoomies.slnx
```

## API Endpoints

| Method | Endpoint | Meaning |
| --- | --- | --- |
| `POST` | `/api/Auth/register` | Creates a new user account. |
| `POST` | `/api/Auth/login` | Authenticates a user and returns a JWT access token. |
| `PUT` | `/api/Auth/phone` | Updates the logged-in user's phone number. |
| `POST` | `/api/Auth/refresh-token` | Issues a new access token using the refresh token cookie. |
| `GET` | `/api/Cars` | Returns paginated car listings with optional filters and sorting. |
| `GET` | `/api/Cars/{id}` | Returns details for one car listing. |
| `GET` | `/api/Cars/mine` | Returns listings created by the logged-in user. |
| `POST` | `/api/Cars` | Creates a new car listing with form data and uploaded images. |
| `PUT` | `/api/Cars/{id}` | Updates an existing listing owned by the user or manageable by an admin. |
| `DELETE` | `/api/Cars/{id}` | Deletes a listing owned by the user or manageable by an admin. |
| `GET` | `/api/Wishlist` | Returns the logged-in user's saved cars. |
| `POST` | `/api/Wishlist/{carId}` | Adds a car to the logged-in user's wishlist. |
| `DELETE` | `/api/Wishlist/{carId}` | Removes a car from the logged-in user's wishlist. |
| `POST` | `/api/ContactMessages` | Sends a message about a car listing to the seller or selected recipient. |
| `GET` | `/api/ContactMessages/{id}` | Returns one message if the logged-in user has access to it. |
| `GET` | `/api/ContactMessages/inbox` | Returns messages received by the logged-in user. |
| `GET` | `/api/ContactMessages/sent` | Returns messages sent by the logged-in user. |
| `PUT` | `/api/ContactMessages/{id}/read` | Marks a received message as read. |
| `DELETE` | `/api/ContactMessages/{id}` | Deletes a message the logged-in user can access. |
| `GET` | `/api/ContactMessages/thread/{carId}/{otherUserId}` | Returns the conversation for a specific car and user pair. |
| `GET` | `/api/ContactMessages/conversations` | Returns summarized active conversations for the logged-in user. |
| `POST` | `/api/PriceEstimates` | Calculates a rule-based fair price estimate for a vehicle. |
| `GET` | `/chatHub` | SignalR hub endpoint used for realtime message notifications. |

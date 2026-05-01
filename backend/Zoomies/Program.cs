using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using Zoomies.Data;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. SERVICES CONFIGURATION (The "Ingredients" for your app)
// ============================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- SWAGGER SETUP ---
// This configures the Swagger UI to show the "Authorize" padlock icon
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    // This makes sure the "Authorize" button actually sends the token to your API
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// --- AUTHENTICATION SETUP ---
// This tells the app how to read and validate the "wristband" (JWT Token)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            // Uses the Secret Key from appsettings.json to verify the token hasn't been tampered with
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// --- DATABASE SETUP ---
// Connects your code to SQL Server using the connection string in appsettings.json
builder.Services.AddDbContext<ZoomiesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

// --- CORS POLICY ---
// This allows your Frontend (like a website or mobile app) to talk to this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Allow requests from any website
              .AllowAnyMethod()   // Allow GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader();  // Allow any headers (like Authorization)
    });
});

var app = builder.Build();

// ============================================================
// 2. REQUEST PIPELINE (The "Traffic Rules" for every request)
// ============================================================

// If the app is in development mode, show the Swagger documentation pages
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection(); // Forces the use of HTTPS (secure connection)
app.UseExceptionHandler("/error"); // Handles crashes gracefully
app.UseCors("AllowAll"); // Applies the CORS rules defined above

// --- IMPORTANT: THE ORDER BELOW MATTERS! ---

// 1st: Identify WHO the user is (checks the token/wristband)
app.UseAuthentication();

// 2nd: Identify WHAT the user is allowed to do (checks Role/Ownership)
app.UseAuthorization();

// 3rd: Direct the request to the correct Controller (e.g., CarsController)
app.MapControllers();

app.Run();
using Luxury.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Cấu hình Database ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LuxuryHotelContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Chỉ định rõ nguồn Vue
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Cho phép gửi kèm Cookie/Auth header
    });
});

// --- Cấu hình Authentication (JWT) ---
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
})
.AddFacebook(options => {
    options.AppId = "1419032062964737";
    options.AppSecret = "6b815c2b1bc5a6590a0d5e6ac1f095b0";

    // Yêu cầu lấy thêm quyền Email
    options.Scope.Add("email");

    // Ép Facebook trả về các trường dữ liệu cụ thể
    options.Fields.Add("name");
    options.Fields.Add("email");

    options.SaveTokens = true;
});

// --- Cấu hình Authorization (Phân quyền) ---
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffPolicy", policy => policy.RequireRole("Admin", "NhanVien"));
    options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("KhachHang"));
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting(); // Định tuyến phải chạy trước

app.UseCors("AllowVueApp");

app.UseAuthentication(); // Xác thực (Bạn là ai?)
app.UseAuthorization();  // Phân quyền (Bạn được làm gì?)

app.MapControllers();

app.Run();
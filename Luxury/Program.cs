using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Luxury.Models;
using Luxury.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<EmailService>();
builder.Services.AddMemoryCache();

// --- 1. Cấu hình Controllers & JSON (Sửa lỗi camelCase cho Voucher/Payment) ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Đảm bảo Frontend Vue nhận được maCode thay vì MaCode
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Chống lỗi vòng lặp khi truy vấn dữ liệu quan hệ (Voucher <-> KhachHang)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. Cấu hình Database ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LuxuryHotelContext>(options =>
    options.UseSqlServer(connectionString));

// --- 3. Cấu hình CORS (Cho phép Vue App truy cập API) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Cổng mặc định của Vite/Vue
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc để gửi AuthToken qua Cookie
    });
});

// --- 4. Cấu hình Authentication (JWT + Cookie + Facebook) ---
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

    // QUAN TRỌNG: Tự động lấy Token từ Cookie "AuthToken" để xác thực người dùng
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddFacebook(options => {
    options.AppId = "1419032062964737";
    options.AppSecret = "6b815c2b1bc5a6590a0d5e6ac1f095b0";
    options.Scope.Add("email");
    options.Fields.Add("name");
    options.Fields.Add("email");
    options.SaveTokens = true;
});

// --- 5. Cấu hình Phân quyền (Authorization) ---
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffPolicy", policy => policy.RequireRole("Admin", "NhanVien"));
    options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("KhachHang"));
});

var app = builder.Build();

// --- 6. Cấu hình HTTP Pipeline (Middleware) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// SỬA LỖI ĐỔI ẢNH: Đảm bảo thư mục lưu ảnh tồn tại để tránh lỗi khởi động
var userImgPath = Path.Combine(builder.Environment.ContentRootPath, "src", "DuanKhachSan", "KhachHang", "IMG", "USER");
if (!Directory.Exists(userImgPath))
{
    Directory.CreateDirectory(userImgPath);
}

app.UseStaticFiles(); // Cho thư mục wwwroot mặc định

// Cấu hình đường dẫn ảo cho ảnh đại diện khách hàng
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(userImgPath),
    RequestPath = "/USER_IMG"
});

app.UseHttpsRedirection();
app.UseRouting();

// Thứ tự này cực kỳ quan trọng để Auth hoạt động với Vue
app.UseCors("AllowVueApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
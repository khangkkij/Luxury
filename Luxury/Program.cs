
using Luxury.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

//Bộ phận tìm kiếm các đường dẫn API trong code
builder.Services.AddEndpointsApiExplorer();
// Bộ phận tạo giao diện đồ họa cho Swagger UI
builder.Services.AddSwaggerGen();

//Chuoi ket noi CSDL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Sửa dòng 21 thành:
builder.Services.AddDbContext<LuxuryHotelContext>(options =>
    options.UseSqlServer(connectionString));

// Cấu hình CORS: Cho phép các ứng dụng bên ngoài (như React, Angular, Mobile) truy cập API này
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});




// Cấu hình CORS để cho phép Vue.js truy cập API với Cookie
// Trong Program.cs
// XÓA ĐOẠN CORS CŨ (Dòng 24-27) VÀ THAY BẰNG ĐOẠN NÀY:
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins("http://localhost:5173") // Thay bằng URL chính xác của trang Vue (thường là port 5173)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Cho phép gửi nhận Cookie
    });
});
// CẤU HÌNH AUTHENTICATION & AUTHORIZATION (BẢO MẬT)

// Cấu hình Xác thực bằng JWT (Authentication)
builder.Services.AddAuthentication(options => {
    // Thiết lập mặc định sử dụng JWT Bearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    // Các quy tắc kiểm tra tính hợp lệ của Token từ Client gửi lên
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,          // Kiểm tra bên phát hành token
        ValidateAudience = true,        // Kiểm tra đối tượng sử dụng token
        ValidateLifetime = true,        // Kiểm tra thời hạn của token
        ValidateIssuerSigningKey = true, // Kiểm tra chữ ký bảo mật
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization(options => {
    // 1. Chính sách cho Admin: Chỉ những ai có Role "Admin" mới được vào
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));

    // 2. Chính sách cho Nhân viên: Admin và NhanVien đều có thể vào (tùy bạn thiết lập)
    options.AddPolicy("StaffPolicy", policy => policy.RequireRole("Admin", "NhanVien"));

    // 3. Chính sách cho Khách hàng: Người dùng đã đăng ký
    options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("KhachHang"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors("AllowVue");

app.Run();

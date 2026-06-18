# 🎓 DATN - Backend Hệ Thống Thi Trắc Nghiệm Trực Tuyến

![DATN Backend Banner](./images/datn-backend-banner.png)

## 📋 Giới Thiệu

**DATN** (Đồ Án Tốt Nghiệp) là backend của hệ thống **QuizCast** - một nền tảng thi trắc nghiệm trực tuyến hiện đại. Repository này chứa toàn bộ logic xử lý backend, quản lý dữ liệu và các dịch vụ phức tạp.

---

## 🛠️ Công Nghệ Chính

### Ngôn Ngữ Lập Trình
- **C#** (100%) - Ngôn ngữ chính cho backend
- **.NET Framework** hoặc **.NET Core** - Nền tảng phát triển

### Kiến Trúc & Mẫu Thiết Kế

#### 1. 📊 Database - SQL Server
```
Mô tả:
- Mô hình dữ liệu phức tạp với nhiều bảng liên kết
- Quản lý Identity và xác thực người dùng
- Tracking 7 loại hành vi người dùng
- Đảm bảo tính toàn vẹn dữ liệu
- Hỗ trợ transaction và stored procedures
```

#### 2. 🔌 REST API
```
Kiến Trúc:
- Repository & Service Pattern
- 3 t���ng kiến trúc rõ ràng:
  * Controllers (API Endpoints)
  * Services (Business Logic)
  * Repositories (Data Access)
- Dependency Injection (DI Container)
- Data Transfer Objects (DTOs)
```

#### 3. 🎥 Video Streaming
```
Tính Năng:
- Real-time stream qua WebSocket
- FFmpeg Integration để xử lý video
- Tạo file HLS (HTTP Live Streaming)
- Quản lý đa luồng đồng thời
- Tối ưu hóa bandwidth
```

#### 4. 🔐 Authentication & Authorization
```
Bảo Mật:
- JWT (JSON Web Token) Authentication
- Role-Based Access Control (RBAC)
- Quản lý quyền người dùng chi tiết
- Tự động khóa tài khoản sau 10 lần nhập sai mật khẩu
- Hỗ trợ refresh token
```

#### 5. 📧 Email Notification
```
Tính Năng:
- Gửi email bất đồng bộ (Asynchronous)
- Thông báo kết quả thi
- Xác nhận đăng ký tài khoản
- Cài lại mật khẩu
- Queue-based email processing
```

#### 6. 🔄 Data Mapping
```
Công Cụ:
- AutoMapper
- Bidirectional Mapping
- Nested Object Mapping
- Tự động chuyển đổi DTO ↔ Entity
```

---

## ✨ Tính Năng Chính

| Tính Năng | Mô Tả |
|-----------|-------|
| 👤 **Quản Lý Người Dùng** | Đăng ký, đăng nhập, quản lý hồ sơ |
| 📝 **Quản Lý Bài Thi** | CRUD bài thi, câu hỏi, đáp án |
| ⏱️ **Quản Lý Thời Gian** | Thiết lập và kiểm soát thời gian làm bài |
| 📊 **Chấm Điểm Tự Động** | Tính toán kết quả và thống kê |
| 🎥 **Streaming Video** | Phát video trong bài thi |
| 🔐 **Xác Thực & Phân Quyền** | JWT + RBAC |
| 📧 **Thông Báo Email** | Gửi email async |
| 📈 **Tracking Hành Vi** | Ghi lại 7 loại hành vi người dùng |
| 🔍 **Logging & Monitoring** | Ghi nhật ký và giám sát hệ thống |

---

## 🏗️ Cấu Trúc Dự Án

```
DATN/
├── DATN.API/                    # API Controllers
│   ├── Controllers/
│   ├── Middleware/
│   └── appsettings.json
├── DATN.Core/                   # Business Logic
│   ├── Services/
│   ├── Interfaces/
│   └── Models/
├── DATN.Data/                   # Data Access
│   ├── Repositories/
│   ├── Context/
│   ├── Entities/
│   └── Migrations/
├── DATN.Common/                 # Utilities & Helpers
│   ├── Constants/
│   ├── Enums/
│   ├── Exceptions/
│   └── Extensions/
├── DATN.Tests/                  # Unit & Integration Tests
├── appsettings.json             # Cấu hình chung
└── DATN.sln                      # Solution file
```

---

## 🚀 Cài Đặt & Chạy

### Yêu Cầu
- **.NET 6.0+** hoặc **.NET Framework 4.7.2+**
- **SQL Server 2019+**
- **Visual Studio 2019+** hoặc **Visual Studio Code**
- **Git**

### Hướng Dẫn

#### 1. Clone Repository
```bash
git clone https://github.com/16November/DATN.git
cd DATN
```

#### 2. Cấu Hình Database
```bash
# Chỉnh sửa connection string trong appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DATN;User Id=sa;Password=YourPassword;"
  }
}
```

#### 3. Chạy Migration
```bash
# Mở Package Manager Console
Update-Database
```

#### 4. Khôi Phục Dependencies
```bash
dotnet restore
```

#### 5. Chạy Ứng Dụng
```bash
dotnet run --project DATN.API
```

API sẽ chạy tại: `http://localhost:5000` hoặc `https://localhost:5001`

#### 6. Swagger Documentation
Truy cập: `http://localhost:5000/swagger`

---

## 📡 API Endpoints (Ví Dụ)

### Authentication
```
POST   /api/auth/register          - Đăng ký tài khoản
POST   /api/auth/login             - Đăng nhập
POST   /api/auth/refresh-token     - Làm mới token
POST   /api/auth/logout            - Đăng xuất
```

### Quizzes (Bài Thi)
```
GET    /api/quizzes                - Lấy danh sách bài thi
GET    /api/quizzes/{id}           - Lấy chi tiết bài thi
POST   /api/quizzes                - Tạo bài thi mới
PUT    /api/quizzes/{id}           - Cập nhật bài thi
DELETE /api/quizzes/{id}           - Xóa bài thi
```

### Questions (Câu Hỏi)
```
GET    /api/questions              - Lấy danh sách câu hỏi
GET    /api/questions/{id}         - Lấy chi tiết câu hỏi
POST   /api/questions              - Tạo câu hỏi mới
PUT    /api/questions/{id}         - Cập nhật câu hỏi
DELETE /api/questions/{id}         - Xóa câu hỏi
```

### Results (Kết Quả)
```
GET    /api/results                - Lấy kết quả thi
GET    /api/results/{id}           - Lấy chi tiết kết quả
POST   /api/results                - Nộp bài thi
```

### Users (Người Dùng)
```
GET    /api/users/{id}             - Lấy thông tin người dùng
PUT    /api/users/{id}             - Cập nhật hồ sơ
GET    /api/users/{id}/history     - Lịch sử làm bài
```

---

## 🔧 Cấu Hình Chính

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "ExpiresInMinutes": 60,
    "RefreshTokenExpiresInDays": 7
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-password"
  },
  "VideoProcessing": {
    "FFmpegPath": "ffmpeg",
    "HLSOutputPath": "./wwwroot/videos"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## 🧪 Testing

### Chạy Unit Tests
```bash
dotnet test DATN.Tests
```

### Coverage Report
```bash
dotnet test DATN.Tests /p:CollectCoverage=true
```

---

## 📚 Dependencies Chính

- **Entity Framework Core** - ORM cho database
- **JWT Bearer** - Authentication
- **AutoMapper** - Object mapping
- **Serilog** - Logging
- **FluentValidation** - Validation
- **MediatR** - CQRS Pattern (tùy chọn)
- **xUnit** - Testing framework

---

## 🔒 Bảo Mật

✅ **Các biện pháp bảo mật được áp dụng:**
- JWT Authentication
- HTTPS/TLS Encryption
- CORS Configuration
- SQL Injection Prevention (Parameterized Queries)
- XSS Protection
- CSRF Token
- Password Hashing (bcrypt)
- Rate Limiting
- Input Validation & Sanitization

---

## 📖 Documentation

Chi tiết về API endpoints, models, và services có thể tìm thấy trong:
- **Swagger UI**: `http://localhost:5000/swagger`
- **Code Documentation**: XML comments trong source code
- **Database Diagram**: `/docs/database-diagram.md`

---

## 🐛 Debug & Troubleshooting

### Lỗi Connection String
```bash
# Kiểm tra SQL Server đang chạy
sqlcmd -S localhost -U sa -P YourPassword
```

### Lỗi Migration
```bash
# Reset database
Update-Database -Migration 0
Update-Database
```

### Lỗi Port
```bash
# Nếu port 5000 đang sử dụng, chỉnh sửa launchSettings.json
{
  "applicationUrl": "https://localhost:5001;http://localhost:5000"
}
```

---

## 👥 Đóng Góp

Nếu bạn muốn đóng góp vào dự án:
1. Fork repository
2. Tạo branch mới (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Mở Pull Request

---

## 📧 Liên Hệ & Hỗ Trợ

Nếu gặp vấn đề hoặc có câu hỏi:
- 📝 Tạo Issue trên GitHub
- 💬 Thảo luận trong Discussions
- ✉️ Liên hệ tác giả

---

## 📄 License

*[Chỉ định license của bạn nếu có]*

---

## 👨‍💻 Tác Giả

- **Người phát triển**: 16November
- **Loại dự án**: Đồ Án Tốt Nghiệp (DATN)
- **Lĩnh vực**: Backend Development - .NET/C#

---

**⭐ Nếu dự án này hữu ích với bạn, vui lòng star repository!**

---

*Phát triển với ❤️ cho đồ án tốt nghiệp*

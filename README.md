# CorpProcure

**Enterprise E-Procurement System** - A comprehensive procurement management solution built with ASP.NET Core MVC.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=flat&logo=bootstrap)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat&logo=microsoftsqlserver)
![License](https://img.shields.io/badge/License-MIT-green.svg)

## 📋 Overview

CorpProcure is a full-featured e-procurement system designed for enterprises to streamline their purchasing workflow. It supports the complete procurement lifecycle from purchase request creation to purchase order generation with a 2-level approval workflow.

## ✨ Features

### Core Functionality
- **Purchase Request Management** - Create, edit, and track purchase requests
- **2-Level Approval Workflow** - Manager → Finance approval chain
- **Purchase Order Generation** - Auto-generate POs from approved requests
- **Vendor Management** - Manage vendor profiles and item catalogs
- **Budget Management** - Department budget allocation and tracking
- **Item Catalog** - Centralized item/product management

### Advanced Features
- **📊 Dashboard Analytics** - Statistics, charts, and KPIs
- **📤 Export to Excel** - Export reports, audit logs, and master data
- **📥 Import from Excel** - Bulk import vendors, items, and departments
- **📄 Configurable PO Template** - Customize PDF with logo, colors, and company info
- **🔐 Role-Based Access Control** - Admin, Finance, Manager, Procurement, Staff
- **📝 Complete Audit Trail** - Track all system changes

### Roles & Permissions
| Role | Capabilities |
|------|-------------|
| **Admin** | Full system access, user management, settings |
| **Finance** | Approve PRs, manage budgets, view all reports |
| **Manager** | Approve department PRs, view department data |
| **Procurement** | Manage vendors, items, generate POs |
| **Staff** | Create and view own purchase requests |

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap 5, ApexCharts, Remix Icons
- **PDF Generation**: QuestPDF
- **Excel Handling**: ClosedXML
- **QR Code**: QRCoder

## 📦 Installation

### Prerequisites
- .NET 8.0 SDK
- SQL Server 2019+
- Node.js (optional, for frontend tooling)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/CorpProcure.git
   cd CorpProcure
   ```

2. **Configure database connection**
   ```bash
   # Copy example environment file
   cp .env.example .env
   
   # Edit appsettings.json with your connection string
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   ```
   https://localhost:5001
   ```

### Default Admin Account
```
Email: admin@corpprocure.com
Password: Admin123!
```

## 🐳 Docker Deployment

```bash
# Development
docker-compose up -d

# Production
docker-compose -f docker-compose.prod.yml up -d
```

## 📁 Project Structure

```
CorpProcure/
├── Controllers/        # MVC Controllers + API Controllers
├── Models/            # Entity models and enums
├── DTOs/              # Data Transfer Objects
├── Services/          # Business logic layer
├── Data/              # DbContext and configurations
├── Views/             # Razor views
├── Authorization/     # Custom authorization handlers
├── Configuration/     # DI and service registration
└── wwwroot/          # Static assets (CSS, JS, images)
```

## 🔧 Configuration

### Environment Variables
| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Database connection string |
| `AppSettings__BaseUrl` | Application base URL |
| `Email__SmtpHost` | SMTP server for notifications |

### System Settings
Accessible via Admin → System Settings:
- Auto-approval thresholds
- Email notification settings
- PO template customization
- Company information

## 📊 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/vendors` | GET | List vendors |
| `/api/vendoritems/{vendorId}` | GET | Get vendor items |
| `/api/items/search` | GET | Search items catalog |

## � Try the Workflow

### Demo Accounts
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@corpprocure.com | Admin123! |
| Finance | finance@corpprocure.com | Finance123! |
| Manager | manager@corpprocure.com | Manager123! |
| Staff | staff@corpprocure.com | Staff123! |

### Complete Procurement Workflow

**Step 1: Create Purchase Request (as Staff)**
1. Login as Staff
2. Go to **Purchase Requests** → **Create New**
3. Fill in request details, add items from catalog
4. Click **Submit for Approval**

**Step 2: Manager Approval**
1. Login as Manager
2. Go to **Approvals** → **Pending**
3. Review the request details and items
4. Click **Approve** or **Reject with reason**

**Step 3: Finance Approval**
1. Login as Finance
2. Go to **Approvals** → **Pending Finance**
3. Verify budget availability
4. Click **Approve** or **Reject**

**Step 4: Generate Purchase Order (as Admin/Procurement)**
1. Login as Admin or Procurement
2. Go to **Purchase Orders** → **Generate**
3. Select approved request
4. Review PO details and click **Generate**
5. Download PDF with QR code for verification

### Additional Features to Try
- **Dashboard**: View analytics and charts
- **Export**: Export data to Excel from Reports menu
- **Import**: Bulk import data from Import Data menu
- **Settings**: Customize PO template with company logo

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 Contact

**Roihan Sori** - Developer

---

Made with ❤️ using ASP.NET Core

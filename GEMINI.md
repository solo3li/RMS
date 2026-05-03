# RMS - Restaurant Management System (Enterprise Edition)

## Project Overview
RMS is a high-performance, multi-branch restaurant management system built with ASP.NET Core 10. It features strict data isolation, granular permission-based security, and real-time operational synchronization.

## Architecture & Frameworks
- **Framework:** ASP.NET Core 10 MVC
- **Database:** Entity Framework Core (SQLite)
- **Real-time:** SignalR (OrderHub)
- **Security:** Claims-based Permission Policy System
- **Testing:** XUnit + Moq

## Key Mandates
### 1. Branch Binding (Data Isolation)
All operational data is strictly isolated by branch. Staff members (Kitchen, Delivery, Call Center) are assigned to specific branches and can **only** access data related to those branches. This is enforced via `IBranchService` and filtered at the repository/query level.

### 2. Granular Permissions
Authorization is handled via specific permission claims (e.g., `Permissions.Orders.Create`). Roles (Admin, Kitchen, etc.) are collections of these permissions. Do not use hardcoded role checks in new controllers; use policies based on the `Permissions` constants.

### 3. Auditing
Every data mutation in `ApplicationDbContext` is automatically audited. The system captures "Before" and "After" snapshots for full forensic transparency.

### 4. POS Readiness
The system includes a specialized 80mm thermal receipt view (`Orders/Receipt`) styled for professional POS printers.

## Development Workflows
### Seeding & Security
- Default Admin: `admin@rms.com` / `Admin@123`
- To update permissions, use the **Roles Management** interface or modify `DbInitializer.cs`.

### Real-time Notifications
- Use the `OrderHub` to broadcast status updates.
- Clients listen for `OrderStatusUpdated`, `NewOrderReceived`, and `StockStatusChanged`.

### Testing
- Run tests from the `RMS.Tests` directory: `dotnet test`
- Always add scenario tests for new business logic, especially regarding branch isolation and permission checks.

## Standard Conventions
- **Controllers:** Must be decorated with `[Authorize]` and use specific policies for actions.
- **Nullability:** The project uses C# 10 nullable reference types. Ensure all ViewModels and Services are warning-free.
- **UI:** Follow the `rms-theme.css` and use FontAwesome 6 for icons.

# dotnet.calcifer 🚀

[![NuGet version](https://badge.fury.io/nu/Calcifer.Microservice.Api.Template.svg)](https://www.nuget.org/packages/Calcifer.Microservice.Api.Template)

A professional-grade, production-ready **.NET 8 Web API Microservice Template** distributed via NuGet. Built with Clean Architecture principles, pre-configured with JWT Authentication, ASP.NET Core Identity, and Role-Based Authorization.

## 🌟 Features

- **.NET 8 Web API:** Utilizing the latest framework features and C# 12.
- **Hybrid Routing:** Seamlessly mixes MVC Controllers (for Authentication) and Minimal APIs (for domain logic).
- **Robust Security:** Pre-configured JWT Bearer Authentication and HMAC-SHA256 signing.
- **ASP.NET Core Identity:** Extended `ApplicationUser` and `ApplicationRole` models for fine-grained control.
- **Role Management:** Built-in policies (`AdminPolicy`, `ManagerPolicy`, `OfficerPolicy`) and endpoints for role assignment.
- **Entity Framework Core:** Pre-configured for SQL Server with automatic migration and database seeding on startup.
- **Audit Trails:** Base classes for entity tracking (`CreatedAt`, `UpdatedAt`, `DeletedBy`, etc.) and soft-delete functionality.
- **Swagger/OpenAPI:** Ready out of the box for easy API testing and payload inspection.

## 🛠️ Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB or standard instance)

## 📦 Installation

Install the template globally on your machine via the .NET CLI:

```bash
dotnet new install Calcifer.Microservice.Api.Template
```

```bash
dotnet new dotnet.calcifer -n MyProjectName
```

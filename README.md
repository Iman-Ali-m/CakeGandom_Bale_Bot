# CakeGandom Bot

A feature-rich Telegram-style bot for the **Bale** messenger, built to manage a cake shop.
The bot offers a dynamic four-level menu, user registration, role-based access control, and full CRUD operations for menu items and cakes. All content is stored in a **SQL Server** database and managed through **Entity Framework Core**.

Built with **.NET 10**, **ASP.NET Core**, **Entity Framework Core 10**, and **SQL Server**.

---

## Features

- **Dynamic multi-level menu** (up to 4 levels) fully stored in the database
- **User registration** with encrypted phone number storage (AES-256)
- **Role-based access** for the main user and additional admins
- **CRUD operations** for menu buttons and cake items (add, edit, delete, move)
- **Media management** – attach multiple photos with captions to each cake
- **Reply keyboard** for the main menu, **inline keyboards** for submenus
- **Polling** and **webhook** modes (switchable via configuration)
- **Admin REST API** protected by an `X-Api-Key` header
- **Automated tests** using xUnit with SQLite in-memory database

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 10 / ASP.NET Core |
| ORM | Entity Framework Core 10 |
| Main database | **Microsoft SQL Server** |
| Test database | SQLite in-memory (tests only) |
| Bot API | Bale Bot HTTP API |
| Testing | xUnit, FluentAssertions |
| Hosting | ASP.NET Core Web API (`BackgroundService` for polling) |

> **Note:** SQL Server is used for the real application. SQLite is used **only inside the test project** to keep tests fast and independent from any external SQL Server instance.

---

## Project Structure

# 🚜 Agri-Energy Connect Platform

Welcome to the **Agri-Energy Connect Platform** – a modern prototype for connecting South African farmers and energy providers. This platform demonstrates a scalable, secure, and modular solution to foster collaboration and innovation in food and green energy.

---

## 🗂️ Table of Contents

1. [🌍 Overview](#overview)
2. [✨ Key Features](#key-features)
3. [🏛️ Architecture](#architecture)
4. [🗃️ Database & Scripts](#database--scripts)
5. [🔑 Account Credentials (Demo)](#account-credentials-demo)
6. [🎥 Demo Video](#demo-video)
7. [⚡ Quick Start Guide](#quick-start-guide)
    - [Clone the Repo](#clone-the-repo)
    - [Run in Visual Studio](#run-in-visual-studio)
    - [Run via Command Line](#run-via-command-line)
8. [💡 Part 2 Feedback Implementation](#part-2-feedback-implementation)
9. [👥 Contributors](#contributors)

---

## 🌍 Overview

**Agri-Energy Connect** enables:
- **Farmers**: Manage product listings and profile.
- **Employees**: Add farmers, manage produce, and view/filter product lists.
- **Energy Providers/Stakeholders**: Engage with sustainable agriculture and energy markets.

> This platform creates a marketplace for **food and energy sustainability** in South Africa.

---

## ✨ Key Features

| Feature                    | Description                                                                 |
|----------------------------|-----------------------------------------------------------------------------|
| 🔐 Authentication          | Employees and farmers **must log in** to access the platform.               |
| 👩‍🌾 Farmer Management      | Employees add and manage farmers and their produce.                         |
| 🧺 Product Management      | Farmers add/manage products; employees view/filter product lists.           |
| 📅 Filtering               | Products can be filtered by **date range** and **product type**.            |
| 🏷️ Categories              | Products are linked to categories for easy browsing/filtering.              |
| 🧾 Preloaded Data           | Database seeded with demo users, products, and categories.                  |
| 📦 Modern UI                | Clean, intuitive frontend with responsive design.                           |

---

## 🏛️ Architecture

- **Microservices-based** (.NET 8):
  - **AgriEnergyConnect.API**: ASP.NET Core Web API (Business logic, DB, JWT Auth)
  - **AgriEnergyConnect.MVC**: ASP.NET Core MVC (User Interface)
  - **AgriEnergyConnect.Shared**: Shared models/contracts (DTOs, validation, logic)

- **Layered Pattern**:
  - MVC Controllers ↔️ Services ↔️ Data Access
  - Razor Views for clean separation of presentation

**Layout:**
```
/AgriEnergyConnect.API      # Backend API (Runs DB/auth/business logic)
/AgriEnergyConnect.MVC      # Frontend MVC app (User interface)
/AgriEnergyConnect.Shared   # Shared models & contracts (DTOs, validation)
```

---

## 🗃️ Database & Scripts

- **SQLite** powers all persistent data storage for easy setup (no external DB required).
- The schema and seed data are **automatically created at first run**.
- _No manual script execution needed!_
- Includes preloaded:
  - Employees & Farmers (demo users)
  - Product categories
  - Products

**To reset or inspect the DB:**
- The SQLite file is located at:  
  `/AgriEnergyConnect.API/App_Data/AgriEnergyConnect.db`
- Use any SQLite browser to view data or reset as needed.

---

## 🔑 Account Credentials (Demo)

> Use these demo credentials to log into the platform as either an employee or a farmer.

### Agri-Energy Connect Employee Credentials

- **Email:** `employee@agrienergy.com`
- **Password:** `Password123!`

### Farmer User Credentials

1. **Farmer 1**
   - **Email:** `johndoe@mail.com`
   - **Password:** `Johndoe123$`

2. **Farmer 2**
   - **Email:** `thabo@mail.com`
   - **Password:** `Thabo@2025!`

---

## 🎥 Demo Video

Watch a complete walkthrough of the Agri-Energy Connect Platform below:

[Click here to open the demo video directly in your browser.](https://advtechonline-my.sharepoint.com/:v:/g/personal/st10361554_vcconnect_edu_za/EQzm7M7ds6NMqz5lONIMl6wBvHQ-wZHm0NWaDLdp9yRUNg?nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJPbmVEcml2ZUZvckJ1c2luZXNzIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXciLCJyZWZlcnJhbFZpZXciOiJNeUZpbGVzTGlua0NvcHkifX0&e=jpFeSp)

---

## ⚡ Quick Start Guide

**Get started in minutes, either with Visual Studio or the .NET CLI.**

### 1️⃣ Clone the Repo

#### Option 1: Visual Studio

1. In Visual Studio, select **"Clone a repository"**.
2. Enter the repo URL:
   ```
   https://github.com/SashveerRamjathan/ST10361554-PROG7311-POE-Part-3.git
   ```
3. Click **Clone** and open the solution file.

#### Option 2: Command Line

```bash
git clone https://github.com/SashveerRamjathan/ST10361554-PROG7311-POE-Part-3.git
cd ST10361554-PROG7311-POE-Part-3
```

---

### 2️⃣ Running the Solution

#### Option 1: Visual Studio (Recommended)

- **Multi-Project Start**:  
  1. In the toolbar next to the Start (▶️) button, select the profile:  
     **`WebApp+API`**
  2. Click the green **Start** button (▶️)  
     > Both the API and the Web App will launch automatically.

  _You can also run each project individually:_
  - Right-click `AgriEnergyConnect.API` → **Set as Startup Project** → **Start**  
  - Then, right-click `AgriEnergyConnect.MVC` → **Set as Startup Project** → **Start**

#### Option 2: Command Line (.NET CLI)

```bash
# Start the API
cd AgriEnergyConnect.API
dotnet run

# In a new terminal, start the MVC Web App
cd ../AgriEnergyConnect.MVC
dotnet run
```
- Once both are running, access the web app at:  
  ```
  http://localhost:xxxx/
  ```
  (Replace `xxxx` with the port Visual Studio or CLI assigns.)

---

### 3️⃣ Configuration Notes

- No need to edit connection strings; defaults are set for local development.
- The API will **auto-create and seed** the SQLite database if missing.
- The MVC app's `appsettings.json` should have the correct `BaseUrl` for the API (defaults to localhost).

---

## 💡 Part 2 Feedback Implementation

### Feedback Received
> Readme needs more information on running your application.  
> Add in the scripts for your DB and speak about it in your readme.  
> Clear instructions are needed here.

### Improvements Made

- **Expanded Quick Start:** Added comprehensive setup instructions for both Visual Studio and command line, including the new "WebApp+API" combined startup profile for easy multi-project launch.
- **Database Details:** Clarified SQLite usage, auto-seeding behavior, and database file location—no manual script execution required.
- **Cloning Instructions:** Provided clear, step-by-step guidance for cloning the repository via Visual Studio and the command line.
- **More Engaging Structure:** Incorporated tables, emojis, and organized sections for a more user-friendly and visually appealing README.
- **Unified Startup Profile:** Configured a new startup profile to launch all three projects (Web App, API, Shared Library) simultaneously with a single action in Visual Studio.
- **Website Improvements:** Updated the site icon (favicon) and introduced a dedicated About page for platform and project context.

---

## 👥 Contributors

| Name                | Student ID   | Group |
|---------------------|--------------|-------|
| Sashveer Ramjathan  | ST10361554   | 2     |

---

<p align="center">
  <sub>
    Built with ❤ by Sashveer Lakhan Ramjathan.
  </sub>
</p>

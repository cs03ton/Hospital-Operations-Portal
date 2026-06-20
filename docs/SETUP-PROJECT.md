# HOP Project Setup Guide

คำสั่งสร้างโปรเจกต์ Hospital Operations Portal (HOP)

Tech Stack

```text
Frontend : React + Vite + TypeScript + MUI
Backend  : .NET 9 Web API
Database : PostgreSQL
Deploy   : Ubuntu Server + Docker
```

---

# 1. Project Structure

```text
hospital-operations-portal/
│
├── frontend/
├── backend/
├── database/
├── docs/
├── docker-compose.yml
└── README.md
```

สร้างโฟลเดอร์หลัก ถ้ามีอยู่แล้วไม่เป็นไร

```bash
mkdir frontend backend database docs
```

---

# 2. Create Frontend

## React + Vite + TypeScript

```bash
cd frontend

npm create vite@latest . -- --template react-ts
npm install
```

## Install UI / Form / API Packages

```bash
npm install @mui/material @mui/icons-material @emotion/react @emotion/styled
npm install @tanstack/react-query axios react-router-dom
npm install react-hook-form yup @hookform/resolvers
npm install dayjs
```

## Install Dev Tools

```bash
npm install -D eslint prettier
```

Run frontend

```bash
npm run dev
```

---

# 3. Frontend Theme

สร้างไฟล์

```text
frontend/src/theme.ts
```

# Hospital Identity Configuration

ระบบต้องอ่านชื่อระบบและชื่อโรงพยาบาลจาก Environment Variable ผ่าน config กลาง `frontend/src/config/appConfig.ts`

Frontend:

```env
VITE_APP_NAME=Hospital Operations Portal
VITE_HOSPITAL_NAME=โรงพยาบาลนาหมื่น
```

ห้าม hardcode ชื่อโรงพยาบาลใน Component โดยตรง ให้เรียกใช้ `appConfig`, `appName`, หรือ `hospitalName` จากไฟล์ config กลาง

# UI Design Philosophy

รูปแบบ UI ที่ต้องการ

- Modern Healthcare Dashboard
- Clean
- Professional
- Minimal
- Mobile Friendly
- Large Readable Text
- Suitable for Hospital Staff
- Suitable for Executive Dashboard

หลีกเลี่ยง

- Dark Theme เป็นค่าเริ่มต้น
- สีฉูดฉาด
- Animation มากเกินไป
- Layout ซับซ้อน

## Theme สีโรงพยาบาล เขียว-ฟ้า

```ts
import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#0F766E",
      light: "#14B8A6",
      dark: "#115E59",
      contrastText: "#FFFFFF",
    },
    secondary: {
      main: "#0284C7",
      light: "#38BDF8",
      dark: "#0369A1",
      contrastText: "#FFFFFF",
    },
    background: {
      default: "#F8FAFC",
      paper: "#FFFFFF",
    },
    success: {
      main: "#16A34A",
    },
    warning: {
      main: "#F59E0B",
    },
    error: {
      main: "#DC2626",
    },
  },
  shape: {
    borderRadius: 14,
  },
  typography: {
    fontFamily: [
      "Prompt",
      "Sarabun",
      "Roboto",
      "Arial",
      "sans-serif",
    ].join(","),
  },
});
```

## Theme สีกรมท่า-ฟ้า ดูเป็นทางการ

```ts
import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#1E3A8A",
      light: "#3B82F6",
      dark: "#172554",
      contrastText: "#FFFFFF",
    },
    secondary: {
      main: "#0891B2",
      light: "#22D3EE",
      dark: "#155E75",
      contrastText: "#FFFFFF",
    },
    background: {
      default: "#F1F5F9",
      paper: "#FFFFFF",
    },
  },
  shape: {
    borderRadius: 12,
  },
  typography: {
    fontFamily: [
      "Prompt",
      "Sarabun",
      "Roboto",
      "Arial",
      "sans-serif",
    ].join(","),
  },
});
```

## ใช้ Theme ใน `main.tsx`

```tsx
import React from "react";
import ReactDOM from "react-dom/client";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { theme } from "./theme";
import App from "./App";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  </React.StrictMode>
);
```

---

# 3.1 Localization & Language Standards

## Primary Language

ระบบ Hospital Operations Portal (HOP) ใช้งานภาษาไทยเป็นหลัก

ผู้ใช้งานส่วนใหญ่ประกอบด้วย

* บุคลากรทางการแพทย์
* เจ้าหน้าที่โรงพยาบาล
* หัวหน้าหน่วยงาน
* ผู้บริหาร

ดังนั้น UI และข้อความต่าง ๆ ควรใช้ภาษาไทยเป็นค่าเริ่มต้น

---

## Language Rules

| Area | Language |
|---|---|
| Frontend UI | ภาษาไทย |
| Database | English |
| Source Code | English |
| Document | ภาษาไทย |
| API Endpoint | English |
| Report | ภาษาไทย |

## User Interface Language

Frontend Requirements

ใช้ภาษาไทยสำหรับ

* Menu
* Form Label
* Button
* Validation Message
* Notification Message
* Dashboard
* Report
* Error Message

ตัวอย่าง

ถูกต้อง

```text
เข้าสู่ระบบ
บันทึกข้อมูล
ยกเลิก
อนุมัติ
ไม่อนุมัติ
ค้นหา
เพิ่มข้อมูล
แก้ไขข้อมูล
ลบข้อมูล
```

ไม่ควรใช้

```text
Login
Save
Cancel
Approve
Reject
Search
Add
Edit
Delete
```

ยกเว้นคำศัพท์ทางเทคนิคที่ไม่มีคำแปลที่เข้าใจง่าย

---

## Database Standards

ใช้ชื่อ Table และ Column เป็นภาษาอังกฤษเท่านั้น

ตัวอย่าง

```sql
users
departments
leave_requests
repair_requests
inventory_items
```

ไม่ใช้

```sql
ผู้ใช้งาน
แผนก
ข้อมูลลา
```

---

## Source Code Standards

ใช้ภาษาอังกฤษสำหรับ

* Class Name
* Method Name
* Variable Name
* API Endpoint
* Database Object

ตัวอย่าง

```csharp
UserController
DepartmentService
CreateLeaveRequest
```

---

## Report Standards

รายงานที่แสดงต่อผู้ใช้งาน

ใช้ภาษาไทย

ตัวอย่าง

```text
รายงานการลา
รายงานการยืมอุปกรณ์
รายงานการแจ้งซ่อม
```

---

## Date Format

ใช้รูปแบบประเทศไทย

```text
01/01/2569
15/07/2569
```

หรือ

```text
1 มกราคม 2569
15 กรกฎาคม 2569
```

รองรับ พ.ศ.

---

## Number Format

ใช้รูปแบบประเทศไทย

```text
1,000
10,500
100,000
```

---

## Future Support

ระบบควรรองรับ Multi-Language ในอนาคต

Architecture ควรรองรับ

```text
th-TH (Default)
en-US (Future)
```

แต่ Phase แรกให้พัฒนาเฉพาะภาษาไทยก่อน


# 3.2 Branding & UI Design Standard

## Hospital Branding

ระบบ Hospital Operations Portal (HOP) ต้องใช้ Branding ของโรงพยาบาลเป็นมาตรฐานเดียวกันทั้งระบบ

หากมีไฟล์ Logo ของโรงพยาบาลอยู่ใน Repository ให้ใช้เป็น Brand Identity หลัก

---

## Logo Location

ไฟล์ Logo จะถูกจัดเก็บใน

```text
frontend/src/assets/logo/
```

ตัวอย่าง

```text
frontend/src/assets/logo/
├── hospital-logo.png
├── hospital-logo.svg
└── favicon.ico
```

---

## Design Requirements

เมื่อออกแบบหน้า UI ใหม่

AI ต้อง

* แสดง Logo โรงพยาบาลในหน้า Login
* แสดง Logo โรงพยาบาลใน Sidebar
* แสดง Logo โรงพยาบาลใน Header
* ใช้ Logo เป็นจุดอ้างอิงในการเลือก Theme สี

---

## Theme Generation

หากพบไฟล์ Logo

AI ควรวิเคราะห์สีหลักจาก Logo และสร้าง Theme ให้สอดคล้อง

ตัวอย่าง

```text
Primary Color
Secondary Color
Background Color
Accent Color
```

---

## Login Page

หน้า Login ควรมี

```text
[Logo โรงพยาบาล]

Hospital Operations Portal

ระบบบริหารจัดการงานภายในโรงพยาบาล

Username
Password

[เข้าสู่ระบบ]
```

---

## Sidebar

Sidebar ควรแสดง

```text
[Logo]

Hospital Operations Portal
(HOP)

----------------

Dashboard
ระบบลา
ระบบยืมอุปกรณ์
ระบบแจ้งซ่อม
...
```

---

## Responsive Design

Mobile

```text
Logo
Hospital Operations Portal
```

Desktop

```text
Logo + Hospital Name
```

---

## Logo Protection Rules

ห้าม

* ยืด Logo จนผิดสัดส่วน
* เปลี่ยนสี Logo
* ครอบทับ Logo ด้วยสีอื่น
* ตัด Logo บางส่วนออก

---

## Assets Structure

```text
frontend/src/assets/
│
├── logo/
│   ├── hospital-logo.svg
│   ├── hospital-logo.png
│   └── favicon.ico
│
├── images/
│
└── icons/
```

---

## Future Enhancement

หากมีหลายโรงพยาบาลในอนาคต

รองรับ

```text
Tenant Branding

Logo
Theme
Hospital Name
Color Scheme
```

แยกตามหน่วยงานได้


# 4. Create Backend

กลับไปที่ root project

```bash
cd ../backend
```

สร้าง .NET Web API

```bash
dotnet new webapi -n Hop.Api
cd Hop.Api
```

ติดตั้ง Package

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
dotnet add package Swashbuckle.AspNetCore
```

Run backend

```bash
dotnet run
```

## Visual Studio 2022 Solution

สร้าง Solution File ที่ root project สำหรับเปิดด้วย Visual Studio 2022

```bash
cd ..
dotnet new sln --name Hospital-Operations-Portal
dotnet sln Hospital-Operations-Portal.sln add backend/Hop.Api/Hop.Api.csproj
dotnet sln Hospital-Operations-Portal.sln add backend/Hop.Api.Tests/Hop.Api.Tests.csproj
dotnet sln Hospital-Operations-Portal.sln list
dotnet build Hospital-Operations-Portal.sln
```

Project references ปัจจุบัน:

```text
Hop.Api.Tests
└─ Hop.Api
```

หากเพิ่ม Clean Architecture projects ในอนาคต ให้เพิ่มเข้า Solution และตั้ง references ตามแนวทางนี้:

```text
Hop.Api
├─ Hop.Application
└─ Hop.Infrastructure

Hop.Application
└─ Hop.Domain

Hop.Infrastructure
├─ Hop.Application
└─ Hop.Domain
```

---

# 5. Backend Project Structure

```text
backend/Hop.Api/
│
├── Controllers/
├── Data/
├── Models/
├── DTOs/
├── Services/
├── Interfaces/
├── Helpers/
├── Migrations/
├── Program.cs
└── appsettings.json
```

สร้างโฟลเดอร์

```bash
mkdir Controllers Data Models DTOs Services Interfaces Helpers
```

---

# 6. PostgreSQL Database

## Create Database

```sql
CREATE DATABASE hop_db;
```

## Create User

```sql
CREATE USER hop_user WITH PASSWORD 'StrongPasswordHere';
```

## Grant Permission

```sql
GRANT ALL PRIVILEGES ON DATABASE hop_db TO hop_user;
```

---

# 7. Connection String

แก้ไฟล์

```text
backend/Hop.Api/appsettings.json
```

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=hop_db;Username=hop_user;Password=StrongPasswordHere"
  },
  "Jwt": {
    "Key": "CHANGE_THIS_TO_LONG_SECRET_KEY",
    "Issuer": "Hop.Api",
    "Audience": "Hop.Client"
  },
  "Line": {
    "ChannelAccessToken": "YOUR_LINE_CHANNEL_ACCESS_TOKEN",
    "ChannelSecret": "YOUR_LINE_CHANNEL_SECRET"
  }
}
```

---

# 8. Docker Compose เบื้องต้น

สร้างไฟล์ที่ root project

```text
docker-compose.yml
```

```yaml
services:
  postgres:
    image: postgres:16
    container_name: hop-postgres
    restart: always
    environment:
      POSTGRES_DB: hop_db
      POSTGRES_USER: hop_user
      POSTGRES_PASSWORD: StrongPasswordHere
    ports:
      - "5432:5432"
    volumes:
      - hop_postgres_data:/var/lib/postgresql/data

volumes:
  hop_postgres_data:
```

Run database

```bash
docker compose up -d
```

---

# 9. Database Folder

```text
database/
│
├── schema.sql
├── seed.sql
└── backup/
```

สร้างไฟล์

```bash
cd ../../database
touch schema.sql seed.sql
mkdir backup
```

---

# 10. Initial Tables

ไฟล์

```text
database/schema.sql
```

```sql
CREATE TABLE departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_code VARCHAR(50),
    fullname VARCHAR(255) NOT NULL,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    role VARCHAR(50) NOT NULL,
    department_id UUID REFERENCES departments(id),
    line_user_id VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE leave_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    leave_type VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    total_days NUMERIC(5,2) NOT NULL,
    reason TEXT,
    status VARCHAR(50) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE approval_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_type VARCHAR(100) NOT NULL,
    request_id UUID NOT NULL,
    approver_id UUID REFERENCES users(id),
    action VARCHAR(50) NOT NULL,
    remark TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

# 11. Git Init

กลับไป root project

```bash
cd ..
git init
```

สร้าง `.gitignore`

```bash
touch .gitignore
```

```gitignore
# Node
node_modules
dist
.env

# .NET
bin
obj
*.user
*.suo

# Database
*.backup
*.dump

# OS
.DS_Store
Thumbs.db
```

Commit แรก

```bash
git add .
git commit -m "Initial Hospital Operations Portal project"
```

---

# 12. Recommended Theme

สำหรับระบบโรงพยาบาล แนะนำใช้ธีมนี้ก่อน

```text
Primary   : Teal Green
Secondary : Sky Blue
Background: Slate Light
Style     : Clean, Rounded, Modern
```

เหตุผล

* ดูเป็น Healthcare
* อ่านง่าย
* ไม่ล้าสมัย
* เข้ากับ Dashboard / Form / Table
* ผู้ใช้ทั่วไปไม่งง

---

# 13. Next Step

หลังสร้าง Project แล้วให้ทำตามลำดับนี้

```text
1. Auth/Login
2. User Management
3. Department Management
4. Layout/Menu
5. Dashboard
6. Leave Module
7. LINE Notification
8. Approval Engine
9. Repair Module
10. Borrow Module
```

# TaskFlow Frontend

Complete React + Vite + Tailwind CSS frontend for the TaskFlow Task Management System.

---

## 🚀 Quick Start

```bash
# 1. Copy environment variables
cp .env.example .env

# 2. Install dependencies
npm install

# 3. Start development server
npm run dev

# Opens: http://localhost:3000
# API proxy: /api/* → http://localhost:5000 (ASP.NET Core)
```

---

## 📁 Project Structure

```
src/
├── api/           ← Axios + all API calls (authApi, tasksApi, ...)
├── store/         ← Zustand global state (auth, ui, notifications)
├── router/        ← React Router + ProtectedRoute + RoleRoute
├── layouts/       ← AuthLayout, DashboardLayout, Sidebar, Header
├── hooks/         ← useAuth, useSignalR, useAuth
├── components/ui/ ← Button, Input, Modal, Badge, Avatar, PageLoader
└── pages/
    ├── auth/      ← Login, Register, ForgotPassword, ResetPassword
    ├── DashboardPage      ← Stats + Charts + Tasks + Activity
    ├── ProjectsPage       ← Project cards + Create modal
    ├── ProjectDetailPage  ← Kanban board
    ├── TasksPage          ← Task grid + filters + Create modal
    ├── UsersPage          ← User management (Admin only)
    ├── NotificationsPage  ← Notification feed
    ├── ProfilePage        ← Edit profile, security, sessions
    └── NotFoundPage       ← 404
```

---

## 🔐 Authentication

JWT tokens are stored in Zustand (persisted to `localStorage`).
- **Access Token**: 15 min expiry — auto-attached to all Axios requests
- **Refresh Token**: 7 days — auto-refreshed on 401 responses (transparent to UI)
- **Logout**: Clears localStorage + React Query cache

---

## ⚡ Real-Time (SignalR)

SignalR connects to `/hubs/notifications` on app startup.

Events received:
- `TaskCreated` / `TaskUpdated` / `TaskStatusChanged` → invalidates tasks cache
- `CommentAdded` → invalidates comments cache
- `NewNotification` → bumps bell badge + shows toast

---

## 🎨 Design System

| Token | Value |
|-------|-------|
| Primary | `#6366f1` (Indigo) |
| Background | `#0f1117` |
| Surface | `#161929` |
| Done | `#10b981` |
| Warning | `#f59e0b` |
| Danger | `#ef4444` |
| Font | Inter (Google Fonts) |

---

## 🏗️ State Strategy

| Type | Tool |
|------|------|
| Auth + UI state | **Zustand** |
| Server data (API) | **React Query** |
| Form values | **React Hook Form** |

---

## 📦 Build for Production

```bash
npm run build   # Outputs to dist/
npm run preview # Preview the production build
```

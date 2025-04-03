# Chroma

A modular, interactive game platform powered by a custom hardware-software ecosystem, built for real-time engagement through networked tile inputs and dynamic visuals. Designed for immersive, multiplayer physical games using RGB floor tiles and a central Godot client.

![image](https://github.com/user-attachments/assets/c94dab46-e484-4705-a22c-eb99ede6bd35)

---

## 🔧 Architecture Overview

### 🧠 Internal Components
- **Database**: PostgreSQL
- **Server**: ASP.NET Core (with Entity Framework Core ORM)
- **Admin Client** (WIP):
  - Game configuration tools
  - Realtime and historical data monitoring
  - Analytics dashboards
- **Game Clients**: Developed in **Godot 4 (.NET)**

### 🌐 External Integrations
- Vendor-provided microcontroller and tile hardware (UDP protocol)
- Custom-built communication protocol for real-time input and visual feedback

![image](https://github.com/user-attachments/assets/a7ad6321-a416-4970-8716-c71b1b00f5fa)

---

## 🗄️ Database
- **Technology**: PostgreSQL
- **Setup**: *To be completed*

---

## 🌐 Server
- **Framework**: ASP.NET Core
- **ORM**: Entity Framework Core
- **Setup**: *To be completed*

---

## 🧪 Local Client Setup

> *Temporary setup flow; will later be managed by the backend server.*

### Prerequisites
- [Node.js / NPM](https://nodejs.org/en/download/package-manager)

### Steps
```bash
cd ./Chroma.LocalClient
npm install
npm run dev
```
Visit [http://localhost:5173](http://localhost:5173) if it doesn’t open automatically.

---

## 🎮 Godot Game Client

### Getting Started
1. Download [Godot 4 (.NET)](https://godotengine.org/)
2. Import the project from `GodotClient/project.godot`
3. (Optional) Configure external editor in:
   `Editor > Editor Settings > DotNet > Editor`

### Debugging
Set `GODOT_BIN` as an environment variable to point to the Godot executable.  
Also update `DebugLauncher > Properties > Debugging`.  
More info: https://github.com/godotengine/godot-proposals/issues/8648#issuecomment-1974909410

---

## ✅ Testing

### Unit Tests
- Framework: [xUnit](https://xunit.net/)
```bash
dotnet test
```

### Godot Tests
- Framework: [gdUnit4](https://mikeschulze.github.io/gdUnit4/)
- Tests are located in `Chroma.GodotClient/Tests`

---

## 🚀 Releasing (Exporting from Godot)

1. Download export templates: https://godotengine.org/download/archive/
2. Install via:
   `Editor > Manage Export Templates`
3. Download [rcedit](https://github.com/electron/rcedit/releases)
4. Set `rcedit` path in:
   `Editor > Settings > Export > Windows`
5. Export the game via:
   `Project > Export > Export Project`

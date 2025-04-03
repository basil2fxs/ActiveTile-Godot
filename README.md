# Chroma

## Table of Contents
* [Architecture](#architecture)
  * [Internal Components](#planned-internal)
  * [External Integrations](#external-integrations)
* [Database](#database)
  * [Setup](#setup)
* [Server](#server)
  * [Setup](#setup)
* [Admin Client](#admin-client)
  * [Setup](#setup)
* [Godot Client](#godotclient)
  * [Setup](#setup)


## Architecture
### Planned Internal:
- Database (Postgres)
- Server (ASPNET Core)
- Admin Client (TBC)
  - Game configuration
  - Game monitoring
  - Realtime/Historic data analytics
- Game Clients (Godot)

### External Integrations:
- Vendor Microcontroller and Tiles

<img src="https://github.com/user-attachments/assets/89e43dc8-9f39-46e3-bc4c-83cc6ad8b65c">

## Database
- DB: Postgresql

### Setup
TBC

## Server
- Web Framework: ASPNET Core
- ORM Framework: Entity Framework Core

### Setup
TBC

## LocalClient
### Prerequisites
- Node Package Manager ([NPM](https://nodejs.org/en/download/package-manager)) installed

### Setup
Note: These steps are only temporary. These will be covered by the backend Server setup later.

1. Head to the client directory, ``cd ./Chroma.LocalClient``
1. Install all dependencies, ``npm install``
1. Run the client, ``npm run dev``. This should open the browser. Otherwise, head to the following url on your browser, ``http://localhost:5173``

## GodotClient

Note: GodotClient has its own `.sln` in the same folder because Godot currently requires this.

### Setup

1. Download Godot 4 (the .NET variant) from https://godotengine.org/. It has no installer so put it somewhere reasonable. The first time you run it, `Import` the project (`GodotClient/project.godot`).
1. To use an external editor like VS Code or Visual Studio, in the Godot editor go to `Editor > Editor Settings > DotNet > Editor` and set the external editor. Leave other fields at defaults.
1. To allow the external editor to run the project in Godot, set the `GODOT_BIN` environment variable to the executable downloaded above. See more instructions [here](https://github.com/Mikeware/GoDotNet.BlankTemplate?tab=readme-ov-file#godot-location).
   Additionally, the DebugLauncher project needs its `Properties > Debugging` updated as described [here](https://github.com/godotengine/godot-proposals/issues/8648#issuecomment-1974909410). Note we use call the envvar `GODOT_BIN` instead of `GODOT4` because the former is required by gdUnit4 (see [Godot Tests](#godot-tests) below).

### Testing

#### Pure C# Tests

We use [xUnit](https://xunit.net/). You can just run `dotnet test`, or from the Test Explorer in Visual Studio or other IDE. This will also run the [Godot Tests](#godot-tests) below.

#### Godot Tests

We use [gdUnit4](https://mikeschulze.github.io/gdUnit4/), along with the [adapter for VSTest](https://mikeschulze.github.io/gdUnit4/csharp_project_setup/vstest-adapter/). This allows us to run all tests with `dotnet test`. You can also [run and debug tests from within Godot](https://mikeschulze.github.io/gdUnit4/testing/run-tests/).

See [gdUnit4 docs](https://mikeschulze.github.io/gdUnit4/advanced_testing/index/) for how to use the test APIs.

Since gdUnit4 is installed as an addon into a Godot project, tests go in `Chroma.GodotClient/Tests`, and we need to exclude any new test files from being [exported](#release).

### Release

This is known as "Exporting" in Godot. More details in https://docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html.

1. Download the export templates for your version of Godot: https://godotengine.org/download/archive/
1. Install them in `Editor > Manage Export Templates`.
1. Download `rcedit`: https://github.com/electron/rcedit/releases
1. Update the `rcedit` path in `Editor > Settings > Export > Windows`.
Go to `Project > Export` and click `Export Project`.

Я общаюсь на русском языке! Я использую Rider IDE!
==================================================

# Project Overview

This project is a **Game Tracker** application for Windows. It consists of two main components:

1.  **`GameTrackerService`**: A .NET Windows Service that runs in the background, monitors running processes, and tracks the time spent on each application. It uses a SQLite database (`tracker.db`) to store the tracking data.
2.  **`GameTrackerClient`**: A .NET Windows Forms application that provides a user interface to view the tracked data. It communicates with the `GameTrackerService` via a Named Pipe.

## Technologies Used

*   **.NET 8**: The core framework for both the service and the client.
*   **Windows Forms**: Used for the client's graphical user interface.
*   **Entity Framework Core with SQLite**: Used for data persistence in the service.
*   **Serilog**: Used for logging in both the service and the client.
*   **Named Pipes**: Used for inter-process communication (IPC) between the service and the client.

## Architecture

The application follows a client-server architecture:

*   The **`GameTrackerService`** acts as the server, running as a Windows Service. It's responsible for:
    *   Tracking running processes.
    *   Storing process data in a SQLite database.
    *   Exposing an IPC endpoint via a Named Pipe for the client to connect to.
*   The **`GameTrackerClient`** acts as the client. It's a desktop application that:
    *   Connects to the service via the Named Pipe.
    *   Sends requests to the service to get process statistics.
    *   Displays the data to the user in a clear and interactive way.

# Building and Running

## Building the Project

To build the solution, you can use Visual Studio or the `dotnet` CLI:

```bash
dotnet build GameTrackerSolution.sln
```

## Running the Application

### 1. Install and Run the Service

The `GameTrackerService` needs to be installed as a Windows Service to function correctly.

**Publish the service:**

```bash
cd GameTrackerService
dotnet publish -c Release -r win-x64 --self-contained true
```

**Install the service (requires an administrator command prompt):**

```bash
sc create GameTrackerService binPath="C:\path\to\your\project\GameTrackerSolution\GameTrackerService\bin\Release\net8.0\win-x64\publish\GameTrackerService.exe"
```

**Start the service:**

```bash
sc start GameTrackerService
```

### 2. Run the Client

Once the service is running, you can run the `GameTrackerClient` by executing the `GameTrackerClient.exe` file located in the `GameTrackerClient\bin\Debug\net8.0-windows` or `GameTrackerClient\bin\Release\net8.0-windows` directory.

# Development Conventions

*   **Logging**: The project uses Serilog for structured logging. Logs for the service are stored in `C:\ProgramData\GameTracker\logs`, and client logs are in the application's base directory.
*   **IPC**: Communication between the client and service is handled via Named Pipes. The pipe name is `GameTrackerPipe`. The `IpcClient` and `IpcServer` classes encapsulate the communication logic.
*   **Configuration**: Application settings are stored in `appsettings.json` files for both the client and the service.
*   **Database**: The service uses a SQLite database named `tracker.db`. The database file is located in `C:\ProgramData\GameTracker` in production and in the application's base directory during development.

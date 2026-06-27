# Quiz App - .NET

This repository contains a Windows Forms quiz application and a small ASP.NET Core backend that provides a SQLite-backed data API for the desktop client.

## Projects

- `Quiz App/Quiz App.csproj` - .NET Framework 4.7.2 Windows Forms application.
- `QuizApp.Backend/QuizApp.Backend.csproj` - ASP.NET Core Web API targeting .NET 8 with a local SQLite database.

## Requirements

- Visual Studio 2022 with .NET desktop development workload.
- .NET 8 SDK or newer.
- .NET Framework 4.7.2 developer targeting pack.

## Restore and Build

Restore and build the backend:

```powershell
dotnet restore .\QuizApp.Backend\QuizApp.Backend.csproj
dotnet build .\QuizApp.Backend\QuizApp.Backend.csproj --configuration Release
```

Build the full solution from Visual Studio, or with Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ".\Quiz App.sln" /restore /p:Configuration=Release
```

## Run

Start the backend API:

```powershell
dotnet run --project .\QuizApp.Backend\QuizApp.Backend.csproj
```

The backend creates `quizApp.db` beside its compiled output if the database does not already exist, including default admin and student records for first-run use.

Open `Quiz App.sln` in Visual Studio to run the Windows Forms client.

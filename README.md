# Common.Web
Common Web Library for freebyTech Software written in .NET Core 2.1. Because it is written in .NET Core, this library can be run on Windows, Linux, or a Mac.

Technologies currently used are:

* .NET Core 2.1

## Installing Required Tools
| Tool                               | URL                                              |
| ---------------------------------- | ------------------------------------------------ |
| Git for Windows                    | https://git-scm.com/download/win                 |
| Install Latest .NET Core 2.1 SDK   | https://www.microsoft.com/net/download/windows   |
| Install Latest LTS Version of Node | https://nodejs.org/en/                           |


# Building this project
```
# Building the library
cd ./src/freebyTech.Common.Web
dotnet build

# Building and running unit tests
cd ./src/freebyTech.Common.Web.Tests
dotnet test

# Build via the docker build file
docker build ./src -t freebyTech/common-web
```

# Pulling this package from NuGet
The standard released version of this package can be referenced in a project by pulling it from NuGet.
```
# VS Code or command line usage
dotnet add package freebyTech.Common.Web

# Visual Studio Package Manager Console
install-package freebyTech.Common.Web
```
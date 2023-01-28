FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /lib

# copy csproj files and restore as distinct layers so we break cache as little as possible
COPY ./freebyTech.Common.Web/*.csproj ./freebyTech.Common.Web/
COPY ./freebyTech.Common.Web.Tests/*.csproj ./freebyTech.Common.Web.Tests/

WORKDIR /lib/freebyTech.Common.Web
RUN dotnet restore

WORKDIR /lib/freebyTech.Common.Web.Tests
RUN dotnet restore

ARG BUILD_VERSION
ARG PACKAGE_ID
ENV BUILD_VERSION=${BUILD_VERSION}
ENV PACKAGE_ID=${PACKAGE_ID}

# copy everything else and build library
WORKDIR /lib/freebyTech.Common.Web
COPY ./freebyTech.Common.Web/. .
RUN dotnet build

# copy unit test project and run tests
WORKDIR /lib/freebyTech.Common.Web.Tests
COPY ./freebyTech.Common.Web.Tests/. .
RUN dotnet build
RUN dotnet test

WORKDIR /lib/freebyTech.Common.Web
RUN dotnet build -c Debug -o /lib/debug
RUN dotnet pack -c Debug --include-symbols --include-source -o /lib/nuget_d -p:SymbolPackageFormat=snupkg
RUN dotnet build -c Release -o /lib/release
RUN dotnet pack -c Release -o /lib/nuget
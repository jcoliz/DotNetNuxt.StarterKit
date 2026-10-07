FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /App

# Restore as distinct layer
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Application/*.csproj src/Application/
COPY src/Controllers/*.csproj src/Controllers/
COPY src/Data/Postgres/*.csproj src/Data/Postgres/
COPY src/Entities/*.csproj src/Entities/
COPY src/BackEnd/*.csproj src/BackEnd/
COPY src/ServiceDefaults/*.csproj src/ServiceDefaults/
COPY submodules/DotNetNuxt/Directory.Build.props submodules/DotNetNuxt/
COPY submodules/DotNetNuxt/src/Hosting/*.csproj submodules/DotNetNuxt/src/Hosting/
COPY submodules/DotNetNuxt/src/AspNetCore/*.csproj submodules/DotNetNuxt/src/AspNetCore/
RUN dotnet restore ./src/BackEnd/

# Copy each source project
COPY src/Entities/. src/Entities/
COPY src/Data/Postgres/. src/Data/Postgres/
COPY src/Application/. src/Application/
COPY src/Controllers/. src/Controllers/
COPY src/BackEnd/. src/BackEnd/
COPY src/ServiceDefaults/. src/ServiceDefaults/
COPY submodules/DotNetNuxt/src/Hosting/. submodules/DotNetNuxt/src/Hosting/
COPY submodules/DotNetNuxt/src/AspNetCore/. submodules/DotNetNuxt/src/AspNetCore/
COPY docker/. docker/

# Software version number
#   - Should correspond to tag
#   - Including default value so if someone just runs "docker build", it will work
ARG SOLUTION_VERSION
ENV SOLUTION_VERSION=${SOLUTION_VERSION:-container}

# Build and publish a release
RUN dotnet publish ./src/BackEnd/ -c Release -o out --no-restore -p:SolutionVersion=$SOLUTION_VERSION

# Optionally include docker/config.toml when present.
RUN if [ -f docker/config.toml ]; then cp docker/config.toml out/config.toml; fi

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /App

# Install Curl for healthcheck
RUN apt-get update -yq && apt-get install -yq curl

COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "DotNetNuxt.StarterKit.BackEnd.dll"]
HEALTHCHECK --interval=10s --timeout=5s --retries=5 CMD [ "curl", "http://localhost:8080/health" ]

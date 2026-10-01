FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /App

# Install Node
RUN apt-get update -yq \
    && apt-get -yq install curl gnupg ca-certificates \
    && curl -L https://deb.nodesource.com/setup_20.x | bash \
    && apt-get update -yq \
    && apt-get install -yq \
        nodejs

# Restore as distinct layer
COPY Directory.Build.props .
COPY src/Application/*.csproj src/Application/
COPY src/Controllers/*.csproj src/Controllers/
COPY src/Data/Postgres/*.csproj src/Data/Postgres/
COPY src/Entities/*.csproj src/Entities/
COPY src/BackEnd/*.csproj src/BackEnd/
COPY src/ServiceDefaults/*.csproj src/ServiceDefaults/
RUN dotnet restore ./src/BackEnd/

# Software version number
#   - Should correspond to tag
#   - Including default value so if someone just runs "docker build", it will work
ARG SOLUTION_VERSION=docker
ENV SOLUTION_VERSION=${SOLUTION_VERSION}

# Copy everything
# TODO: Would like to build each with --no-dependencies, but can't get that to work :(
COPY src/Entities/. src/Entities/
COPY src/Data/Postgres/. src/Data/Postgres/
COPY src/Application/. src/Application/
COPY src/Controllers/. src/Controllers/
COPY src/BackEnd/. src/BackEnd/
COPY src/ServiceDefaults/. src/ServiceDefaults/
COPY docker/. docker/

# Build and publish a release
RUN dotnet publish ./src/BackEnd/ -c Release -o out -p:SolutionVersion=$SOLUTION_VERSION

# Optionally include docker/config.toml in Main.Vue when present.
RUN if [ -f docker/config.toml ]; then cp docker/config.toml out/config.toml; fi

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /App

# Install Curl for healthcheck
RUN apt-get update -yq && apt-get install -yq curl

COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "DotNetNuxt.StartKit.dll"]
HEALTHCHECK --interval=10s --timeout=5s --retries=5 CMD [ "curl", "http://localhost:8080/health" ]

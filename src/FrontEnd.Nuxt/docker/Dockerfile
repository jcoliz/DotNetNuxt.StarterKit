# syntax=docker/dockerfile:1

# Build stage
FROM node:24-alpine AS builder

# Install pnpm
RUN corepack enable && corepack prepare pnpm@11.20.0 --activate

WORKDIR /app

# Copy package files including pnpm lockfile
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./

# Install dependencies using pnpm (scripts skipped: nuxt prepare needs the source, and runs during generate)
RUN --mount=type=cache,id=pnpm,target=/root/.local/share/pnpm/store \
    pnpm install --frozen-lockfile --ignore-scripts

# Copy source code
COPY ./ ./

# Set the API base URL at build time
ARG NUXT_PUBLIC_API_BASE_URL=https://your-backend.azurewebsites.net
ENV NUXT_PUBLIC_API_BASE_URL=$NUXT_PUBLIC_API_BASE_URL

# Set the solution version at build time
ARG NUXT_PUBLIC_SOLUTION_VERSION=
ENV NUXT_PUBLIC_SOLUTION_VERSION=$NUXT_PUBLIC_SOLUTION_VERSION

# Set the application insights connection string at build time
ARG NUXT_PUBLIC_APPLICATION_INSIGHTS_CONNECTION_STRING=
ENV NUXT_PUBLIC_APPLICATION_INSIGHTS_CONNECTION_STRING=$NUXT_PUBLIC_APPLICATION_INSIGHTS_CONNECTION_STRING

# Build the static site
RUN pnpm run generate

# Production stage
FROM nginx:1.30-alpine

# Copy the built static files from builder stage
COPY --from=builder /app/.output/public /usr/share/nginx/html

# Copy custom nginx configuration (optional)
COPY docker/nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

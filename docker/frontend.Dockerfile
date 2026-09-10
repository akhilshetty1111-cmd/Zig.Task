# Build context is the REPO ROOT (see docker-compose.yml), not
# frontend/zigzag-web/ - this Dockerfile needs both the frontend source AND
# docker/nginx.conf, which are siblings, so the context has to be their
# common ancestor. Every COPY path below is written relative to the root.

# ---- Stage 1: build -------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

# Same layer-caching idea as the backend: copy only the lockfile first, so
# `npm ci` (which reinstalls everything from scratch) only re-runs when a
# dependency actually changed, not on every source edit.
COPY frontend/zigzag-web/package.json frontend/zigzag-web/package-lock.json ./
RUN npm ci

# Vite reads any environment variable prefixed VITE_ at build time and bakes
# its value directly into the compiled JavaScript - this is the ONLY point
# where the frontend learns the API's address, since there is no server-side
# process left at runtime to read an environment variable from (see the
# nginx runtime stage below). ARG receives it from docker-compose.yml's
# build.args; ENV makes it visible to the `npm run build` process.
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY frontend/zigzag-web/ .
RUN npm run build

# ---- Stage 2: runtime -------------------------------------------------------
# nginx:alpine (~40MB) just serves the static files `npm run build` produced.
# There is no Node.js, no npm, no source code in the final image at all.
FROM nginx:alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

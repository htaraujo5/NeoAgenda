# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json ./
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

COPY . .
RUN npm run build

# Stage 2: Nginx Web Server
FROM nginx:alpine AS runtime

# Remove configuração padrão do Nginx
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copia configuração customizada com proxy reverso para a API
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copia estáticos compilados
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

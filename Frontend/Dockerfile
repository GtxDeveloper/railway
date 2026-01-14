# ----------------------------
# Stage 1: Сборка приложения
# ----------------------------
FROM node:20-alpine AS build
WORKDIR /app

# Копируем package.json и package-lock.json
COPY package*.json ./

# Устанавливаем зависимости (ci лучше для CI/CD, чем install)
RUN npm ci

# Копируем весь исходный код
COPY . .

# Собираем билд для продакшена
RUN npm run build -- --configuration production

# ----------------------------
# Stage 2: Запуск Nginx
# ----------------------------
FROM nginx:alpine

# Копируем наш конфиг Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Копируем собранные файлы Angular из первого этапа
# !!! ВНИМАНИЕ: Проверь путь ниже. Если у тебя Angular 17+, оставь /browser.
# Если старый - убери /browser. Замени 'tringelty-front' на имя своего проекта.
COPY --from=build /app/dist/tringelty-frontend/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

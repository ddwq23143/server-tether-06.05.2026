# 📩 C# ASP.NET Core API — Сохранение сообщений

Простой веб‑сервер на ASP.NET Core, который принимает сообщение от пользователя и сохраняет его в файл `messages.txt`.

---

## 🚀 Возможности

- Приём POST-запроса с формы
- Сохранение данных в текстовый файл
- Разрешён CORS (AllowAnyOrigin)
- Подключён OpenAPI (в режиме Development)
- Поддержка статических файлов (HTML страница)

---

## 🛠️ Технологии

- C#
- ASP.NET Core Web API
- JavaScript (Fetch API)
- System.IO (запись в файл)

---

## 📡 API

### POST
/api/home/savemessage

### Параметры (FormData)

- `Username` — имя пользователя  
- `Message` — сообщение  

Отправка должна быть `form-data` (`[FromForm]`).

---

## ▶️ Запуск проекта

1. Откройте проект в Visual Studio / Rider  
2. Запустите проект  
3. Перейдите в браузере:
http://localhost:5058

В корне проекта.

Формат записи:
Имя: Ivan | Сообщение: Привет!

---

## ⚠️ Важно

Если изменили порт — обновите его в HTML:

```js
const API_URL = 'http://localhost:5058/api/home/savemessage';
const API_URL = 'http://localhost:5000/api';

// Управление темой
function toggleTheme() {
    document.body.classList.toggle('dark-theme');
    const isDark = document.body.classList.contains('dark-theme');
    localStorage.setItem('app_theme', isDark ? 'dark' : 'light');
    document.getElementById('theme-toggle').innerText = isDark ? '☀️' : '🌙';
}
if (localStorage.getItem('app_theme') === 'dark') {
    document.body.classList.add('dark-theme');
    document.addEventListener("DOMContentLoaded", () => document.getElementById('theme-toggle').innerText = '☀️');
}

// Запрос профиля для проверки авторизации
async function checkAuth() {
    try {
        const response = await fetch(`${API_URL}/profile`, { credentials: 'include' });
        if (!response.ok) throw new Error('Не авторизован');
        return await response.json();
    } catch (err) {
        window.location.href = 'index.html'; // Выкидываем на логин
        return null;
    }
}

// Выход
async function logout() {
    await fetch(`${API_URL}/logout`, { method: 'POST', credentials: 'include' });
    window.location.href = 'index.html';
}

// Инициализация локальной БД для ТЕСТОВ (т.к. в C++ их пока нет)
function initLocalTestDB() {
    if (!localStorage.getItem('tests_db')) localStorage.setItem('tests_db', '[]');
    if (!localStorage.getItem('results_db')) localStorage.setItem('results_db', '[]');
}
initLocalTestDB();
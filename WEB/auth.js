async function login() {
    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;

    const res = await fetch(`${API_URL}/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ username, password })
    });

    if (res.ok) {
        // Узнаем роль пользователя, чтобы сделать правильный редирект
        const profile = await fetch(`${API_URL}/profile`, { credentials: 'include' }).then(r => r.json());
        window.location.href = profile.role === 'teacher' ? 'teacher.html' : 'student.html';
    } else {
        const errorText = await res.text();
        alert('Ошибка входа: ' + errorText);
    }
}

async function register() {
    const body = {
        username: document.getElementById('username').value.trim(),
        password: document.getElementById('password').value,
        firstname: document.getElementById('firstname').value.trim(),
        lastname: document.getElementById('lastname').value.trim(),
        role: document.getElementById('role').value
    };

    const res = await fetch(`${API_URL}/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(body)
    });

    if (res.ok) {
        alert('Успешно! Выполняется вход...');
        window.location.href = body.role === 'teacher' ? 'teacher.html' : 'student.html';
    } else {
        const errorText = await res.text();
        alert('Ошибка регистрации: ' + errorText);
    }
}
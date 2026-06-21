// --- ТЕМНАЯ ТЕМА ---
function toggleTheme() {
    document.body.classList.toggle('dark-theme');
    const isDark = document.body.classList.contains('dark-theme');
    localStorage.setItem('app_theme', isDark ? 'dark' : 'light');
    document.getElementById('theme-toggle').innerText = isDark ? '☀️' : '🌙';
}

if (localStorage.getItem('app_theme') === 'dark') {
    document.body.classList.add('dark-theme');
    document.addEventListener("DOMContentLoaded", () => {
        document.getElementById('theme-toggle').innerText = '☀️';
    });
}

// --- БАЗА ДАННЫХ В LOCALSTORAGE ---
function initDB() {
    if (!localStorage.getItem('accounts_db')) {
        localStorage.setItem('accounts_db', JSON.stringify([
            { username: 'teacher', password: '123', role: 'teacher', firstName: 'Иван', lastName: 'Иванов' },
            { username: 'student', password: '123', role: 'student', firstName: 'Алексей', lastName: 'Смирнов' },
            { username: 'ivan', password: '123', role: 'student', firstName: 'Иван', lastName: 'Петров' }
        ]));
    }
    if (!localStorage.getItem('tests_db')) {
        localStorage.setItem('tests_db', JSON.stringify([{
            id: 1,
            title: "Основы Web-технологий",
            allowedUsers: [], 
            questions: [
                { q: "Какой язык используется для стилизации?", options: ["HTML", "CSS", "JavaScript", "Python"], correct: 1 },
                { q: "Что означает HTML?", options: ["Hyper Text Markup Language", "Home Tool Markup Language"], correct: 0 }
            ]
        }]));
    }
    if (!localStorage.getItem('results_db')) {
        localStorage.setItem('results_db', JSON.stringify([]));
    }
}
initDB();

let currentUser = null;
let currentTestObj = null;
let chartInstance = null;
let questionCounter = 0;

// --- АВТОРИЗАЦИЯ И РЕГИСТРАЦИЯ ---
function login() {
    const u = document.getElementById('login-username').value.trim();
    const p = document.getElementById('login-password').value;
    const r = document.getElementById('login-role').value;
    
    const accounts = JSON.parse(localStorage.getItem('accounts_db'));
    const user = accounts.find(acc => acc.username === u && acc.password === p && acc.role === r);
    
    if (user) {
        currentUser = user;
        document.getElementById('login-username').value = '';
        document.getElementById('login-password').value = '';
        document.getElementById('login-firstname').value = '';
        document.getElementById('login-lastname').value = '';
        showDashboard();
    } else {
        alert('Неверный логин, пароль или роль!');
    }
}

function register() {
    const u = document.getElementById('login-username').value.trim();
    const p = document.getElementById('login-password').value;
    const r = document.getElementById('login-role').value;
    const fName = document.getElementById('login-firstname').value.trim();
    const lName = document.getElementById('login-lastname').value.trim();

    if (!u || !p || !fName || !lName) {
        return alert('Пожалуйста, заполните Логин, Пароль, Имя и Фамилию!');
    }

    const accounts = JSON.parse(localStorage.getItem('accounts_db'));
    if (accounts.find(acc => acc.username === u)) return alert('Пользователь с таким логином уже существует!');

    accounts.push({ username: u, password: p, role: r, firstName: fName, lastName: lName });
    localStorage.setItem('accounts_db', JSON.stringify(accounts));
    
    alert('Регистрация успешна! Теперь нажмите "Войти".');
}

function logout() {
    currentUser = null;
    document.getElementById('auth-view').classList.remove('hidden');
    document.getElementById('header-view').classList.add('hidden');
    document.getElementById('teacher-view').classList.add('hidden');
    document.getElementById('student-view').classList.add('hidden');
    document.getElementById('take-test-view').classList.add('hidden');
    document.getElementById('stats-view').classList.add('hidden');
}

function showDashboard() {
    document.getElementById('auth-view').classList.add('hidden');
    document.getElementById('header-view').classList.remove('hidden');
    
    const roleName = currentUser.role === 'teacher' ? 'Преподаватель' : 'Студент';
    document.getElementById('user-info').innerText = `${roleName}: ${currentUser.firstName} ${currentUser.lastName} (@${currentUser.username})`;

    if (currentUser.role === 'teacher') {
        document.getElementById('teacher-view').classList.remove('hidden');
        renderTeacherDashboard();
    } else {
        document.getElementById('student-view').classList.remove('hidden');
        renderStudentDashboard();
    }
}

// ==========================================
// ЛОГИКА ПРЕПОДАВАТЕЛЯ
// ==========================================
function renderTeacherDashboard() {
    const tests = JSON.parse(localStorage.getItem('tests_db'));
    const list = document.getElementById('teacher-tests-list');
    list.innerHTML = '';

    if(tests.length === 0) list.innerHTML = '<p>Нет созданных тестов.</p>';

    // Для красивого вывода имен тех, кому доступен тест
    const accounts = JSON.parse(localStorage.getItem('accounts_db'));

    tests.forEach(t => {
        let accessText = '(Доступно всем)';
        
        if (t.allowedUsers && t.allowedUsers.length > 0) {
            // Преобразуем массив логинов в массив "Имя Фамилия"
            const namesArray = t.allowedUsers.map(login => {
                const acc = accounts.find(a => a.username === login);
                return acc ? `${acc.firstName} ${acc.lastName}` : login;
            });
            accessText = `(Только для: ${namesArray.join(', ')})`;
        }

        list.innerHTML += `
            <div class="test-card">
                <div>
                    <b>${t.title}</b> <br>
                    <small>Вопросов: ${t.questions.length} | ${accessText}</small>
                </div>
                <div>
                    <button class="btn-stats" onclick="showStats(${t.id})">Статистика</button>
                </div>
            </div>
        `;
    });
}

// Переключение видимости списка студентов
function toggleStudentList() {
    const accessType = document.querySelector('input[name="access_type"]:checked').value;
    const list = document.getElementById('student-checkbox-list');
    if (accessType === 'selected') {
        list.classList.remove('hidden');
    } else {
        list.classList.add('hidden');
    }
}

function showCreateTestForm() {
    document.getElementById('create-test-form').classList.remove('hidden');
    document.getElementById('questions-container').innerHTML = '';
    document.getElementById('new-test-title').value = '';
    
    // Сброс выбора доступа на "Всем"
    document.querySelector('input[name="access_type"][value="all"]').checked = true;
    toggleStudentList();

    // Генерация чекбоксов со списком студентов
    const accounts = JSON.parse(localStorage.getItem('accounts_db')) || [];
    const students = accounts.filter(acc => acc.role === 'student');
    const listContainer = document.getElementById('student-checkbox-list');
    
    if (students.length === 0) {
        listContainer.innerHTML = '<p>Студентов пока нет в базе.</p>';
    } else {
        let html = '';
        students.forEach(st => {
            html += `<label><input type="checkbox" class="student-cb" value="${st.username}"> ${st.firstName} ${st.lastName} (@${st.username})</label>`;
        });
        listContainer.innerHTML = html;
    }

    questionCounter = 0; 
    addQuestionUI(); 
}

function hideCreateTestForm() {
    document.getElementById('create-test-form').classList.add('hidden');
}

function addQuestionUI() {
    questionCounter++;
    const container = document.getElementById('questions-container');
    const qId = `q_block_${questionCounter}`;
    
    container.innerHTML += `
        <div class="question-block" id="${qId}">
            <h4>Вопрос</h4>
            <input type="text" class="q-text" placeholder="Текст вопроса">
            <p>Варианты ответов:</p>
            <div class="options-list">
                <div class="option-block"><input type="radio" name="correct_${questionCounter}" value="0" checked> <input type="text" class="opt-text" placeholder="Вариант 1"></div>
                <div class="option-block"><input type="radio" name="correct_${questionCounter}" value="1"> <input type="text" class="opt-text" placeholder="Вариант 2"></div>
                <div class="option-block"><input type="radio" name="correct_${questionCounter}" value="2"> <input type="text" class="opt-text" placeholder="Вариант 3"></div>
                <div class="option-block"><input type="radio" name="correct_${questionCounter}" value="3"> <input type="text" class="opt-text" placeholder="Вариант 4"></div>
            </div>
        </div>
    `;
}

function saveTest() {
    const title = document.getElementById('new-test-title').value.trim();
    if (!title) return alert('Введите название теста!');

    // Обработка выбора доступа
    const accessType = document.querySelector('input[name="access_type"]:checked').value;
    let allowedUsersList = [];
    
    if (accessType === 'selected') {
        const checkboxes = document.querySelectorAll('.student-cb:checked');
        checkboxes.forEach(cb => {
            allowedUsersList.push(cb.value);
        });
        if (allowedUsersList.length === 0) {
            return alert('Вы выбрали "Выбрать из списка", но не отметили ни одного студента!');
        }
    } // Если accessType === 'all', массив остается пустым (доступно всем)

    const qBlocks = document.querySelectorAll('.question-block');
    let questions = [];

    for (let block of qBlocks) {
        const qText = block.querySelector('.q-text').value.trim();
        if (!qText) return alert('Не все вопросы заполнены!');

        let options = [];
        const optInputs = block.querySelectorAll('.opt-text');
        optInputs.forEach(input => {
            if(input.value.trim() !== '') options.push(input.value.trim());
        });

        if (options.length < 2) return alert('Каждый вопрос должен иметь минимум 2 заполненных варианта ответа!');

        const radios = block.querySelectorAll(`input[type="radio"]`);
        let correctIndex = 0;
        radios.forEach((r, idx) => { if (r.checked) correctIndex = idx; });
        
        if(correctIndex >= options.length) correctIndex = 0; 

        questions.push({ q: qText, options: options, correct: correctIndex });
    }

    const tests = JSON.parse(localStorage.getItem('tests_db'));
    tests.push({ 
        id: Date.now(), 
        title: title, 
        allowedUsers: allowedUsersList, 
        questions: questions 
    });
    
    localStorage.setItem('tests_db', JSON.stringify(tests));

    alert('Тест успешно сохранен!');
    hideCreateTestForm();
    renderTeacherDashboard();
}

function showStats(testId) {
    document.getElementById('stats-view').classList.remove('hidden');
    const results = JSON.parse(localStorage.getItem('results_db')).filter(r => r.testId === testId);
    
    let gradesCount = { "5": 0, "4": 0, "3": 0, "2": 0 };
    results.forEach(r => gradesCount[r.grade]++);

    const ctx = document.getElementById('statsChart').getContext('2d');
    
    if (chartInstance) chartInstance.destroy(); 

    if (results.length === 0) {
        document.getElementById('stats-title').innerText = "Статистика (Никто еще не прошел тест)";
        document.getElementById('stats-students-list').innerHTML = '';
        return;
    }

    document.getElementById('stats-title').innerText = `Статистика (Всего сдач: ${results.length})`;

    chartInstance = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['Оценка "5"', 'Оценка "4"', 'Оценка "3"', 'Оценка "2"'],
            datasets: [{
                data: [gradesCount["5"], gradesCount["4"], gradesCount["3"], gradesCount["2"]],
                backgroundColor: ['#28a745', '#17a2b8', '#ffc107', '#dc3545']
            }]
        }
    });

    const accounts = JSON.parse(localStorage.getItem('accounts_db'));
    let listHTML = '<h4>Результаты студентов:</h4><ul>';
    
    results.forEach(r => {
        const studentAcc = accounts.find(a => a.username === r.student);
        const fullName = studentAcc ? `${studentAcc.firstName} ${studentAcc.lastName}` : 'Неизвестно';
        listHTML += `<li><b>${fullName}</b> (@${r.student}) — Оценка: <b>${r.grade}</b> (${r.score}%)</li>`;
    });
    listHTML += '</ul>';
    
    document.getElementById('stats-students-list').innerHTML = listHTML;
}

// ==========================================
// ЛОГИКА СТУДЕНТА
// ==========================================
function renderStudentDashboard() {
    const tests = JSON.parse(localStorage.getItem('tests_db'));
    const results = JSON.parse(localStorage.getItem('results_db'));
    const list = document.getElementById('student-tests-list');
    list.innerHTML = '';

    let accessibleTestsCount = 0;

    tests.forEach(t => {
        if (t.allowedUsers && t.allowedUsers.length > 0) {
            if (!t.allowedUsers.includes(currentUser.username)) {
                return; 
            }
        }

        accessibleTestsCount++;
        const passed = results.find(r => r.testId === t.id && r.student === currentUser.username);
        let status = passed ? `<span class="status-passed">Сдано (Оценка: ${passed.grade})</span>` : '';
        
        list.innerHTML += `
            <div class="test-card">
                <div><b>${t.title}</b> <br> ${status}</div>
                <button onclick="startTest(${t.id})">${passed ? 'Пройти заново' : 'Пройти тест'}</button>
            </div>
        `;
    });

    if (accessibleTestsCount === 0) {
        list.innerHTML = '<p>Для вас пока нет доступных тестов.</p>';
    }
}

function startTest(testId) {
    document.getElementById('student-view').classList.add('hidden');
    document.getElementById('take-test-view').classList.remove('hidden');
    
    const tests = JSON.parse(localStorage.getItem('tests_db'));
    currentTestObj = tests.find(t => t.id === testId);
    
    document.getElementById('take-test-title').innerText = currentTestObj.title;
    const container = document.getElementById('take-test-container');
    container.innerHTML = '';

    currentTestObj.questions.forEach((q, qIndex) => {
        let optionsHTML = '';
        q.options.forEach((opt, optIndex) => {
            optionsHTML += `
                <label>
                    <input type="radio" name="student_q_${qIndex}" value="${optIndex}">
                    ${opt}
                </label>
            `;
        });

        container.innerHTML += `
            <div class="test-question">
                <h4>${qIndex + 1}. ${q.q}</h4>
                ${optionsHTML}
            </div>
        `;
    });
}

function cancelTest() {
    document.getElementById('take-test-view').classList.add('hidden');
    document.getElementById('student-view').classList.remove('hidden');
    currentTestObj = null;
}

function submitTest() {
    let correctAnswersCount = 0;
    const totalQuestions = currentTestObj.questions.length;

    for (let i = 0; i < totalQuestions; i++) {
        const selected = document.querySelector(`input[name="student_q_${i}"]:checked`);
        if (!selected) return alert(`Вы не ответили на вопрос №${i+1}`);
        
        if (parseInt(selected.value) === currentTestObj.questions[i].correct) {
            correctAnswersCount++;
        }
    }

    const percent = Math.round((correctAnswersCount / totalQuestions) * 100);
    let grade = 2;
    if (percent >= 90) grade = 5;
    else if (percent >= 70) grade = 4;
    else if (percent >= 50) grade = 3;

    const results = JSON.parse(localStorage.getItem('results_db'));
    
    const filteredResults = results.filter(r => !(r.testId === currentTestObj.id && r.student === currentUser.username));
    
    filteredResults.push({
        student: currentUser.username,
        testId: currentTestObj.id,
        score: percent,
        grade: grade
    });
    localStorage.setItem('results_db', JSON.stringify(filteredResults));

    alert(`Тест завершен!\nПравильных ответов: ${correctAnswersCount} из ${totalQuestions}\nВаша оценка: ${grade}`);
    
    cancelTest();
    renderStudentDashboard();
}
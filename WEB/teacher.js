let currentUser = null;
let qCount = 0;

document.addEventListener('DOMContentLoaded', async () => {
    currentUser = await checkAuth();
    if (currentUser.role !== 'teacher') window.location.href = 'student.html';
    document.getElementById('user-info').innerText = `Преподаватель: ${currentUser.firstname} ${currentUser.lastname}`;
    renderTests();
});

function renderTests() {
    const tests = JSON.parse(localStorage.getItem('tests_db'));
    const list = document.getElementById('tests-list');
    list.innerHTML = tests.length ? '' : '<p>Нет тестов</p>';

    tests.forEach(t => {
        list.innerHTML += `
            <div class="test-card">
                <div><b>${t.title}</b><br><small>Вопросов: ${t.questions.length}</small></div>
            </div>`;
    });
}

function showCreateForm() {
    document.getElementById('create-form').classList.remove('hidden');
    document.getElementById('questions-container').innerHTML = '';
    qCount = 0; addQuestion();
}

function addQuestion() {
    qCount++;
    document.getElementById('questions-container').innerHTML += `
        <div class="question-block" id="q_${qCount}">
            <input type="text" class="q-text" placeholder="Текст вопроса">
            <div><input type="radio" name="ans_${qCount}" value="0" checked> <input type="text" class="opt" placeholder="Вар 1"></div>
            <div><input type="radio" name="ans_${qCount}" value="1"> <input type="text" class="opt" placeholder="Вар 2"></div>
        </div>`;
}

function saveTest() {
    const title = document.getElementById('test-title').value;
    const allowed = document.getElementById('test-allowed').value.split(',').map(s=>s.trim()).filter(s=>s);
    const questions = [];
    
    document.querySelectorAll('.question-block').forEach(block => {
        const q = block.querySelector('.q-text').value;
        const options = Array.from(block.querySelectorAll('.opt')).map(inp => input.value).filter(v=>v);
        const correct = parseInt(block.querySelector('input[type="radio"]:checked').value);
        questions.push({ q, options, correct });
    });

    const tests = JSON.parse(localStorage.getItem('tests_db'));
    tests.push({ id: Date.now(), title, allowedUsers: allowed, questions });
    localStorage.setItem('tests_db', JSON.stringify(tests));

    document.getElementById('create-form').classList.add('hidden');
    renderTests();
}
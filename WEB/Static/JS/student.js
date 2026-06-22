let currentUser = null;

document.addEventListener('DOMContentLoaded', async () => {
    currentUser = await checkAuth();
    if (currentUser.role !== 'student') window.location.href = 'teacher.html';
    document.getElementById('user-info').innerText = `Студент: ${currentUser.firstname} ${currentUser.lastname}`;
    renderTests();
});

function renderTests() {
    const tests = JSON.parse(localStorage.getItem('tests_db'));
    const results = JSON.parse(localStorage.getItem('results_db'));
    const list = document.getElementById('tests-list');
    list.innerHTML = '';

    tests.forEach(t => {
        if (t.allowedUsers.length > 0 && !t.allowedUsers.includes(currentUser.user)) return; // C++ бэкенд возвращает логин как 'user'

        const result = results.find(r => r.testId === t.id && r.student === currentUser.user);
        const status = result ? `<span style="color:green">Сдано: ${result.grade}</span>` : '';

        list.innerHTML += `
            <div class="test-card">
                <div><b>${t.title}</b><br>${status}</div>
                <button style="width:auto" onclick="window.location.href='take_test.html?id=${t.id}'">Пройти</button>
            </div>`;
    });
}
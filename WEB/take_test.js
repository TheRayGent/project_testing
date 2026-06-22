let currentUser = null;
let currentTest = null;

document.addEventListener('DOMContentLoaded', async () => {
    currentUser = await checkAuth();
    
    // Получаем ID теста из URL (например: take_test.html?id=1680000000)
    const urlParams = new URLSearchParams(window.location.search);
    const testId = parseInt(urlParams.get('id'));

    const tests = JSON.parse(localStorage.getItem('tests_db'));
    currentTest = tests.find(t => t.id === testId);

    if (!currentTest) return alert('Тест не найден!');
    document.getElementById('test-title').innerText = currentTest.title;

    const container = document.getElementById('q-container');
    currentTest.questions.forEach((q, i) => {
        let optionsHTML = '';
        q.options.forEach((opt, optIndex) => {
            optionsHTML += `<label><input type="radio" name="q_${i}" value="${optIndex}"> ${opt}</label><br>`;
        });
        container.innerHTML += `<div class="question-block"><h4>${i+1}. ${q.q}</h4>${optionsHTML}</div>`;
    });
});

function finishTest() {
    let correct = 0;
    for (let i = 0; i < currentTest.questions.length; i++) {
        const selected = document.querySelector(`input[name="q_${i}"]:checked`);
        if (!selected) return alert('Ответьте на все вопросы!');
        if (parseInt(selected.value) === currentTest.questions[i].correct) correct++;
    }

    const score = (correct / currentTest.questions.length) * 100;
    let grade = score >= 90 ? 5 : score >= 70 ? 4 : score >= 50 ? 3 : 2;

    const results = JSON.parse(localStorage.getItem('results_db'));
    results.push({ testId: currentTest.id, student: currentUser.user, score, grade });
    localStorage.setItem('results_db', JSON.stringify(results));

    alert(`Оценка: ${grade}`);
    window.location.href = 'student.html';
}
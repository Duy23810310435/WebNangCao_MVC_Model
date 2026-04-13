document.addEventListener('DOMContentLoaded', updateCardCounts);

// ==========================================
// 1. XỬ LÝ KÉO THẢ (DRAG & DROP) VÀ AUTO-SCROLL
// ==========================================

function allowDrop(ev) {
    ev.preventDefault();
    ev.currentTarget.classList.add('drag-over');
}

function dragLeave(ev) {
    ev.currentTarget.classList.remove('drag-over');
}
let autoScrollInterval = null;
let currentMouseY = 0;

// 1. Chỉ dùng dragover để liên tục "nghe ngóng" vị trí Y của chuột
document.addEventListener('dragover', function (e) {
    currentMouseY = e.clientY;
});
function drag(ev) {
    ev.dataTransfer.setData("text", ev.target.id);
    ev.target.classList.add('dragging');
    ev.target.classList.add('no-anim');

    // Bắt đầu vòng lặp kiểm tra cuộn mượt (20ms / lần)
    const edgeSize = 120; // 120px từ mép màn hình là chuẩn nhất, 380px là bị chiếm nửa màn hình rồi
    const scrollSpeed = 15; // Tốc độ cuộn

    if (autoScrollInterval) clearInterval(autoScrollInterval);
    autoScrollInterval = setInterval(() => {
        if (currentMouseY < edgeSize) {
            window.scrollBy(0, -scrollSpeed); // Cuộn lên
        } else if (window.innerHeight - currentMouseY < edgeSize) {
            window.scrollBy(0, scrollSpeed); // Cuộn xuống
        }
    }, 20); // Chạy liên tục mỗi 20 mili-giây
}

document.addEventListener('dragend', function (ev) {
    if (ev.target.classList.contains('question-card')) {
        ev.target.classList.remove('dragging');
        updateCardCounts();
    }
    document.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));

    // Quan trọng: Tắt bộ đếm cuộn ngay khi nhả chuột
    if (autoScrollInterval) {
        clearInterval(autoScrollInterval);
        autoScrollInterval = null;
    }
});

function drop(ev) {
    ev.preventDefault();
    ev.currentTarget.classList.remove('drag-over');

    const data = ev.dataTransfer.getData("text");
    const draggedEl = document.getElementById(data);

    if (draggedEl && ev.currentTarget !== draggedEl.parentNode) {
        // Drop ngay lập tức không có delay
        const targetContainer = ev.currentTarget.id === 'col-waiting'
            ? document.getElementById('waiting-container')
            : ev.currentTarget;

        targetContainer.appendChild(draggedEl);
        updateCardCounts();
    }
}

// Tính năng mới: Tự động cuộn trang khi kéo thẻ sát mép màn hình
document.addEventListener('dragover', function (e) {
    const edgeSize = 50; // Khoảng cách (pixels) từ mép màn hình để kích hoạt cuộn
    const scrollSpeed = 15; // Tốc độ cuộn

    // Nếu chuột chạm gần mép trên
    if (e.clientY < edgeSize) {
        window.scrollBy(0, -scrollSpeed);
    }
    // Nếu chuột chạm gần mép dưới
    else if (window.innerHeight - e.clientY < edgeSize) {
        window.scrollBy(0, scrollSpeed);
    }
});

function updateCardCounts() {
    ['col-easy', 'col-medium', 'col-hard', 'col-waiting'].forEach(colId => {
        const col = document.getElementById(colId);
        if (col) {
            const count = col.querySelectorAll('.question-card').length;
            const badge = col.querySelector('.badge-count');
            if (badge) badge.innerText = count;
        }
    });
}

// ==========================================
// 2. THUẬT TOÁN PHÂN LOẠI TỰ ĐỘNG BẰNG DI TRUYỀN (GA) TRÊN FRONTEND
// ==========================================

function runAutoClassification() {
    const waitingContainer = document.getElementById('waiting-container');
    const cards = Array.from(waitingContainer.querySelectorAll('.question-card'));
    const totalQuestions = cards.length;

    if (totalQuestions === 0) {
        return Swal.fire({ icon: 'info', title: 'Thông báo', text: 'Không còn câu hỏi nào để phân loại!' });
    }

    // Đổi nút thành trạng thái "Đang suy nghĩ"
    Swal.fire({
        title: 'AI đang tiến hóa...',
        text: 'Vui lòng chờ thuật toán di truyền tìm phương án tối ưu!',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    // --- BƯỚC 1: LẤY ĐỘ PHỨC TẠP LÀM "ĐÁP ÁN CHUẨN" ---
    // Vì ở JS ta chỉ có data-length, ta sẽ dùng nó làm độ phức tạp thực tế
    const actualComplexities = cards.map(card => parseInt(card.getAttribute('data-length') || 0));

    // Dùng setTimeout để nhường luồng UI render SweetAlert trước khi GA chạy vòng lặp nặng
    setTimeout(() => {
        // --- CẤU HÌNH GA ---
        const POPULATION_SIZE = 50;
        const MAX_GENERATIONS = 100;
        const MUTATION_RATE = 0.05;

        // --- BƯỚC 2: KHỞI TẠO QUẦN THỂ BAN ĐẦU ---
        let population = Array.from({ length: POPULATION_SIZE }, () => ({
            genes: Array.from({ length: totalQuestions }, () => Math.floor(Math.random() * 3)), // 0: Dễ, 1: TB, 2: Khó
            fitnessScore: 0
        }));

        // Hàm tính Fitness (Định hướng tiến hóa)
        function calculateFitness(genes) {
            let score = 0;
            let counts = [0, 0, 0]; // [Số câu Dễ, Số câu TB, Số câu Khó]

            for (let i = 0; i < totalQuestions; i++) {
                counts[genes[i]]++;
                const length = actualComplexities[i];

                // Thưởng / Phạt theo logic ( <50 là Dễ, 50-120 là TB, >120 là Khó)
                if (genes[i] === 0 && length < 50) score += 10;
                else if (genes[i] === 1 && length >= 50 && length <= 120) score += 10;
                else if (genes[i] === 2 && length > 120) score += 10;
                else score -= 10;
            }

            // Hàm phạt cân bằng: 30% Dễ - 40% TB - 30% Khó
            const ideal = [totalQuestions * 0.3, totalQuestions * 0.4, totalQuestions * 0.3];
            // Nếu lệch 1 câu so với tỷ lệ lý tưởng, trừ thẳng 20 điểm (nặng hơn cả mức thưởng 10 điểm)
            score -= Math.abs(counts[0] - ideal[0]) * 20;
            score -= Math.abs(counts[1] - ideal[1]) * 20;
            score -= Math.abs(counts[2] - ideal[2]) * 20;

            return score;
        }

        // --- BƯỚC 3: VÒNG LẶP TIẾN HÓA ---
        for (let generation = 0; generation < MAX_GENERATIONS; generation++) {
            // Đánh giá Fitness
            population.forEach(p => p.fitnessScore = calculateFitness(p.genes));

            // Sắp xếp từ giỏi đến kém
            population.sort((a, b) => b.fitnessScore - a.fitnessScore);

            let newGeneration = [population[0], population[1]]; // Elitism: Giữ 2 cá thể top đầu

            // Lai ghép và Đột biến
            while (newGeneration.length < POPULATION_SIZE) {
                // Chọn ngẫu nhiên bố mẹ từ top 50%
                let father = population[Math.floor(Math.random() * (POPULATION_SIZE / 2))];
                let mother = population[Math.floor(Math.random() * (POPULATION_SIZE / 2))];

                let childGenes = [];
                let crossoverPoint = Math.floor(Math.random() * totalQuestions);

                for (let i = 0; i < totalQuestions; i++) {
                    childGenes.push(i < crossoverPoint ? father.genes[i] : mother.genes[i]);

                    // Đột biến
                    if (Math.random() < MUTATION_RATE) {
                        childGenes[i] = Math.floor(Math.random() * 3);
                    }
                }
                newGeneration.push({ genes: childGenes, fitnessScore: 0 });
            }
            population = newGeneration;
        }

        // --- BƯỚC 4: LẤY CÁ THỂ XUẤT SẮC NHẤT ---
        population.forEach(p => p.fitnessScore = calculateFitness(p.genes));
        population.sort((a, b) => b.fitnessScore - a.fitnessScore);
        const bestGenes = population[0].genes;
        // ========================================================
        // TẠO ĐỘ TRỄ GIẢ LẬP AI ĐANG "SUY NGHĨ" (2 GIÂY)
        // ========================================================
        setTimeout(() => {
            Swal.close(); // Tắt popup loading sau 2 giây diễn kịch

            // --- BƯỚC 5: ANIMATION DI CHUYỂN THẺ ---
            let i = 0;
            const interval = setInterval(() => {
                if (i >= totalQuestions) {
                    clearInterval(interval);
                    return;
                }

                const card = cards[i];
                card.classList.add('no-anim');

                if (bestGenes[i] === 0) {
                    document.getElementById('col-easy').appendChild(card);
                } else if (bestGenes[i] === 1) {
                    document.getElementById('col-medium').appendChild(card);
                } else {
                    document.getElementById('col-hard').appendChild(card);
                }

                updateCardCounts();
                i++;
            }, 250);//mỗi thẻ câu hỏi hiện lên mỗi 250ms để tạo hiệu ứng "AI đang xếp từng thẻ một"

        }, 2000); // 2000 mili-giây = 2 giây chờ đợi

    }, 100);
}

/// ==========================================
// 3. API LƯU DỮ LIỆU & HIỂN THỊ MODAL VÀO THI
// ==========================================
function saveClassification() {
    const dataToSave = [];

    ['col-easy', 'col-medium', 'col-hard'].forEach(colId => {
        const col = document.getElementById(colId);
        if (!col) return;

        const difficultyValue = col.getAttribute('data-difficulty');

        col.querySelectorAll('.question-card').forEach(card => {
            dataToSave.push({
                QuestionId: parseInt(card.getAttribute('data-question-id') || 0),
                Difficulty: difficultyValue
            });
        });
    });

    if (!dataToSave.length) {
        return Swal.fire({ icon: 'warning', title: 'Khoan đã!', text: 'Bạn chưa phân loại câu hỏi nào vào các cột.' });
    }

    const btn = document.getElementById('btn-save');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = "<i class='fas fa-spinner fa-spin'></i> Đang lưu Database...";

    // 1. GỌI API LƯU VÀO POSTGRESQL
    fetch('/Student/SaveClassification', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dataToSave)
    })
        .then(res => res.json())
        .then(resData => {
            if (resData.success) {

                // 2. HIỂN THỊ MODAL CẤU HÌNH THI (Sau khi lưu DB thành công)
                Swal.fire({
                    title: 'Lưu Hệ Thống Thành Công!',
                    html: `
                    <p class="text-muted mb-4">Các câu hỏi đã được cập nhật độ khó. Bạn có muốn cấu hình để vào thi luôn không?</p>
                    <div style="text-align: left; background: #f8fafc; padding: 15px; border-radius: 8px;">
                        <div class="form-check mb-2">
                            <input class="form-check-input" type="checkbox" id="chk-shuffle-questions" checked>
                            <label class="form-check-label" for="chk-shuffle-questions">1. Đảo vị trí câu hỏi</label>
                        </div>
                        <div class="form-check mb-2">
                            <input class="form-check-input" type="checkbox" id="chk-shuffle-answers" checked>
                            <label class="form-check-label" for="chk-shuffle-answers">2. Đảo vị trí đáp án</label>
                        </div>
                        <div class="form-check">
                            <input class="form-check-input" type="checkbox" id="chk-anti-cheat">
                            <label class="form-check-label text-danger" for="chk-anti-cheat">3. Bật chế độ giám sát gian lận (Chống chuyển tab/copy)</label>
                        </div>
                    </div>
                `,
                    icon: 'success',
                    showCancelButton: true,
                    confirmButtonText: '<i class="fas fa-play"></i> Bắt đầu thi ngay',
                    cancelButtonText: 'Để sau',
                    confirmButtonColor: '#22c55e',
                    cancelButtonColor: '#64748b'
                }).then((result) => {
                    // 1. Lấy examId từ thanh địa chỉ URL hiện tại (VD: ?examId=5)
                    const urlParams = new URLSearchParams(window.location.search);
                    const examId = currentExamId;

                    if (result.isConfirmed) {
                        // Lấy các tùy chọn người dùng đã chọn
                        const shuffleQ = document.getElementById('chk-shuffle-questions').checked;
                        const shuffleA = document.getElementById('chk-shuffle-answers').checked;
                        const antiCheat = document.getElementById('chk-anti-cheat').checked;

                        // Ghép thêm tham số examId=${examId} vào URL để Controller biết đang thi bài nào
                        window.location.href = `/TestAttempt/CreateTestSession?examId=${examId}&shuffleQ=${shuffleQ}&shuffleA=${shuffleA}&antiCheat=${antiCheat}`;
                    } else {
                        // NÚT "ĐỂ SAU" - chưa bắt đầu thi vội: Điều hướng thẳng về trang chủ Dashboard của học sinh
                        //tại trang chủ, trong các trang bài thi sẽ luôn mở cho bài kiểm tra cá nhân của người đó
                        window.location.href = '/Student/Dashboard';
                    }
                });

            } else {
                Swal.fire({ icon: 'error', title: 'Lỗi PostgreSQL!', text: resData.message });
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        })
        .catch(error => {
            Swal.fire({ icon: 'error', title: 'Lỗi kết nối!', text: 'Không thể vươn tới máy chủ.' });
            btn.disabled = false;
            btn.innerHTML = originalText;
        });
}
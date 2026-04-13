//
// 1. KHỞI TẠO ICON 

// 
lucide.createIcons();

//
// 2. BỘ ĐẾM NGƯỢC THỜI GIAN
//
//Chống gian lận reload f5 để reset thời gian bằng cách lưu Local Storage
//
// Lấy biến TIME_LIMIT_MINUTES từ thẻ <script> trong HTML, nếu không có thì mặc định là 60
const limitMinutes = typeof TIME_LIMIT_MINUTES !== 'undefined' ? TIME_LIMIT_MINUTES : 60;
const totalTime = limitMinutes * 60;
let timeRemaining = totalTime;


const countdownDisplay = document.getElementById('countdownDisplay');
const timerBadge = countdownDisplay?.parentElement;

function startTimer() {
    if (!countdownDisplay) return;
    //Lấy ID bài thi để tạo KEY lưu trữ của chính bài thi đang làm
    // examId: lấy phần tử cuối cùng của mảng; nếu không thấy số sau "TestAttempt\ " --> lấy ID 1
    // Kiểm tra số Id phải là KDL int, nếu không thì --> lấy Id 1 làm mặc định
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");

    //KEY lưu trong bộ nhớ Local_Storage, đặt tên là: "eduTest_deadline_xyz"
    const storageKey = `eduTest_deadline_${examId}`;//gắn Id bài thi cho dễ phân biệt

    //lấy deadline từ bộ nhớ local_Storage lên khi người dùng thoát bài giữa lúc làm
    // deadlineTime = thời điểm tương lai khi bài thi kết thúc
    // ví dụ: Date.now() = 8:30 = xxxxx (mili-giây)
    // -> deadlineTime = xxxxx (mili-giây) + thời gian thi (dạng mili-giây)
    let savedDeadline = localStorage.getItem(storageKey);
    let deadlineTime;
    if (!savedDeadline) {
        //Nếu chưa có Deadline (Vào thi lần đầu tiên) --> tạo Deadline mới
        //Date.now() trả về mili-giây (thay vì cho ra đơn vị giờ, phút, giây thì sẽ trả về mili-giây)
        deadlineTime = Date.now() + (totalTime * 1000);
        localStorage.setItem(storageKey, deadlineTime); //lưu vào Local Storage
    }
    else {
        //nếu đã có Deadline (Do f5 hoặc mở lại sau khi đã thoát bài)
        //ép kiểu chuỗi lưu trong bộ nhớ về số nguyên int
        // localStorage: chỉ lưu KDL string
        deadlineTime = parseInt(savedDeadline);
    }

    //vòng lặp đếm ngược thời gian
    const timerInterval = setInterval(function () {
        //tính toán lại số giây còn lại dựa vào thời gian thực tế
        let timeRemaining = Math.floor((deadlineTime - Date.now()) / 1000);

        // Nếu bỏ thi quá thời gian thi kể từ lúc vào làm bài --> Nộp hết giờ
        if (timeRemaining < 0) {
            timeRemaining = 0; // về sau khi hết giờ/ đã nộp thì tự động xóa deadline cho lần thi sau
        }

        //xử lý hiển thị phút giây (Ví dụ mô phòng: 08:00 - 8 phút 0 giây)
        let minutes = Math.floor(timeRemaining / 60);
        let seconds = timeRemaining % 60;

        let formattedMinutes = minutes < 10 ? "0" + minutes : minutes;
        let formattedSeconds = seconds < 10 ? "0" + seconds : seconds;

        countdownDisplay.textContent = formattedMinutes + ":" + formattedSeconds;

        // Tính phần trăm thời gian còn lại
        let percentRemaining = (timeRemaining / totalTime) * 100;

        // Đổi màu linh hoạt theo từng mốc %
        if (percentRemaining <= 10 && timeRemaining > 0) {
            timerBadge.style.backgroundColor = '#fef2f2';
            timerBadge.style.color = '#ef4444';
            timerBadge.style.borderColor = '#fecaca';
        }
        else if (percentRemaining <= 30) {
            timerBadge.style.backgroundColor = '#fff7ed';
            timerBadge.style.color = '#ea580c';
            timerBadge.style.borderColor = '#fed7aa';
        }
        else if (percentRemaining <= 50) {
            timerBadge.style.backgroundColor = '#fefce8';
            timerBadge.style.color = '#ca8a04';
            timerBadge.style.borderColor = '#fef08a';
        }

        // Xử lý khi HẾT GIỜ
        if (timeRemaining <= 0) {
            clearInterval(timerInterval);
            countdownDisplay.textContent = "00:00";

            //Xóa Deadline trong bộ nhớ để lần sau thi lại đề này (nếu cho phép làm lại)
            localStorage.removeItem(storageKey);
            alert("Đã hết thời gian làm bài! Hệ thống sẽ tự động lưu và nộp bài của bạn.");

            // Gọi hàm nộp bài và truyền tham số true (báo hiệu là nộp tự động, không cần confirm)
            submitExam(true);
        }
        //else {
        //    timeRemaining--;
        //}
    }, 1000);
}

// 
// 3. LOGIC CHỌN ĐÁP ÁN & THANH TIẾN ĐỘ
// 

function initQuestionLogic() {
    const allRadios = document.querySelectorAll('.answer-option input[type="radio"]');
    const gridItems = document.querySelectorAll('.grid-item');
    const totalQuestions = gridItems.length;

    const progressFill = document.querySelector('.progress-fill');
    const progressText = document.querySelector('.progress-header span:first-child');
    const percentText = document.querySelector('.progress-header .percent');
    const answeredLegendCount = document.querySelectorAll('.legend-count')[0];
    const unansweredLegendCount = document.querySelectorAll('.legend-count')[1];

    

    // examId: lấy phần tử cuối cùng của mảng; nếu không thấy số sau "TestAttempt\ " --> lấy ID 1
    // Kiểm tra số Id phải là KDL int, nếu không thì --> lấy Id 1 làm mặc định
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");

    //KEY lưu trong bộ nhớ Local_Storage, đặt tên là: "eduTest_deadline_xyz"
    const draftKey = `eduTest_draft_${examId}`;//gắn Id bài thi cho dễ phân biệt
    allRadios.forEach(radio => {
        radio.addEventListener('change', function () {
            // lưu đáp án vào Local Storage
            //lấy object draft hiện tại ra, nếu không có thì tạo mới {}
            let draftAnswers = {};
            try {
                const rawData = localStorage.getItem(draftKey);
                draftAnswers = rawData ? JSON.parse(rawData) : {};
            } catch (error) {
                console.warn("Dữ liệu draft bị lỗi định dạng, tiến hành reset.");
                draftAnswers = {};
            }
            //Gắn giá trị: draftAswers["question_1" = "2","question_2" = "3",...]
            //Nếu dùng phím tắt '0' thì dùng lệnh Delete
            draftAnswers[this.name] = this.value;
            //áp kiểu String do Local Storage chỉ lưu string, sau này lấy ra sẽ ép KDL khác
            localStorage.setItem(draftKey, JSON.stringify(draftAnswers));

            // Lấy ID của khối câu hỏi chứa radio này (VD: question-block-1)
            const block = this.closest('.question-block');
            const blockId = block.id;
            const qIndex = parseInt(blockId.replace('question-block-', ''));
            const gridIndex = qIndex - 1;

            // Cập nhật màu ô vuông bên Sidebar
            if (gridItems[gridIndex]) {
                gridItems[gridIndex].classList.add('answered');
            }

            // Đếm số câu đã làm (dựa trên số lượng ô đã đổi class 'answered')
            const answeredCount = document.querySelectorAll('.grid-item.answered').length;

            // Cập nhật giao diện
            progressText.textContent = `Tiến độ: ${answeredCount}/${totalQuestions} câu`;
            const percent = Math.round((answeredCount / totalQuestions) * 100);
            percentText.textContent = `${percent}%`;
            progressFill.style.width = `${percent}%`;

            answeredLegendCount.textContent = answeredCount;
            unansweredLegendCount.textContent = totalQuestions - answeredCount;

            refreshSidebarFilter();//refresh lại bộ lọc - tránh lỗi logic khi sử dụng dữ liệu lưu trữ từ LocalStorage
        });
    });
}

// 
// 4. LOGIC ĐÁNH DẤU CÂU HỎI (FLAG) VÀ LƯU LOCAL STORAGE
// 

function initFlagLogic() {
    const flagButtons = document.querySelectorAll('.btn-flag');
    const gridItems = document.querySelectorAll('.grid-item');
    const flaggedLegendCount = document.querySelectorAll('.legend-count')[2];

    // Tạo KEY lưu trữ cờ dựa trên ID bài thi
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");
    const flagKey = `eduTest_flagged_${examId}`;

    flagButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            if (e) e.preventDefault(); // Chống reload trang

            this.classList.toggle('active-flag');

            const questionNumText = this.closest('.card').querySelector('.question-number').textContent;
            const gridIndex = parseInt(questionNumText) - 1;

            // Đổi màu ô trên Sidebar
            if (gridItems[gridIndex]) {
                gridItems[gridIndex].classList.toggle('flagged');
            }

            // XỬ LÝ LƯU VÀO LOCAL STORAGE
            // Lấy danh sách cờ hiện tại ra (nếu không có thì tạo mảng rỗng)
            let flaggedArray = JSON.parse(localStorage.getItem(flagKey)) || [];

            if (this.classList.contains('active-flag')) {
                // Nếu đang BẬT cờ và câu này chưa có trong mảng -> Thêm vào
                if (!flaggedArray.includes(gridIndex)) {
                    flaggedArray.push(gridIndex);
                }
            } else {
                // Nếu đang TẮT cờ -> Lọc bỏ câu này ra khỏi mảng
                flaggedArray = flaggedArray.filter(item => item !== gridIndex);
            }

            // Ép kiểu mảng thành chuỗi JSON và lưu lại
            localStorage.setItem(flagKey, JSON.stringify(flaggedArray));

            // CẬP NHẬT GIAO DIỆN
            const totalFlagged = document.querySelectorAll('.grid-item.flagged').length;
            if (flaggedLegendCount) flaggedLegendCount.textContent = totalFlagged;

            // refresh lại bộ lọc - tránh lỗi logic với câu hỏi lưu trữ trên LocalStorage
            refreshSidebarFilter();
        });
    });
}

// 
// 5. LOGIC ĐIỀU HƯỚNG CÂU HỎI (NAVIGATION)
// 
//chỉ hiển thị 1 câu hỏi cùng 1 lúc, chỉ chuyển câu hỏi khác khi ấn "Next"

let currentQuestionIndex = 1;

function showQuestion(index) {
    const questions = document.querySelectorAll('.question-block');
    const totalQs = questions.length;

    questions.forEach(q => q.style.display = 'none');

    const targetQuestion = document.getElementById(`question-block-${index}`);
    if (targetQuestion) {
        targetQuestion.style.display = 'block';
    }

    document.querySelectorAll('.grid-item').forEach(item => {
        item.classList.remove('active');
    });
    const activeGridItem = document.querySelector(`.grid-item[onclick="jumpToQuestion(${index})"]`);
    if (activeGridItem) {
        activeGridItem.classList.add('active');
    }

    updateNavButtons(index, totalQs);
    currentQuestionIndex = index;
}

function updateNavButtons(index, totalQs) {
    const currentBlock = document.getElementById(`question-block-${index}`);
    if (!currentBlock) return;

    const btnPrev = currentBlock.querySelector('.btn-nav-prev');
    const btnNext = currentBlock.querySelector('.btn-nav-next');

    if (index === 1) {
        btnPrev.style.visibility = 'hidden';
    } else {
        btnPrev.style.visibility = 'visible';
    }

    // Nếu là câu cuối thì đổi chữ "Câu sau" thành "Nộp bài"
    if (index === totalQs) {
        btnNext.innerHTML = '<span>Nộp bài</span> <i data-lucide="send" width="18" height="18"></i>';
        btnNext.classList.add('btn-finish');
        btnNext.onclick = function () { submitExam(false); }; // Nộp bằng tay
    } else {
        btnNext.innerHTML = 'Câu sau <i data-lucide="chevron-right" width="18" height="18"></i>';
        btnNext.classList.remove('btn-finish');
        btnNext.onclick = nextQuestion;
    }

    lucide.createIcons();
}

function nextQuestion() {
    const totalQs = document.querySelectorAll('.question-block').length;
    if (currentQuestionIndex < totalQs) {
        showQuestion(currentQuestionIndex + 1);
    }
}

function prevQuestion() {
    if (currentQuestionIndex > 1) {
        showQuestion(currentQuestionIndex - 1);
    }
}

function jumpToQuestion(index) {
    showQuestion(index);
}

// 
// 6. LOGIC NỘP BÀI (SUBMIT EXAM VỚI MODAL)
// 

let pendingAnswers = []; // Biến mảng tạm để lưu đáp án chờ nộp

function submitExam(isAutoSubmit = false) {
    pendingAnswers = [];
    const questionBlocks = document.querySelectorAll('.question-block');
    const totalQs = questionBlocks.length;
    let answeredCount = 0;
    let flaggedCount = document.querySelectorAll('.grid-item.flagged').length;

    // 1. Thu thập dữ liệu
    questionBlocks.forEach(block => {
        const radio = block.querySelector('input[type="radio"]:checked');
        const firstRadio = block.querySelector('input[type="radio"]');
        if (firstRadio) {
            const questionId = firstRadio.name.replace('question_', '');
            pendingAnswers.push({
                QuestionId: parseInt(questionId),
                SelectedAnswerId: radio ? parseInt(radio.value) : 0
            });
            if (radio) answeredCount++;
        }
    });

    // Nếu hết giờ (isAutoSubmit = true), bỏ qua Modal và nộp thẳng luôn
    if (isAutoSubmit) {
        executeSubmit();
        return;
    }

    // 2. Tính toán hiển thị lên Modal
    const unansweredCount = totalQs - answeredCount;

    document.getElementById('modal-answered').textContent = `${answeredCount}/${totalQs} câu`;
    document.getElementById('modal-unanswered').textContent = `${unansweredCount} câu`;
    document.getElementById('modal-flagged').textContent = `${flaggedCount} câu`;

    const warningBox = document.getElementById('modal-warning-box');
    const warningText = document.getElementById('modal-warning-text');

    // Nếu còn câu chưa làm thì hiện cảnh báo cam, nếu làm full rồi thì ẩn đi cho đẹp
    if (unansweredCount > 0) {
        warningBox.style.display = 'flex';
        warningText.textContent = `Bạn còn ${unansweredCount} câu chưa trả lời. Bài làm sẽ không thể chỉnh sửa sau khi nộp.`;
    } else {
        warningBox.style.display = 'none';
    }

    // 3. Hiển thị Modal
    document.getElementById('submitModal').style.display = 'flex';
    lucide.createIcons(); // Load icon cho Modal
}

function closeSubmitModal() {
    document.getElementById('submitModal').style.display = 'none';
}
// 
// BIẾN LƯU TRỮ ID KẾT QUẢ, SAU NÀY XEM LẠI KẾT QUẢ
// 
let currentResultId = 0;

// 
// HÀM NỘP BÀI LÊN SERVER (AJAX)
// 
async function executeSubmit() {
    closeSubmitModal();

    // Lấy ID bài thi từ URL
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");

    // Xóa Local Storage (* Cập nhật: chuyển Xóa Local Storage khi điều kiện chấm xong bài thi là TRUE)
    //localStorage.removeItem(`eduTest_deadline_${examId}`);
    //localStorage.removeItem(`eduTest_violations_${examId}`);
    //localStorage.removeItem(`eduTest_draft_${examId}`);

    const mainSubmitBtn = document.querySelector('.btn-submit');
    if (mainSubmitBtn) {
        mainSubmitBtn.disabled = true;
        mainSubmitBtn.innerHTML = '<span>Đang nộp...</span>';
    }

    // GOM ĐÁP ÁN TRỰC TIẾP TỪ GIAO DIỆN 
    let finalAnswers = [];
    // Tìm tất cả các input radio đang ở trạng thái "checked" (đã được chọn)
    const checkedRadios = document.querySelectorAll('input[type="radio"]:checked');

    checkedRadios.forEach(radio => {
        // Tên radio đang có dạng "question_12", ta cắt bỏ chữ "question_" để lấy ID (12)
        const qId = radio.name.replace('question_', '');
        const aId = radio.value;

        finalAnswers.push({
            QuestionId: parseInt(qId),
            SelectedAnswerId: parseInt(aId)
        });
    });

    try {
        const response = await fetch('/TestAttempt/SubmitExam', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({
                ExamId: examId, // Đảm bảo dùng biến examId
                UserAnswers: finalAnswers
            })
        });

        const result = await response.json();

        if (result.success) {
            //  CHỈ XÓA LOCAL STORAGE KHI SERVER ĐÃ NHẬN BÀI THÀNH CÔNG 
            localStorage.removeItem(`eduTest_deadline_${examId}`);
            localStorage.removeItem(`eduTest_violations_${examId}`);
            localStorage.removeItem(`eduTest_draft_${examId}`);
            localStorage.removeItem(`eduTest_flagged_${examId}`);
            // 1. Gán dữ liệu hiển thị lên Modal Kết quả
            document.getElementById('result-score').textContent = result.score;
            document.getElementById('result-correct').textContent = `${result.correctCount} / ${result.totalQuestions}`;
            document.getElementById('result-easy').textContent = `${result.correctEasy} câu`;
            document.getElementById('result-medium').textContent = `${result.correctMedium} câu`;
            document.getElementById('result-hard').textContent = `${result.correctHard} câu`;

            // 2. GÁN ID KẾT QUẢ VÀO BIẾN TOÀN CỤC
            currentResultId = result.attemptId || result.resultId || result.AttemptId || result.ResultId || result.id || result.Id || 0;

            // 3. Hiển thị Modal Kết quả
            document.getElementById('resultModal').style.display = 'flex';
            lucide.createIcons();

        } else {
            alert("Có lỗi xảy ra: " + result.message);
            if (mainSubmitBtn) resetSubmitBtn(mainSubmitBtn);
        }
    } catch (error) {
        console.error("Lỗi khi nộp bài:", error);
        alert("Không thể gửi dữ liệu nộp bài. Vui lòng kiểm tra kết nối mạng!");
        if (mainSubmitBtn) resetSubmitBtn(mainSubmitBtn);
        // Vì nộp không thành công qua mạng, Local Storage vẫn an toàn
    }
}

// 
// HÀM XỬ LÝ KHI BẤM NÚT "XEM CHI TIẾT ĐÁP ÁN VÀ ĐIỂM"
// 
function goToResultPage() {
    if (currentResultId > 0) {
        // Có ID -> Chuyển hướng sang trang xem chi tiết
        window.location.href = '/TestAttempt/ReviewResult?resultId=' + currentResultId;
    } else {
        // Lỗi không có ID -> Bắt buộc quay về Dashboard
        alert("Không tìm thấy dữ liệu bài thi. Đang quay về Dashboard!");
        window.location.href = '/Student/Dashboard';
    }
}
// Hàm phụ để reset lại nút nộp bài khi gặp lỗi mạng
function resetSubmitBtn(btn) {
    btn.disabled = false;
    btn.innerHTML = '<i data-lucide="send" width="16" height="16"></i><span>Nộp bài</span>';
    lucide.createIcons();
}

// Hàm tự động refresh lại bộ lọc Sidebar
function refreshSidebarFilter() {
    const activeTab = document.querySelector('.filter-tab.active');
    if (activeTab) {
        activeTab.click(); // Giả lập hành động click để chạy lại logic lọc
    }
}
// 
// 7. BỘ LỌC TABS: TẤT CẢ / ĐÃ LÀM / ĐÁNH DẤU
// 

function initFilterLogic() {
    const tabs = document.querySelectorAll(".filter-tab");
    const gridItems = document.querySelectorAll('.grid-item');

    tabs.forEach(tab => {
        tab.addEventListener("click", function () {
            // 1. Xóa class "active" ở tất cả các tab và thêm vào tab đang được click
            tabs.forEach(t => t.classList.remove("active"));
            //active cho Tab đang được Click
            this.classList.add("active");

            // 2. Lấy tên của tab để làm điều kiện lọc, tên tab chính là TEXT bên trong thẻ HTML (VD: "Đã làm", "Đánh dấu", "Tất cả")
            const filterType = this.textContent.trim();

            // 3. Lặp qua tất cả các ô số câu hỏi để ẩn/hiện
            gridItems.forEach(item => {
                // Reset về hiển thị mặc định trước
                item.style.display = "";

                if (filterType === "Đã làm") {
                    // Nếu tab là "Đã làm", chỉ hiện những câu có class 'answered'
                    if (!item.classList.contains("answered")) {
                        item.style.display = "none";
                    }
                }
                else if (filterType === "Đánh dấu") {
                    // Nếu tab là "Đánh dấu", chỉ hiện những câu có class 'flagged'
                    if (!item.classList.contains("flagged")) {
                        item.style.display = "none";
                    }
                }
                // Nếu là tab "Tất cả", thì mọi item.style.display = "" đã làm nó hiện lên hết rồi
            });
        });
    });
}
// 
// 8. LOGIC PHÍM TẮT (KEYBOARD SHORTCUTS)
// 
document.addEventListener('keydown', function (event) {
    // Bỏ qua nếu người dùng đang mở Modal (ví dụ modal Xác nhận nộp bài)
    // Để tránh việc lỡ tay bấm phím chuyển câu hay đổi đáp án khi đang xem popup
    const submitModal = document.getElementById('submitModal');
    const resultModal = document.getElementById('resultModal');
    if ((submitModal && submitModal.style.display === 'flex') ||
        (resultModal && resultModal.style.display === 'flex')) {

        // Phím Esc: Đóng modal nộp bài
        if (event.key === 'Escape' && submitModal.style.display === 'flex') {
            closeSubmitModal();
        }
        return;
    }

    // Lấy Block câu hỏi đang hiển thị hiện tại
    const currentBlock = document.getElementById(`question-block-${currentQuestionIndex}`);
    if (!currentBlock) return;

    // 1. Phím Mũi tên: Chuyển câu hỏi
    if (event.key === 'ArrowLeft') {
        prevQuestion();
    }
    else if (event.key === 'ArrowRight') {
        nextQuestion();
    }
    // 2. Phím 'f' hoặc 'F': Cắm cờ (Đánh dấu)
    else if (event.key.toLowerCase() === 'f') {
        const flagBtn = currentBlock.querySelector('.btn-flag');
        if (flagBtn) {
            flagBtn.click(); // Giả lập hành động click vào nút cờ

            //HIỆU ỨNG PHẢN HỒI XÚC GIÁC CHO NÚT CỜ
            // Xoá class cũ đi (đề phòng bấm liên tục)
            flagBtn.classList.remove('flash-flag-effect');

            // Trigger reflow để reset lại animation của CSS
            void flagBtn.offsetWidth;

            // Thêm class hiệu ứng vào
            flagBtn.classList.add('flash-flag-effect');

            // Tự động gỡ class sau 400ms
            setTimeout(() => {
                flagBtn.classList.remove('flash-flag-effect');
            }, 400);
        }
    }
    // 3. Phím số 1, 2, 3, 4, 5...: Chọn đáp án tương ứng
    else if (event.key >= '1' && event.key <= '9') {
        const answerIndex = parseInt(event.key) - 1;
        const radios = currentBlock.querySelectorAll('.answer-option input[type="radio"]');

        if (radios && radios.length > answerIndex) {
            radios[answerIndex].checked = true;
            radios[answerIndex].dispatchEvent(new Event('change'));

            // HIỆU ỨNG PHẢN HỒI XÚC GIÁC (VISUAL FEEDBACK) ---
            const labelElement = radios[answerIndex].closest('.answer-option');
            if (labelElement) {
                // Xoá class cũ đi (đề phòng người dùng bấm liên tục 2 lần)
                labelElement.classList.remove('flash-effect');

                // Trigger reflow để reset lại animation của CSS
                void labelElement.offsetWidth;

                // Thêm class hiệu ứng vào
                labelElement.classList.add('flash-effect');

                // Tự động gỡ class chớp nhoáng khi nhấn phím tắt sau 400ms (vừa đúng lúc animation chạy xong)
                setTimeout(() => {
                    labelElement.classList.remove('flash-effect');
                }, 400);
            }
        }
    }
        // 4. Phím '0': Xóa sạch lựa chọn của câu hỏi hiện tại
        //bỏ dấu chấm tròn trên thẻ radio
        //rút lại thanh màu xanh tiến độ,
        //trừ đi số câu đã làm, 
        //cộng lại số câu chưa làm,
        //và xoá luôn màu ở cái ô số tương ứng bên thanh Sidebar
    else if (event.key === '0') {
        // Tìm radio đang được chọn trong câu hiện tại
        const checkedRadio = currentBlock.querySelector('.answer-option input[type="radio"]:checked');

        if (checkedRadio) {
            checkedRadio.checked = false; // Hủy check radio button

            //xóa đáp án khỏi Local Storage khi nhấn phím tắt "0"
            const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");
            const draftKey = `eduTest_draft_${examId}`;
            let draftAnswers = {};
            try {
                const rawData = localStorage.getItem(draftKey);
                draftAnswers = rawData ? JSON.parse(rawData) : {};
            } catch (error) {
                console.warn("Dữ liệu draft bị lỗi định dạng, tiến hành reset.");
                draftAnswers = {};
            }

            //xóa phần tử khỏi Object (ví dụ xóa thuộc tính "question_1": "2")
            delete draftAnswers[checkedRadio.name];
            localStorage.setItem(draftKey, JSON.stringify(draftAnswers));

            // Xóa hiệu ứng màu nền của đáp án (nếu có)
            const labelElement = checkedRadio.closest('.answer-option');
            if (labelElement) {
                labelElement.classList.remove('flash-effect');
            }

            // --- ĐỒNG BỘ LẠI VỚI BẢNG SIDEBAR ---
            const gridIndex = currentQuestionIndex - 1;
            const gridItems = document.querySelectorAll('.grid-item');

            if (gridItems[gridIndex]) {
                gridItems[gridIndex].classList.remove('answered'); // Xóa màu xanh ở ô bên phải
            }

            // --- TÍNH TOÁN LẠI THANH TIẾN ĐỘ ---
            const totalQuestions = gridItems.length;
            const answeredCount = document.querySelectorAll('.grid-item.answered').length;

            const progressFill = document.querySelector('.progress-fill');
            const progressText = document.querySelector('.progress-header span:first-child');
            const percentText = document.querySelector('.progress-header .percent');
            const answeredLegendCount = document.querySelectorAll('.legend-count')[0];
            const unansweredLegendCount = document.querySelectorAll('.legend-count')[1];

            progressText.textContent = `Tiến độ: ${answeredCount}/${totalQuestions} câu`;
            const percent = Math.round((answeredCount / totalQuestions) * 100);
            percentText.textContent = `${percent}%`;
            progressFill.style.width = `${percent}%`;

            answeredLegendCount.textContent = answeredCount;
            unansweredLegendCount.textContent = totalQuestions - answeredCount;

            //refresh lại bộ lọc - tránh lỗi logic khi lấy dữ liệu lưu trữ từ Local Storage
            refreshSidebarFilter();
        }
    }
    // 5. Phím Enter: Mở nhanh popup Nộp bài
    else if (event.key === 'Enter') {
        submitExam(false);
    }
});

//
// 9. CẢNH BÁO GIAN LẬN (CHỐNG CHUYỂN TAB)
//
function initAntiCheat() {
    //1. lấy ID bài thi để tạo KEY lưu trữ (tránh nhầm bài thi)
    // examId: lấy phần tử cuối cùng của mảng; nếu không thấy số sau "TestAttempt\ " --> lấy ID 1
    // Kiểm tra số Id phải là KDL int, nếu không thì --> lấy Id 1 làm mặc định
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");
    
    //KEY lưu trong bộ nhớ Local_Storage, đặt tên là: "eduTest_violations_xyz"
    const violationKey = `eduTest_violations_${examId}`;//gắn Id bài thi cho dễ phân biệt

    //2. lấy số lần vi phạm vào bộ nhớ Local Storage (tránh f5 bị reset số lần vi phạm)
    let violationCount = parseInt(localStorage.getItem(violationKey)) || 0;//mặc định là 0

    // quy định số lần vi phạm tối đa
    const maxViolations = 3;
    //3. lắng nghe sự kiện chuyển Tab, nếu có Tab hay cửa sổ đè lên Tab đang thi thì bắt lỗi
    document.addEventListener("visibilitychange", function () {
        //Chỉ xử lý khi trạng thái chuyển sang 'hidden' (chị che khuất, chuyển tab)
        //bỏ qua các Modal xuất hiện khi ấn nộp bài hoặc xem kết quả
        const submitModal = document.getElementById('submitModal');
        const resultModal = document.getElementById('resultModal');
        const isModalOpen = (submitModal && submitModal.style.display == 'flex') ||
            (resultModal && resultModal.style.display == 'flex');
        if (document.visibilityState === 'hidden' && !isModalOpen) {
            //Tăng số lần vi phạm lên 1 và lưu vào Local Storage
            violationCount++;
            localStorage.setItem(violationKey, violationCount);

            //kiểm tra mức độ vi phạm
            //cảnh báo 3 lần là Out ra khỏi phòng thi
            if (violationCount >= maxViolations) {
                // Vượt quá giới hạn -> Thu bài ép buộc
                alert(`CẢNH BÁO ĐỎ: Bạn đã chuyển tab hoặc thoát màn hình thi ${violationCount} lần! Vi phạm quy chế thi. Hệ thống sẽ tự động thu bài ngay lập tức!`);
                submitExam(true);
            } else {
                // Chưa vượt giới hạn -> Đưa ra cảnh báo
                alert(`CẢNH BÁO GIAN LẬN (${violationCount}/${maxViolations}): Bạn vừa chuyển tab hoặc rời khỏi màn hình làm bài! Nếu vi phạm quá ${maxViolations} lần, bài thi sẽ tự động nộp.`);
            }
        }
    });
}


//
//10. KHÔI PHỤC ĐÁP ÁN ĐÃ CHỌN KHI TẢI LẠI TRANG
//
function restoreDraftAnswers() {
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");
    const draftKey = `eduTest_draft_${examId}`;
    const draftAnswers = JSON.parse(localStorage.getItem(draftKey));//lấy ra kết quả đang lưu tại Local Storage
    if (draftAnswers) {
        //lặp lại qua từng đáp án đã lưu
        //VD từ draftAnswers = {"question_1": "2", "question_2": "4"}
        // Object.entries(draftAnswers) = [["question1","2"], ["question2","4"]]
        for (const [questionName, answerValue] of Object.entries(draftAnswers)) {
            //tìm ô radio tương ứng với tên câu hỏi và số ID đáp án
            const radioQuestionRestore = document.querySelector(`input[name="${questionName}"][value="${answerValue}"]`);

            if (radioQuestionRestore) {
                radioQuestionRestore.checked = true;
                //Kích hoạt sự kiện 'change' để giao diện (màu sắc, tiến độ) tự update
                //giống như người dùng đang thao tác chọn đáp án
                radioQuestionRestore.dispatchEvent(new Event('change'));
            }
        }
    }
}



//
// 10.5. KHÔI PHỤC CÁC CÂU ĐÃ CẮM CỜ KHI TẢI LẠI TRANG
//
function restoreFlaggedQuestions() {
    const examId = typeof currentExamId !== 'undefined' ? currentExamId : parseInt(document.getElementById("ExamId")?.value || "0");
    const flagKey = `eduTest_flagged_${examId}`;

    // Lấy mảng cờ đã lưu
    let flaggedArray = JSON.parse(localStorage.getItem(flagKey)) || [];

    if (flaggedArray.length > 0) {
        const gridItems = document.querySelectorAll('.grid-item');
        const flaggedLegendCount = document.querySelectorAll('.legend-count')[2];

        flaggedArray.forEach(gridIndex => {
            // 1. Phục hồi class 'flagged' cho ô vuông trên Sidebar
            if (gridItems[gridIndex]) {
                gridItems[gridIndex].classList.add('flagged');
            }

            // 2. Phục hồi class 'active-flag' cho nút Cờ ở khối câu hỏi
            // ID câu hỏi bắt đầu từ 1, nên index phải + 1
            const targetBlock = document.getElementById(`question-block-${gridIndex + 1}`);
            if (targetBlock) {
                const flagBtn = targetBlock.querySelector('.btn-flag');
                if (flagBtn) {
                    flagBtn.classList.add('active-flag');
                }
            }
        });

        // 3. Cập nhật lại con số hiển thị số câu đã đánh dấu trên Sidebar
        if (flaggedLegendCount) {
            flaggedLegendCount.textContent = flaggedArray.length;
        }
    }
}



// ==========================================
// XXX. KHỞI CHẠY KHI TRANG VỪA LOAD XONG
// ==========================================
window.onload = function () {
    startTimer();//hàm đếm ngược thời gian làm bài thi
    initQuestionLogic();//chọn đáp án và Animation thanh tiến độ khi chọn đáp án
    restoreDraftAnswers() //hàm khôi phục đáp ấn câu hỏi
    restoreFlaggedQuestions();//hàm khôi phục các mốc cờ đã cắm
    initFlagLogic();//hàm cắm cờ flag cho câu hỏi
    initFilterLogic();//hàm lọc Tất cả/ Đã làm/ Đánh dấu
    initAntiCheat();//hàm chống chuyển tab
    showQuestion(1);// hàm hiển thị câu hỏi, bắt đầu từ 1
};
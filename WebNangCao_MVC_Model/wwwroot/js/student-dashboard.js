document.addEventListener('DOMContentLoaded', function () {
    // 0. KHỞI TẠO BIỂU TƯỢNG (LUCIDE ICONS)
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // --- CẤU HÌNH BIẾN TOÀN CỤC ---
    const searchInput = document.getElementById('searchInput');
    const filterSubject = document.getElementById('filterSubject');
    const filterStatus = document.getElementById('filterStatus');
    const filterDifficulty = document.getElementById('filterDifficulty');
    const listItems = document.querySelectorAll('.data-item');
    const tabButtons = document.querySelectorAll('.tab-btn');
    const jsEmptyState = document.getElementById('jsEmptyState');
    const pageNumbersContainer = document.getElementById('pageNumbers');
    const prevBtn = document.getElementById('prevPage');
    const nextBtn = document.getElementById('nextPage');

    // Biến điều khiển phân trang
    let currentPage = 1;
    const itemsPerPage = 6; // Đã chỉnh lại số lượng hợp lý (6 thay vì 1)
    let currentTabStatus = 'all';
    let totalPagesGlobal = 1;

    // --- 1. LOGIC CHÍNH: LỌC & PHÂN TRANG ---

    function applyFilters() {
        console.log("%c--- BẮT ĐẦU CHẠY BỘ LỌC ---", "color: blue; font-weight: bold;");

        const searchText = searchInput ? searchInput.value.toLowerCase().trim() : '';
        const selectedSubject = filterSubject ? filterSubject.value : 'all';
        const selectedStatus = filterStatus ? filterStatus.value : 'all';
        const selectedDifficulty = filterDifficulty ? filterDifficulty.value : 'all';

        let filteredItems = [];

        // BƯỚC A: Lọc danh sách dựa trên dữ liệu Attributes
        listItems.forEach((item) => {
            const itemText = item.textContent.toLowerCase();
            const itemSubject = item.getAttribute('data-subject') || 'all';
            const itemStatusAttr = item.getAttribute('data-status') || 'all';
            const itemDifficulty = item.getAttribute('data-difficulty') || 'all';

            const matchSearch = itemText.includes(searchText);
            const matchSubject = selectedSubject === 'all' || itemSubject === selectedSubject;
            const matchStatus = selectedStatus === 'all' || itemStatusAttr === selectedStatus;
            const matchDifficulty = selectedDifficulty === 'all' || itemDifficulty === selectedDifficulty;
            const matchTab = currentTabStatus === 'all' || itemStatusAttr === currentTabStatus;

            // Ẩn tất cả trước
            item.style.display = 'none';

            if (matchSearch && matchSubject && matchStatus && matchDifficulty && matchTab) {
                filteredItems.push(item);
            }
        });

        // BƯỚC B: Tính toán số trang
        const totalItems = filteredItems.length;
        totalPagesGlobal = Math.ceil(totalItems / itemsPerPage) || 1;

        if (currentPage > totalPagesGlobal) currentPage = 1;

        // BƯỚC C: Hiển thị các item thuộc trang hiện tại
        const startIndex = (currentPage - 1) * itemsPerPage;
        const endIndex = startIndex + itemsPerPage;

        filteredItems.forEach((item, index) => {
            if (index >= startIndex && index < endIndex) {
                item.style.display = 'block';
                item.style.animation = 'fadeInUp 0.4s ease forwards';
            }
        });

        // BƯỚC D: Cập nhật UI bổ trợ
        renderPaginationUI(totalPagesGlobal);
        updateNavigationButtons();

        if (jsEmptyState) {
            jsEmptyState.style.display = (totalItems === 0) ? 'flex' : 'none';
            if (totalItems === 0 && typeof lucide !== 'undefined') lucide.createIcons();
        }

        console.log(`Đang hiển thị trang ${currentPage}/${totalPagesGlobal} (${totalItems} bài thi)`);
    }

    // HÀM VẼ CÁC SỐ TRANG (1, 2, 3...)
    function renderPaginationUI(totalPages) {
        if (!pageNumbersContainer) return;
        pageNumbersContainer.innerHTML = '';

        for (let i = 1; i <= totalPages; i++) {
            const btn = document.createElement('button');
            btn.innerText = i;
            btn.className = `page-num ${i === currentPage ? 'active' : ''}`;
            btn.onclick = () => {
                currentPage = i;
                applyFilters();
                window.scrollTo({ top: 400, behavior: 'smooth' });
            };
            pageNumbersContainer.appendChild(btn);
        }
    }

    // HÀM CẬP NHẬT TRẠNG THÁI NÚT PREV/NEXT KHI Ở CUỐI / ĐẦU TRANG
    function updateNavigationButtons() {
        if (prevBtn) prevBtn.disabled = (currentPage === 1);
        if (nextBtn) nextBtn.disabled = (currentPage === totalPagesGlobal || totalPagesGlobal === 0);
    }

    // --- 2. ĐĂNG KÝ SỰ KIỆN (EVENT LISTENERS) ---

    if (prevBtn) {
        prevBtn.onclick = () => {
            if (currentPage > 1) {
                currentPage--;
                applyFilters();
                window.scrollTo({ top: 400, behavior: 'smooth' });
            }
        };
    }

    if (nextBtn) {
        nextBtn.onclick = () => {
            if (currentPage < totalPagesGlobal) {
                currentPage++;
                applyFilters();
                window.scrollTo({ top: 400, behavior: 'smooth' });
            }
        };
    }

    // Lọc tự động khi nhập liệu
    [searchInput, filterSubject, filterStatus, filterDifficulty].forEach(el => {
        if (el) {
            const eventType = el.tagName === 'INPUT' ? 'input' : 'change';
            el.addEventListener(eventType, () => {
                currentPage = 1;
                applyFilters();
            });
        }
    });

    // Sự kiện Click Tab
    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            tabButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');
            currentTabStatus = this.getAttribute('data-tab') || 'all';
            currentPage = 1;
            applyFilters();
        });
    });

    // --- 3. UI/UX: DROPDOWN, MODAL, ALERTS ---

    // Dropdown Avatar
    const userAvatar = document.getElementById('userAvatar');
    const avatarDropdown = document.getElementById('avatarDropdown');
    if (userAvatar && avatarDropdown) {
        userAvatar.onclick = (e) => {
            e.stopPropagation();
            avatarDropdown.classList.toggle('show');
            avatarDropdown.style.display = avatarDropdown.classList.contains('show') ? 'block' : 'none';
        };
    }

    // Đóng dropdown khi click bên ngoài
    document.addEventListener('click', () => {
        if (avatarDropdown) {
            avatarDropdown.classList.remove('show');
            avatarDropdown.style.display = 'none';
        }
    });

    // Modal Tham gia lớp học
    const modalOverlay = document.getElementById('joinClassModal');
    ['btnOpenJoinModal', 'btnCloseJoinModal', 'btnCancelJoinModal'].forEach(id => {
        const el = document.getElementById(id);
        if (el && modalOverlay) {
            el.onclick = (e) => {
                e.preventDefault();
                modalOverlay.classList.toggle('active');
            };
        }
    });

    // Tự động tắt Alerts
    document.querySelectorAll('.custom-alert').forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = "0";
            setTimeout(() => alert.remove(), 500);
        }, 5000);

        const closeBtn = alert.querySelector('.btn-close-alert');
        if (closeBtn) {
            closeBtn.onclick = () => alert.remove();
        }
    });

    // Nút mở bộ lọc nâng cao
    const btnToggleFilter = document.getElementById('btnToggleFilter');
    const advancedFilter = document.getElementById('advancedFilter');
    if (btnToggleFilter && advancedFilter) {
        btnToggleFilter.onclick = () => {
            advancedFilter.classList.toggle('active');
        };
    }

    // --- 4. KHỞI CHẠY LẦN ĐẦU ---
    applyFilters();

    // Khôi phục trạng thái collapse của section lớp học
    const classSection = document.getElementById('classSection');
    if (classSection && localStorage.getItem('classSection_state') === 'closed') {
        classSection.classList.add('collapsed');
    }

    // Logic kéo thả phân loại câu hỏi
    const personalModal = document.getElementById('personalExamModal');
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('fileInput');
    const errorContainer = document.getElementById('errorContainer');
    const errorList = document.getElementById('errorList');

    // Mở modal (khi bấm vào khu vực chọn file)
    dropZone.onclick = () => fileInput.click();

    // Đóng modal
    document.getElementById('btnOpenPersonalExamModal').onclick = () => personalModal.classList.add('active');
    document.getElementById('btnClosePersonalModal').onclick = () => {
        personalModal.classList.remove('active');
        resetDropZone(); // Hàm reset lại giao diện khi đóng
    };

    // Xử lý hiệu ứng Drag & Drop
    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.style.borderColor = '#4f46e5';
        dropZone.style.background = '#e0e7ff';
    });

    dropZone.addEventListener('dragleave', (e) => {
        e.preventDefault();
        dropZone.style.borderColor = '#cbd5e1';
        dropZone.style.background = '#f8fafc';
    });

    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.style.borderColor = '#cbd5e1';
        dropZone.style.background = '#f8fafc';

        if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
            handleFileUpload(e.dataTransfer.files[0]);
        }
    });

    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            handleFileUpload(e.target.files[0]);
        }
    });

    // Đọc file Excel và gửi lên server
    function handleFileUpload(file) {
        if (!file.name.match(/\.(xlsx|xls)$/i)) {
            alert('Chỉ chấp nhận định dạng file Excel (.xlsx, .xls)');
            return;
        }

        const formData = new FormData();
        formData.append('excelFile', file); // 'excelFile' phải trùng với tên tham số trong Controller

        // Hiển thị trạng thái loading UI
        dropZone.innerHTML = `
            <i data-lucide="loader" class="lucide-spin" style="width: 48px; height: 48px; color: #4f46e5; margin-bottom: 16px;"></i>
            <h4 style="margin: 0 0 8px 0; color: #334155;">Đang tải lên và xử lý...</h4>
            <p style="margin: 0; font-size: 14px; color: #64748b;">Vui lòng không đóng cửa sổ này</p>
        `;
        if (typeof lucide !== 'undefined') lucide.createIcons();
        errorContainer.style.display = 'none';

        // GỌI API LÊN BACKEND
        fetch('/Student/UploadPersonalExam', {
            method: 'POST',
            body: formData
        })
            // .then(response => response.json())
            .then(response => {
                //  Kiểm tra Content-Type trước khi parse JSON
                const contentType = response.headers.get('content-type');

                if (!contentType || !contentType.includes('application/json')) {
                    // Server trả về HTML → không phải JSON
                    throw new Error(`Server trả về HTTP ${response.status}. Có thể session hết hạn hoặc server bị lỗi.`);
                }

                return response.json();
            })

            .then(data => {
                if (data.success) {
                    // Đọc file thành công: Chuyển hướng thẳng sang giao diện phân loại câu hỏi
                    // Chuyển hướng sang trang phân loại câu hỏi với ID vừa tạo
                    window.location.href = `/Student/ClassifyExam?examId=${data.examId}`;
                } else {
                    // Tải thất bại -> Hiển thị danh sách lỗi trả về từ NPOI
                    showErrors(data.errors || [data.message || 'Đã có lỗi xảy ra khi xử lý file.']);
                    resetDropZone();
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showErrors(['Lỗi kết nối đến máy chủ. Vui lòng thử lại sau.']);
                resetDropZone();
            });
    }

    // 1. Sửa lỗi showErrors: Phân tích đúng Object để lấy "dong" và "loi" thay vì xuất ra [object Object]
    function showErrors(errors) {
        errorList.innerHTML = '';
        errors.forEach(err => {
            const li = document.createElement('li');

            if (typeof err === 'object' && err !== null) {
                // Nếu đối tượng trả về chứa dòng và nội dung lỗi
                li.textContent = err.dong && err.dong > 0
                    ? `Dòng ${err.dong}: ${err.loi}`
                    : err.loi;
            } else {
                // Fallback nếu error là một string bình thường
                li.textContent = err;
            }

            li.style.marginBottom = '4px';
            errorList.appendChild(li);
        });
        errorContainer.style.display = 'block';
    }

    // Hàm phụ trợ: Reset lại UI của drop zone
    function resetDropZone() {
        fileInput.value = '';
        dropZone.innerHTML = `
            <i data-lucide="upload-cloud" style="width: 48px; height: 48px; color: #64748b; margin-bottom: 16px;"></i>
            <h4 style="margin: 0 0 8px 0; color: #334155;">Kéo thả file Excel vào đây</h4>
            <p style="margin: 0; font-size: 14px; color: #64748b;">hoặc click để chọn file (.xlsx, .xls)</p>
        `;
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }
}); // Kết thúc block DOMContentLoaded

// Hàm toàn cục cho HTML
function toggleSection(sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return;
    section.classList.toggle('collapsed');
    const isCollapsed = section.classList.contains('collapsed');
    localStorage.setItem(sectionId + '_state', isCollapsed ? 'closed' : 'open');
}
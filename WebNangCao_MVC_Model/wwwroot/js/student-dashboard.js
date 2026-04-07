document.addEventListener('DOMContentLoaded', function () {
    // Khởi tạo Lucide Icons (Biến các thẻ <i> thành <svg>)
    // Khởi tạo giúp đọc và hiểu lệnh: "Data-lucide" trong HTML
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    //
    // 1. LOGIC ẨN/HIỆN UI BỘ LỌC NÂNG CAO
    // 
    const btnToggleFilter = document.getElementById('btnToggleFilter');
    const advancedFilter = document.getElementById('advancedFilter');

    if (btnToggleFilter && advancedFilter) {
        btnToggleFilter.addEventListener('click', function () {
            // Thêm/Xóa class 'active' để ẩn hiện (đã css display: grid)
            advancedFilter.classList.toggle('active');

            // Đổi màu nút lọc để tạo cảm giác đang được chọn
            if (advancedFilter.classList.contains('active')) {
                btnToggleFilter.style.backgroundColor = '#eaeaea';
            } else {
                btnToggleFilter.style.backgroundColor = 'var(--bg-main)';
            }
        });
    }

    // 
    // 2. LOGIC BỘ LỌC NHIỀU LỚP (TÌM KIẾM + TABS + DROPDOWN)
    // 
    // Giả định các ID cho input và dropdown (sếp nhớ map đúng với HTML nhé)
    const searchInput = document.getElementById('searchInput');
    const filterDropdown1 = document.getElementById('filterSubject'); // Ví dụ: dropdown môn học
    const filterDropdown2 = document.getElementById('filterDate');    // Ví dụ: dropdown thời gian
    const listItems = document.querySelectorAll('.data-item');        // Các thẻ card/row bài thi cần lọc
    const tabButtons = document.querySelectorAll('.tab-btn');

    let currentTabStatus = 'all'; // Trạng thái mặc định của tab

    // Hàm thực thi lọc đa điều kiện
    const jsEmptyState = document.getElementById('jsEmptyState'); // kiểm tra nếu không thấy cái gì trong bộ lọc thì in ra thông báo

    function applyFilters() {
        const searchText = searchInput ? searchInput.value.toLowerCase().trim() : '';
        const selectedSubject = filterSubject ? filterSubject.value : 'all';
        const selectedStatus = filterStatus ? filterStatus.value : 'all';
        const selectedDifficulty = filterDifficulty ? filterDifficulty.value : 'all';

        let visibleCount = 0; // Thêm biến đếm số lượng thẻ được hiển thị

        listItems.forEach(item => {
            const itemText = item.textContent.toLowerCase();
            const itemSubject = item.getAttribute('data-subject') || 'all';
            const itemStatusAttr = item.getAttribute('data-status') || 'all';
            const itemDifficulty = item.getAttribute('data-difficulty') || 'all';

            const matchSearch = itemText.includes(searchText);
            const matchSubject = selectedSubject === 'all' || itemSubject === selectedSubject;
            const matchStatus = selectedStatus === 'all' || itemStatusAttr === selectedStatus;
            const matchDifficulty = selectedDifficulty === 'all' || itemDifficulty === selectedDifficulty;
            const matchTab = currentTabStatus === 'all' || itemStatusAttr === currentTabStatus;

            if (matchSearch && matchSubject && matchStatus && matchDifficulty && matchTab) {
                item.style.display = '';
                visibleCount++; // Có 1 thẻ thỏa mãn thì cộng 1
            } else {
                item.style.display = 'none';
            }
        });

        // Xử lý hiện thông báo nếu không có thẻ nào
        if (jsEmptyState) {
            if (visibleCount === 0 && listItems.length > 0) {
                jsEmptyState.style.display = 'flex';
                // Chạy lại lucide icon cho cái kính lúp (nếu cần)
                if (typeof lucide !== 'undefined') lucide.createIcons();
            } else {
                jsEmptyState.style.display = 'none';
            }
        }
    }

    if (searchInput) searchInput.addEventListener('input', applyFilters);
    if (filterSubject) filterSubject.addEventListener('change', applyFilters);
    if (filterStatus) filterStatus.addEventListener('change', applyFilters);
    if (filterDifficulty) filterDifficulty.addEventListener('change', applyFilters);

    // Lắng nghe sự kiện gõ tìm kiếm -> lọc realtime
    if (searchInput) {
        searchInput.addEventListener('input', applyFilters);
    }

    // Lắng nghe sự kiện đổi dropdown -> lọc
    if (filterDropdown1) filterDropdown1.addEventListener('change', applyFilters);
    if (filterDropdown2) filterDropdown2.addEventListener('change', applyFilters);

    // Xử lý Tabs kết hợp bộ lọc
    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            // Xóa class active ở tất cả các nút
            tabButtons.forEach(btn => btn.classList.remove('active'));
            // Thêm class active vào nút vừa được click
            this.classList.add('active');

            // Cập nhật trạng thái tab hiện tại
            currentTabStatus = this.getAttribute('data-tab') || 'all';

            // Gọi lại bộ lọc tổng thay vì chỉ fetch API
            applyFilters();
        });
    });

    // 
    // 3. LOGIC AVATAR DROPDOWN MENU PROFILE USER
    // 
    const userAvatar = document.getElementById('userAvatar');
    const avatarDropdown = document.getElementById('avatarDropdown');

    if (userAvatar && avatarDropdown) {
        // 1. Click vào Avatar thì bật/tắt Menu
        userAvatar.addEventListener('click', function (event) {
            event.stopPropagation(); // Ngăn chặn sự kiện click lan ra ngoài body
            avatarDropdown.classList.toggle('show');
        });

        // 2. Click ra bất kỳ đâu trên màn hình thì tự đóng Menu
        document.addEventListener('click', function (event) {
            // Nếu menu đang mở VÀ chỗ click chuột KHÔNG NẰM TRONG menu hoặc avatar
            if (avatarDropdown.classList.contains('show') &&
                !avatarDropdown.contains(event.target) &&
                event.target !== userAvatar) {
                avatarDropdown.classList.remove('show'); // Đóng menu lại
            }
        });
    }

    // 
    // 4. XỬ LÝ MODAL THAM GIA LỚP
    // 
    const btnOpenModal = document.getElementById('btnOpenJoinModal');
    const btnCloseModal = document.getElementById('btnCloseJoinModal');
    const btnCancelModal = document.getElementById('btnCancelJoinModal');
    const modalOverlay = document.getElementById('joinClassModal');

    function toggleModal(e) {
        if (e) e.preventDefault();
        if (modalOverlay) modalOverlay.classList.toggle('active');
    }

    if (btnOpenModal) btnOpenModal.addEventListener('click', toggleModal);
    if (btnCloseModal) btnCloseModal.addEventListener('click', toggleModal);
    if (btnCancelModal) btnCancelModal.addEventListener('click', toggleModal);

    // Bấm ra ngoài vùng xám để đóng modal
    if (modalOverlay) {
        modalOverlay.addEventListener('click', function (e) {
            if (e.target === modalOverlay) toggleModal();
        });
    }

    // 
    // 5. XỬ LÝ TẮT THÔNG BÁO (ALERTS)
    // 
    const alertCloseBtns = document.querySelectorAll('.btn-close-alert');
    alertCloseBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const alertBox = this.closest('.custom-alert');
            if (alertBox) {
                alertBox.style.display = 'none';
            }
        });
    });
});
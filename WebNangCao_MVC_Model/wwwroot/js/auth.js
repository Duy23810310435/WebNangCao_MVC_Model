// auth.js

function switchTab(tabName) {
    // 1. Xác định các Element
    const loginForm = document.getElementById('tab-content-login');
    const registerForm = document.getElementById('tab-content-register');
    const loginBtn = document.getElementById('btn-tab-login');
    const registerBtn = document.getElementById('btn-tab-register');

    // 2. Xử lý Logic ẩn/hiện Form
    if (tabName === 'login') {
        loginForm.classList.remove('hidden');
        registerForm.classList.add('hidden');

        // Style cho nút
        loginBtn.classList.add('active');
        registerBtn.classList.remove('active');
    } else {
        loginForm.classList.add('hidden');
        registerForm.classList.remove('hidden');

        // Style cho nút
        registerBtn.classList.add('active');
        loginBtn.classList.remove('active');
    }
}
// --- LOGIC ROLE SELECTOR ---

// 1. Hàm bật/tắt dropdown
function toggleRoleList() {
    const dropdown = document.getElementById('roleDropdown');
    const options = document.getElementById('roleOptions');

    dropdown.classList.toggle('open');
    options.classList.toggle('active');
}

// 2. Hàm chọn vai trò
function selectRole(value, text, iconName) {
    // 1. CẬP NHẬT TEXT: Đổi chữ hiển thị trên Dropdown (ví dụ: "Học viên" -> "Quản trị viên")
    document.getElementById('current-role-text').innerText = text;

    // 2. CẬP NHẬT ICON (Xử lý an toàn với DOM):
    const oldIcon = document.getElementById('current-role-icon');
    if (oldIcon) {
        // Tạo hẳn một thẻ <i> mới tinh để tránh bị dính rác từ thẻ <svg> cũ của Lucide
        const newIcon = document.createElement('i');
        newIcon.setAttribute('data-lucide', iconName); // Gắn tên icon mới (user, briefcase, shield)
        newIcon.id = 'current-role-icon'; // Giữ lại ID để lần sau còn tìm được
        newIcon.className = 'role-icon'; // Giữ lại class CSS
        oldIcon.replaceWith(newIcon);
        lucide.createIcons(); // Lúc này Lucide chỉ chú ý đến cái thẻ vừa tạo, không phá phách chỗ khác
    }

    // c. THUẬT TOÁN TẬN DIỆT 2 DẤU TÍCH:
    // Tìm TẤT CẢ các thẻ có ID bắt đầu bằng chữ "check-" và giáng đòn "Tàng hình"
    const allChecks = document.querySelectorAll('[id^="check-"]');
    allChecks.forEach(icon => {
        icon.style.display = 'none';
        icon.style.opacity = '0'; // Đè thêm opacity cho chắc cú 100%
    });

    // Chỉ "Hồi sinh" đúng cái dấu tích của Role đang được chọn
    const activeCheck = document.getElementById('check-' + value);
    if (activeCheck) {
        activeCheck.style.display = 'inline-block'; // Hoặc block tuỳ CSS của em
        activeCheck.style.opacity = '1';
    }

    // d. Cập nhật Input ẩn
    const inputLogin = document.getElementById('input-role-login');
    const inputRegister = document.getElementById('input-role-register');
    if (inputLogin) inputLogin.value = value;
    if (inputRegister) inputRegister.value = value;

    // e. Khởi động hệ thống chặn Admin đăng ký
    handleAdminSecurity(value);

    // f. Đóng dropdown an toàn
    if (window.event) {
        window.event.stopPropagation();
    }
    toggleRoleList();
}
function handleAdminSecurity(selectedRole) {
    const registerBtn = document.getElementById('btn-tab-register');
    const loginBtn = document.getElementById('btn-tab-login');
    if (selectedRole === 'admin') {
        //Nếu đang ở tab đăng ký thì tự động quay về tab đăng nhập
        switchTab('login');
        //Vô hiệu hoá nút đăng ký
        registerBtn.style.display = 'none';
    }
    else {
        //Nếu chọn role Hhọc Viên/ Giảng Viên thì nút đăng ký lại hiện ra
        registerBtn.style.display = 'inline-flex';
    }
}

// 3. Đóng dropdown khi click ra ngoài
document.addEventListener('click', function (event) {
    const dropdown = document.getElementById('roleDropdown');
    const options = document.getElementById('roleOptions');

    // Nếu click không nằm trong dropdown thì đóng nó
    if (!dropdown.contains(event.target)) {
        dropdown.classList.remove('open');
        options.classList.remove('active');
    }
});
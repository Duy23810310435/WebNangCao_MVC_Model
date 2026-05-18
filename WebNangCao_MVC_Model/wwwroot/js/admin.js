    /*const tabButtons = document.querySelectorAll('.tab-btn');
    const allCards = document.querySelectorAll('.user-card');
    const activeClasses = ['bg-white', 'text-gray-900', 'shadow-sm', 'active-tab'];
    const inactiveClasses = ['hover:bg-gray-200/50', 'inactive-tab'];
    const openbuttonDetails = document.getElementById('system-open-details');
    const modelDetails = document.getElementById('settings-modal-details');
    const closebuttonDetails = document.getElementById('system-close-details');
    const openbuttonAdd = document.getElementById('system-open-add');
    const modelAdd = document.getElementById('settings-model-add');
    const closebuttonAdd = document.getElementById('system-close-add');
    const openbuttonDatabase = document.getElementById('system-open-database');
    const modelDatabase = document.getElementById('settings-model-database');
    const closebuttonDatabase = document.getElementById('system-close-database');
    const openbuttonConfig = document.getElementById('system-open-config');
    const modelConfig = document.getElementById('settings-model-config');
    const closebuttonConfig = document.getElementById('system-close-config');
    // THÊM 2 DÒNG NÀY VÀO ĐỘI HÌNH:
const openTopConfig = document.getElementById('top-open-config');
const closebuttonConfigBtn = document.getElementById('system-close-config-btn');
    // Hàm mở Modal chung
    const openConfigModal = () => {
        modelConfig.classList.remove('hidden');
        modelConfig.classList.add('flex');
        document.body.style.overflow = 'hidden'; // Khóa cuộn màn hình ở dưới
    };

    // Hàm đóng Modal chung
    const closeConfigModal = () => {
        modelConfig.classList.remove('flex');
        modelConfig.classList.add('hidden');
        document.body.style.overflow = 'auto'; // Mở lại cuộn màn hình
    };
    // Gắn event cho các nút
    openTopConfig.addEventListener('click', openConfigModal);
    openbuttonConfig.addEventListener('click', openConfigModal);
    
    closebuttonConfig.addEventListener('click', closeConfigModal);
    closebuttonConfigBtn.addEventListener('click', closeConfigModal);
    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const targetCategory = button.getAttribute('data-tab');
                tabButtons.forEach(btn => {
                    btn.classList.remove(...activeClasses);
                    btn.classList.add(...inactiveClasses);
                });
                if (targetCategory === 'superall') {
                    const allTab = document.querySelector('.tab-btn[data-tab="all"]');
                    allTab.classList.remove(...inactiveClasses);
                    allTab.classList.add(...activeClasses);
                } else {
                    button.classList.remove(...inactiveClasses);
                    button.classList.add(...activeClasses);
                }
                allCards.forEach(card => {
                    const cardCategory = card.getAttribute('data-category');
                    if (targetCategory === 'all' || targetCategory === 'superall' || cardCategory.includes(targetCategory)) { 
                        card.style.display = 'flex';
                    } else { 
                        card.style.display = 'none';
                        }
                    });
                });
            });
    openbuttonDetails.addEventListener('click',() => {
        modelDetails.classList.remove('hidden');
        modelDetails.classList.add('flex');
    });
    closebuttonDetails.addEventListener('click', () => {
        modelDetails.classList.remove('flex');
        modelDetails.classList.add('hidden');
    });
    openbuttonAdd.addEventListener('click', () => {
        modelAdd.classList.remove('hidden');
        modelAdd.classList.add('flex');
    });
    closebuttonAdd.addEventListener('click', () => {
        modelAdd.classList.remove('flex');
        modelAdd.classList.add('hidden');
    });
    openbuttonConfig.addEventListener('click', () => {
        modelConfig.classList.remove('hidden');
        modelConfig.classList.add('flex');
    });
    closebuttonConfig.addEventListener('click', () => {
        modelConfig.classList.remove('flex');
        modelConfig.classList.add('hidden');
    });
    openbuttonDatabase.addEventListener('click', () => {
        modelDatabase.classList.remove('hidden');
        modelDatabase.classList.add('flex');
    });
    closebuttonDatabase.addEventListener('click', () => {
        modelDatabase.classList.remove('flex');
        modelDatabase.classList.add('hidden');
    });*/
    const tabButtons = document.querySelectorAll('.tab-btn');
    const allCards = document.querySelectorAll('.user-card');
    const activeClasses = ['bg-white', 'text-gray-900', 'shadow-sm', 'active-tab'];
    const inactiveClasses = ['hover:bg-gray-200/50', 'inactive-tab'];

    // =========================================
    // 1. HÀM TIỆN ÍCH ĐÓNG/MỞ MODAL CHUẨN UX
    // ==========================================
    const openModal = (modalNode) => {
        if (!modalNode) return; // Không có HTML thì bỏ qua, không văng lỗi!
        modalNode.classList.remove('hidden');
        modalNode.classList.add('flex');
        document.body.style.overflow = 'hidden'; // Khóa cuộn
    };

    const closeModal = (modalNode) => {
        if (!modalNode) return;
        modalNode.classList.remove('flex');
        modalNode.classList.add('hidden');
        document.body.style.overflow = 'auto'; // Mở lại cuộn
    };

    // Hàm đính kèm tính năng "Bấm ra ngoài phông đen để đóng"
    const attachBackdropClose = (modalNode) => {
        if (!modalNode) return;
        modalNode.addEventListener('click', (e) => {
            if (e.target === modalNode) closeModal(modalNode);
        });
    };

    // ==========================================
    // 2. KHỞI TẠO MODAL CẤU HÌNH HỆ THỐNG
    // ==========================================
    const modelConfig = document.getElementById('settings-model-config');
    const btnOpenTopConfig = document.getElementById('top-open-config');
    const btnOpenSystemConfig = document.getElementById('system-open-config');
    const btnCloseConfigX = document.getElementById('system-close-config');
    const btnCloseConfigCancel = document.getElementById('system-close-config-btn');

    if (btnOpenTopConfig) btnOpenTopConfig.addEventListener('click', () => openModal(modelConfig));
    if (btnOpenSystemConfig) btnOpenSystemConfig.addEventListener('click', () => openModal(modelConfig));
    if (btnCloseConfigX) btnCloseConfigX.addEventListener('click', () => closeModal(modelConfig));
    if (btnCloseConfigCancel) btnCloseConfigCancel.addEventListener('click', () => closeModal(modelConfig));
    
    attachBackdropClose(modelConfig);

    // ==========================================
    // 3. KHỞI TẠO MODAL BÁO CÁO CHI TIẾT
    // ==========================================
    const modelDetails = document.getElementById('settings-modal-details');
    const btnOpenDetails = document.getElementById('system-open-details');
    const btnCloseDetails = document.getElementById('system-close-details');

    if (btnOpenDetails) btnOpenDetails.addEventListener('click', () => openModal(modelDetails));
    if (btnCloseDetails) btnCloseDetails.addEventListener('click', () => closeModal(modelDetails));
    
    attachBackdropClose(modelDetails);

    // ==========================================
    // 4. CHUYỂN TAB QUẢN LÝ NGƯỜI DÙNG
    // ==========================================
    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const targetCategory = button.getAttribute('data-tab');
            
            tabButtons.forEach(btn => {
                btn.classList.remove(...activeClasses);
                btn.classList.add(...inactiveClasses);
            });

            if (targetCategory === 'superall') {
                const allTab = document.querySelector('.tab-btn[data-tab="all"]');
                if (allTab) {
                    allTab.classList.remove(...inactiveClasses);
                    allTab.classList.add(...activeClasses);
                }
            } else {
                button.classList.remove(...inactiveClasses);
                button.classList.add(...activeClasses);
            }

            allCards.forEach(card => {
                const cardCategory = card.getAttribute('data-category');
                if (targetCategory === 'all' || targetCategory === 'superall' || cardCategory.includes(targetCategory)) { 
                    card.style.display = 'flex';
                } else { 
                    card.style.display = 'none';
                }
            });
        });
    });
    // ==========================================
    // 5. CHUYỂN TAB TRONG MODAL CẤU HÌNH
    // ==========================================
    const configTabBtns = document.querySelectorAll('.config-tab-btn');
    const configTabContents = document.querySelectorAll('.config-tab-content');

    const configActiveClasses = ['bg-white', 'shadow-sm', 'border-gray-100', 'text-gray-800'];
    const configInactiveClasses = ['text-gray-600', 'border-transparent', 'hover:text-gray-800', 'hover:bg-white/50'];

    configTabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            // Lấy ID của tab cần hiển thị
            const targetId = btn.getAttribute('data-target');

            // 1. Reset màu tất cả các nút
            configTabBtns.forEach(b => {
                b.classList.remove(...configActiveClasses);
                b.classList.add(...configInactiveClasses);
            });

            // 2. Kích hoạt màu cho nút vừa bấm
            btn.classList.remove(...configInactiveClasses);
            btn.classList.add(...configActiveClasses);

            // 3. Ẩn tất cả nội dung tab
            configTabContents.forEach(content => {
                content.classList.add('hidden');
            });

            // 4. Hiển thị nội dung tab tương ứng
            document.getElementById(targetId).classList.remove('hidden');
        });
    });
    // ==========================================
    // 6. XỬ LÝ MODAL THÊM NGƯỜI DÙNG (Trị bệnh trùng ID)
    // ==========================================
    const modalAddUser = document.getElementById('settings-model-add-user');
    const btnsOpenAddUser = document.querySelectorAll('#system-open-add'); // Lấy TẤT CẢ các nút có ID này
    const btnCloseAddUser = document.getElementById('close-add-user');

    // Nút nào bấm cũng mở Modal hết!
    btnsOpenAddUser.forEach(btn => {
        btn.addEventListener('click', () => openModal(modalAddUser));
    });
    if (btnCloseAddUser) btnCloseAddUser.addEventListener('click', () => closeModal(modalAddUser));
    attachBackdropClose(modalAddUser);

    // ==========================================
    // 7. XỬ LÝ NÚT SAO LƯU DỮ LIỆU (Mở Modal & Nhảy sang Tab Database)
    // ==========================================
    const btnsOpenDatabase = document.querySelectorAll('#system-open-database');
    const btnQuickBackup = document.getElementById('quick-backup-btn');

    const openBackupTab = () => {
        openModal(modelConfig); 
        const dbTabBtn = document.querySelector('.config-tab-btn[data-target="tab-database"]');
        if (dbTabBtn) dbTabBtn.click();
    };

    btnsOpenDatabase.forEach(btn => {
        btn.addEventListener('click', openBackupTab);
    });
    if (btnQuickBackup) btnQuickBackup.addEventListener('click', openBackupTab);

    // ==========================================
    // 8. NÃ ALERT XÁC NHẬN KHI BẤM "SAO LƯU NGAY"
    // ==========================================
    // ==========================================
// 8. NÃ ALERT XÁC NHẬN KHI BẤM "SAO LƯU NGAY"
// ==========================================
const btnExecuteBackup = document.getElementById('btn-execute-backup');
if (btnExecuteBackup) {
    btnExecuteBackup.addEventListener('click', () => {
        // Nã Confirm vào mặt để hỏi lại cho chắc
        const isConfirmed = confirm('⚠️ CẢNH BÁO TỪ HỆ THỐNG:\nBạn có chắc chắn muốn tiến hành sao lưu và tải toàn bộ dữ liệu xuống máy tính không?');
        
        if (isConfirmed) {
            // Đổi chữ cái nút cho xịn
            const originalText = btnExecuteBackup.innerHTML;
            btnExecuteBackup.innerHTML = "⏳ Đang kết xuất...";
            btnExecuteBackup.disabled = true;

            // Vì API của mình là HttpGet trả về File, nên chỉ cần gán thẳng window.location.href 
            // Trình duyệt sẽ tự động call API và tải file về máy mà không bị chuyển trang
            window.location.href = '/api/Admin/BackupData';

            // Nhả lại nút sau 2 giây (đợi file tải xong)
            setTimeout(() => {
                btnExecuteBackup.innerHTML = originalText;
                btnExecuteBackup.disabled = false;
            }, 2000);
        }
    });
}
    // Bọc toàn bộ code bằng DOMContentLoaded
document.addEventListener("DOMContentLoaded", function () {
    
    // 1. Tìm nút
    const btnSaveConfig = document.getElementById('btn-save-config');
    
    // 2. Check xem có tìm thấy nút không (Bật F12 -> Console để xem)
    if (!btnSaveConfig) {
        console.error("🔴 BÁO ĐỘNG: Không tìm thấy cái nút nào có ID là btn-save-config cả!");
        return; // Dừng luôn
    }

    console.log("🟢 ĐÃ TÌM THẤY NÚT LƯU CẤU HÌNH! SẴN SÀNG BÓP CÒ!");

    // 3. Gắn sự kiện Click
    btnSaveConfig.addEventListener('click', async function () {
        
        // TEST THỬ XEM NÚT ĐÃ ĂN CHƯA
        console.log("👉 Vừa bấm nút Lưu Cấu Hình!");
        
        // Gom data (giữ nguyên như cũ)
        const payload = {
            SystemName: document.getElementById('conf-sys-name')?.value || "",
            SystemUrl: document.getElementById('conf-sys-url')?.value || "",
            DefaultLanguage: document.getElementById('conf-sys-lang')?.value || "",
            Timezone: document.getElementById('conf-sys-tz')?.value || "",
            EnableEmailNotification: document.getElementById('conf-mail-noti')?.checked || false,
            EnableSmsNotification: document.getElementById('conf-sms-noti')?.checked || false,
            EnablePushNotification: document.getElementById('conf-push-noti')?.checked || false,
            EmailProvider: document.getElementById('conf-email-provider')?.value || "",
            SmtpHost: document.getElementById('conf-email-host')?.value || "",
            SmtpPort: parseInt(document.getElementById('conf-email-port')?.value) || 587,
            SmtpUser: document.getElementById('conf-email-user')?.value || "",
            SmtpPassword: document.getElementById('conf-email-pass')?.value || "",
            SessionTimeoutMinutes: parseInt(document.getElementById('conf-sec-timeout')?.value) || 30,
            MinPasswordLength: parseInt(document.getElementById('conf-sec-minpass')?.value) || 8,
            MaxFailedLogins: parseInt(document.getElementById('conf-sec-maxfail')?.value) || 5,
            Require2FA: document.getElementById('conf-sec-2fa')?.checked || false,
            ForcePasswordChange90Days: document.getElementById('conf-sec-90days')?.checked || false,
            BlockUnknownIps: document.getElementById('conf-sec-blockip')?.checked || false,
            LogAllActivities: document.getElementById('conf-sec-logall')?.checked || false
        };

        const originalText = this.innerHTML;
        this.innerHTML = "⏳ Đang lưu...";
        this.disabled = true;

        try {
            const response = await fetch('/api/Admin/UpdateSystemConfig', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (result.success) {
                alert("✅ " + result.message);
            } else {
                alert("❌ Lỗi rồi: " + result.message);
            }
        } catch (error) {
            alert("💥 Rớt mạng hoặc sập Server!");
            console.error(error);
        } finally {
            this.innerHTML = originalText;
            this.disabled = false;
        }
    });
});
document.addEventListener("DOMContentLoaded", function () {
    const btnAddUser = document.getElementById('btn-submit-add-user');

    if (btnAddUser) {
        btnAddUser.addEventListener('click', async function () {
            // 1. Gom dữ liệu
            const payload = {
                FullName: document.getElementById('add-user-name').value,
                Email: document.getElementById('add-user-email').value,
                Role: document.getElementById('add-user-role').value,
                Password: document.getElementById('add-user-pass').value
            };

            // 2. Validate nhẹ nghiệm thu
            if (!payload.FullName || !payload.Email || !payload.Password) {
                alert("Vui lòng điền đầy đủ thông tin!");
                return;
            }

            const originalText = this.innerHTML;
            this.innerHTML = "⏳ Đang tạo...";
            this.disabled = true;

            try {
                // 3. Bắn pháo sáng API
                const response = await fetch('/api/Admin/AddUser', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    alert("✅ " + result.message);
                    // MỘT BƯỚC KHÔN NGOAN: Load lại trang để Data mới từ DB tự động đổ vào vòng lặp @foreach
                    window.location.reload(); 
                } else {
                    alert("❌ Lỗi: " + result.message);
                }
            } catch (error) {
                alert("💥 Sập Server hoặc đứt cáp quang!");
                console.error(error);
            } finally {
                this.innerHTML = originalText;
                this.disabled = false;
            }
        });
    }
});
// HÀM BẮN API DUYỆT NGƯỜI DÙNG
async function approveUser(userId) {
    // Hỏi lại cho chắc cốp, nhỡ bấm nhầm
    if (!confirm('Bạn có chắc chắn muốn duyệt và cấp quyền cho tài khoản này?')) {
        return;
    }

    try {
        // Bắn API với phương thức POST (Kèm theo cái ID trên URL)
        const response = await fetch(`/api/Admin/ApproveUser/${userId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (response.ok && result.success) {
            alert("✅ " + result.message);
            // DUYỆT XONG THÌ TẢI LẠI TRANG ĐỂ NÓ NHẢY TỪ TAB VÀNG SANG TAB XANH
            window.location.reload(); 
        } else {
            alert("❌ Lỗi: " + result.message);
        }
    } catch (error) {
        alert("💥 Rớt mạng hoặc Server sập!");
        console.error(error);
    }
}
// ==========================================
// HÀM BẮN API XÓA NGƯỜI DÙNG
// ==========================================
async function deleteUser(userId) {
    // Cảnh báo đỏ rực rỡ trước khi xuống tay
    if (!confirm('🚨 BÁO ĐỘNG: Bạn có chắc chắn muốn XÓA VĨNH VIỄN tài khoản này không? Hành động này không thể hoàn tác!')) {
        return;
    }

    try {
        // Dùng method DELETE thay vì POST
        const response = await fetch(`/api/Admin/DeleteUser/${userId}`, {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (response.ok && result.success) {
            alert("✅ " + result.message);
            window.location.reload(); // Quét lại danh sách
        } else {
            alert("❌ Lỗi: " + result.message);
        }
    } catch (error) {
        alert("💥 Rớt mạng hoặc Server sập!");
        console.error(error);
    }
}

// ==========================================
// 9. XỬ LÝ MODAL SỬA NGƯỜI DÙNG (EDIT)
// ==========================================
const modalEditUser = document.getElementById('settings-model-edit-user');
const btnCloseEditUser = document.getElementById('close-edit-user');

if (btnCloseEditUser) btnCloseEditUser.addEventListener('click', () => closeModal(modalEditUser));
attachBackdropClose(modalEditUser);

// NHỊP 1: BẤM NÚT SỬA -> GỌI API LẤY DATA -> ĐỔ VÀO FORM -> HIỆN MODAL
async function openEditModal(userId) {
    try {
        const response = await fetch(`/api/Admin/GetUser/${userId}`);
        const result = await response.json();

        if (response.ok && result.success) {
            const u = result.data;
            
            // Đổ Data vào các ô Input
            document.getElementById('edit-user-id').value = u.id;
            document.getElementById('edit-user-name').value = u.fullName;
            document.getElementById('edit-user-email').value = u.email;
            document.getElementById('edit-user-role').value = u.role.toLowerCase();
            document.getElementById('edit-user-active').value = u.isActive ? "true" : "false";

            // Hiển thị Modal
            openModal(modalEditUser);
        } else {
            alert("❌ Không thể lấy thông tin người dùng: " + result.message);
        }
    } catch (error) {
        alert("💥 Lỗi kết nối đến Server!");
        console.error(error);
    }
}

// NHỊP 2: BẤM LƯU THAY ĐỔI -> GOM DATA -> BẮN LÊN SERVER
document.addEventListener("DOMContentLoaded", function () {
    const btnSubmitEdit = document.getElementById('btn-submit-edit-user');

    if (btnSubmitEdit) {
        btnSubmitEdit.addEventListener('click', async function () {
            const payload = {
                Id: parseInt(document.getElementById('edit-user-id').value),
                FullName: document.getElementById('edit-user-name').value,
                Email: document.getElementById('edit-user-email').value,
                Role: document.getElementById('edit-user-role').value,
                IsActive: document.getElementById('edit-user-active').value === "true"
            };

            if (!payload.FullName || !payload.Email) {
                alert("⚠️ Họ tên và Email không được để trống!");
                return;
            }

            const originalText = this.innerHTML;
            this.innerHTML = "⏳ Đang lưu...";
            this.disabled = true;

            try {
                const response = await fetch('/api/Admin/EditUser', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    alert("✅ " + result.message);
                    window.location.reload(); // Quét lại danh sách
                } else {
                    alert("❌ Lỗi: " + result.message);
                }
            } catch (error) {
                alert("💥 Sập Server rồi!");
                console.error(error);
            } finally {
                this.innerHTML = originalText;
                this.disabled = false;
            }
        });
    }
});
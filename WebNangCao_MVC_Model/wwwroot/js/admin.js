const tabButtons = document.querySelectorAll('.tab-btn');
const allCards = document.querySelectorAll('.user-card');
const activeClasses = ['bg-white', 'text-gray-900', 'shadow-sm', 'active-tab'];
const inactiveClasses = ['hover:bg-gray-200/50', 'inactive-tab'];

// Lấy kho từ điển từ HTML
const t = window.AppTranslations;

// 1. HÀM TIỆN ÍCH ĐÓNG/MỞ MODAL 
const openModal = (modalNode) => {
    if (!modalNode) return; 
    modalNode.classList.remove('hidden');
    modalNode.classList.add('flex');
    document.body.style.overflow = 'hidden'; 
};

const closeModal = (modalNode) => {
    if (!modalNode) return;
    modalNode.classList.remove('flex');
    modalNode.classList.add('hidden');
    document.body.style.overflow = 'auto'; 
};

const attachBackdropClose = (modalNode) => {
    if (!modalNode) return;
    modalNode.addEventListener('click', (e) => {
        if (e.target === modalNode) closeModal(modalNode);
    });
};

// 2. KHỞI TẠO MODAL CẤU HÌNH HỆ THỐNG
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

// 3. KHỞI TẠO MODAL BÁO CÁO CHI TIẾT
const modelDetails = document.getElementById('settings-modal-details');
const btnOpenDetails = document.getElementById('system-open-details');
const btnCloseDetails = document.getElementById('system-close-details');

if (btnOpenDetails) btnOpenDetails.addEventListener('click', () => openModal(modelDetails));
if (btnCloseDetails) btnCloseDetails.addEventListener('click', () => closeModal(modelDetails));

attachBackdropClose(modelDetails);

// 4. CHUYỂN TAB QUẢN LÝ NGƯỜI DÙNG
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

// 5. CHUYỂN TAB TRONG MODAL CẤU HÌNH
const configTabBtns = document.querySelectorAll('.config-tab-btn');
const configTabContents = document.querySelectorAll('.config-tab-content');

const configActiveClasses = ['bg-white', 'shadow-sm', 'border-gray-100', 'text-gray-800'];
const configInactiveClasses = ['text-gray-600', 'border-transparent', 'hover:text-gray-800', 'hover:bg-white/50'];

configTabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
        const targetId = btn.getAttribute('data-target');

        configTabBtns.forEach(b => {
            b.classList.remove(...configActiveClasses);
            b.classList.add(...configInactiveClasses);
        });

        btn.classList.remove(...configInactiveClasses);
        btn.classList.add(...configActiveClasses);

        configTabContents.forEach(content => {
            content.classList.add('hidden');
        });

        document.getElementById(targetId).classList.remove('hidden');
    });
});

// 6. XỬ LÝ MODAL THÊM NGƯỜI DÙNG 
const modalAddUser = document.getElementById('settings-model-add-user');
const btnsOpenAddUser = document.querySelectorAll('#system-open-add'); 
const btnCloseAddUser = document.getElementById('close-add-user');

btnsOpenAddUser.forEach(btn => {
    btn.addEventListener('click', () => openModal(modalAddUser));
});
if (btnCloseAddUser) btnCloseAddUser.addEventListener('click', () => closeModal(modalAddUser));
attachBackdropClose(modalAddUser);

// 7. XỬ LÝ NÚT SAO LƯU DỮ LIỆU
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

// 8. NÃ ALERT XÁC NHẬN KHI BẤM "SAO LƯU NGAY"
const btnExecuteBackup = document.getElementById('btn-execute-backup');
if (btnExecuteBackup) {
    btnExecuteBackup.addEventListener('click', () => {
        const isConfirmed = confirm(t.MsgConfirmBackup); // 🚨 Đã Localize
        
        if (isConfirmed) {
            const originalText = btnExecuteBackup.innerHTML;
            btnExecuteBackup.innerHTML = t.MsgExporting; // 🚨 Đã Localize
            btnExecuteBackup.disabled = true;

            window.location.href = '/api/Admin/BackupData';

            setTimeout(() => {
                btnExecuteBackup.innerHTML = originalText;
                btnExecuteBackup.disabled = false;
            }, 2000);
        }
    });
}

// Bọc toàn bộ code bằng DOMContentLoaded
document.addEventListener("DOMContentLoaded", function () {
    
    // NÚT LƯU CẤU HÌNH
    const btnSaveConfig = document.getElementById('btn-save-config');
    
    if (!btnSaveConfig) return;

    btnSaveConfig.addEventListener('click', async function () {
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
        this.innerHTML = t.MsgSaving; 
        this.disabled = true;

        try {
            const response = await fetch('/api/Admin/UpdateSystemConfig', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (result.success) {
                alert("OK " + result.message);
            } else {
                alert(t.MsgError + result.message); 
            }
        } catch (error) {
            alert(t.MsgNetworkError); 
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
            const payload = {
                FullName: document.getElementById('add-user-name').value,
                Email: document.getElementById('add-user-email').value,
                Role: document.getElementById('add-user-role').value,
                Password: document.getElementById('add-user-pass').value
            };

            if (!payload.FullName || !payload.Email || !payload.Password) {
                alert(t.MsgFillAllFields); 
                return;
            }

            const originalText = this.innerHTML;
            this.innerHTML = t.MsgCreating; 
            this.disabled = true;

            try {
                const response = await fetch('/api/Admin/AddUser', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    alert("OK " + result.message);
                    window.location.reload(); 
                } else {
                    alert("NOT OK" + t.MsgError + result.message); // 🚨 Đã Localize
                }
            } catch (error) {
                alert(t.MsgNetworkError); 
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
    if (!confirm(t.MsgConfirmApprove)) { 
        return;
    }

    try {
        const response = await fetch(`/api/Admin/ApproveUser/${userId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (response.ok && result.success) {
            alert("OK " + result.message);
            window.location.reload(); 
        } else {
            alert("NOT OK " + t.MsgError + result.message); 
        }
    } catch (error) {
        alert(t.MsgNetworkError);
        console.error(error);
    }
}

// HÀM BẮN API XÓA NGƯỜI DÙNG
async function deleteUser(userId) {
    if (!confirm(t.MsgConfirmDelete)) { 
        return;
    }

    try {
        const response = await fetch(`/api/Admin/DeleteUser/${userId}`, {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();

        if (response.ok && result.success) {
            alert("OK " + result.message);
            window.location.reload(); 
        } else {
            alert("NOT OK " + t.MsgError + result.message); 
        }
    } catch (error) {
        alert(t.MsgNetworkError); 
        console.error(error);
    }
}

// 9. XỬ LÝ MODAL SỬA NGƯỜI DÙNG (EDIT)
const modalEditUser = document.getElementById('settings-model-edit-user');
const btnCloseEditUser = document.getElementById('close-edit-user');

if (btnCloseEditUser) btnCloseEditUser.addEventListener('click', () => closeModal(modalEditUser));
attachBackdropClose(modalEditUser);

// Thêm 1 biến toàn cục ở đầu file để lưu cái thẻ đang edit
let currentUserCardToEdit = null;

async function openEditModal(userId) {
    try {
        const response = await fetch(`/api/Admin/GetUser/${userId}`);
        const result = await response.json();

        if (response.ok && result.success) {
            const u = result.data;
            
            document.getElementById('edit-user-id').value = u.id;
            document.getElementById('edit-user-name').value = u.fullName;
            document.getElementById('edit-user-email').value = u.email;
            document.getElementById('edit-user-role').value = u.role.toLowerCase();
            document.getElementById('edit-user-active').value = u.isActive ? "true" : "false";

            // LƯU LẠI CÁI THẺ DIV ĐANG EDIT (Dựa vào nút Edit vừa bấm)
            // Có thể dùng event.target để tìm tổ tiên của nút là thẻ .user-card
            if(window.event && window.event.target) {
                currentUserCardToEdit = window.event.target.closest('.user-card');
            }

            openModal(modalEditUser);
        } else {
            alert(t.MsgCannotGetUserInfo + result.message); 
        }
    } catch (error) {
        alert(t.MsgNetworkError); 
        console.error(error);
    }
}

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
                alert(t.MsgNameEmailRequired); 
                return;
            }

            const originalText = this.innerHTML;
            this.innerHTML = t.MsgSaving; 
            this.disabled = true;

            try {
                const response = await fetch('/api/Admin/EditUser', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    alert("OK " + result.message);
                    
                    if(currentUserCardToEdit) {
                        // 1. Đổi data-category để JS phân trang nhận diện lại
                        currentUserCardToEdit.setAttribute('data-category', payload.Role);
                        
                        // 2. Đổi Tên, Email
                        currentUserCardToEdit.querySelector('h6.font-bold').innerText = payload.FullName;
                        currentUserCardToEdit.querySelector('h6.text-gray-500').innerText = payload.Email;
                        
                        // 3. Đổi Avatar chữ cái đầu
                        const firstChar = payload.FullName.substring(0, 1).toUpperCase();
                        const avatarDiv = currentUserCardToEdit.querySelector('.rounded-full');
                        avatarDiv.innerText = firstChar;
                        
                        // 4. Đổi Màu sắc + Text Role
                        const roleSpan = currentUserCardToEdit.querySelector('span:first-child');
                        if (payload.Role === 'instructor') {
                            avatarDiv.classList.replace('bg-blue-500', 'bg-teal-500');
                            roleSpan.className = "px-2 py-1 bg-blue-100 text-blue-700 text-xs font-semibold rounded-md";
                            roleSpan.innerText = "Giáo Viên"; // Hoặc t.TabTeacher
                        } else {
                            avatarDiv.classList.replace('bg-teal-500', 'bg-blue-500');
                            roleSpan.className = "px-2 py-1 bg-gray-100 text-gray-700 text-xs font-semibold rounded-md";
                            roleSpan.innerText = "Học Sinh"; // Hoặc t.TabStudent
                        }
                        
                        // Chạy lại phân trang để nó sắp xếp vào đúng tab
                        paginateUsers();
                    } else {
                        // Backup fallback nếu không tìm thấy thẻ
                        window.location.reload(); 
                    }
                    
                    closeModal(modalEditUser);
                } else {
                    alert("NOT OK " + t.MsgError + result.message); 
                }
            } catch (error) {
                alert(t.MsgNetworkError); 
                console.error(error);
            } finally {
                this.innerHTML = originalText;
                this.disabled = false;
            }
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const btnCheckDb = document.getElementById('btn-check-db');

    if (btnCheckDb) {
        btnCheckDb.addEventListener('click', async function () {
            const payload = {
                Host: document.getElementById('conf-db-host').value,
                Port: parseInt(document.getElementById('conf-db-port').value) || 5432,
                DatabaseName: document.getElementById('conf-db-name').value,
                DatabaseUser: document.getElementById('conf-db-user').value,
                Password: document.getElementById('conf-db-pass').value
            };

            const originalText = this.innerHTML;
            this.innerHTML = t.MsgPinging; 
            this.disabled = true;

            try {
                const response = await fetch('/api/Admin/CheckDbConnection', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    alert(t.MsgDbConnectSuccess); 
                } else {
                    alert(t.MsgDbConfigError + result.message); 
                }
            } catch (error) {
                alert(t.MsgSystemNotResponding); 
                console.error(error);
            } finally {
                this.innerHTML = originalText;
                this.disabled = false;
            }
        });
    }
});

const btnTestEmail = document.getElementById('btn-test-email');

if (btnTestEmail) {
    btnTestEmail.addEventListener('click', async function () {
        const testEmailAddress = prompt(t.MsgPromptEmail);
        if (!testEmailAddress) return; 

        const payload = {
            SmtpHost: document.getElementById('conf-email-host').value,
            SmtpPort: parseInt(document.getElementById('conf-email-port').value) || 587,
            SmtpUser: document.getElementById('conf-email-user').value,
            SmtpPassword: document.getElementById('conf-email-pass').value, 
            ToEmail: testEmailAddress
        };

        if(!payload.SmtpUser || !payload.SmtpPassword) {
            alert(t.MsgRequireSmtp); 
            return;
        }

        const originalText = this.innerHTML;
        this.innerHTML = t.MsgSendingMail; 
        this.disabled = true;

        try {
            const response = await fetch('/api/Admin/TestEmailConnection', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (response.ok && result.success) {
                alert("OK " + result.message);
            } else {
                alert(t.MsgSendMailError + result.message); 
            }
        } catch (error) {
            alert(t.MsgNetworkError); 
            console.error(error);
        } finally {
            this.innerHTML = originalText;
            this.disabled = false;
        }
    });
}

// TÍNH NĂNG PHÂN TRANG (PAGINATION) BẰNG JS
const itemsPerPage = 4; 
let currentPage = 1;
const paginationContainer = document.getElementById('user-pagination');

function paginateUsers() {
    if (!paginationContainer) return;

    const activeTabBtn = document.querySelector('.tab-btn.active-tab');
    if (!activeTabBtn) return;
    const currentTab = activeTabBtn.getAttribute('data-tab');

    let visibleCards = [];
    allCards.forEach(card => {
        const category = card.getAttribute('data-category');
        if (currentTab === 'all' || currentTab === 'superall' || category.includes(currentTab)) {
            visibleCards.push(card);
        } else {
            card.style.display = 'none'; 
        }
    });

    const totalPages = Math.ceil(visibleCards.length / itemsPerPage);
    paginationContainer.innerHTML = ''; 

    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;

    visibleCards.forEach((card, index) => {
        if (index >= startIndex && index < endIndex) {
            card.style.display = 'flex'; 
        } else {
            card.style.display = 'none'; 
        }
    });

    if (totalPages > 1) {
        for (let i = 1; i <= totalPages; i++) {
            const btn = document.createElement('button');
            btn.innerText = i;
            
            btn.className = `w-8 h-8 rounded-lg font-semibold text-sm transition-colors ${
                i === currentPage
                    ? 'bg-blue-600 text-white shadow-md' 
                    : 'bg-white text-gray-600 border border-gray-200 hover:bg-gray-100' 
            }`;

            btn.addEventListener('click', () => {
                currentPage = i;
                paginateUsers(); 
            });

            paginationContainer.appendChild(btn);
        }
    }
}

paginateUsers();

// TÍNH NĂNG XUẤT PDF
document.addEventListener("DOMContentLoaded", function () {
    const btnExportPdf = document.getElementById('btn-export-pdf');

    if (btnExportPdf) {
        btnExportPdf.addEventListener('click', function () {
            const targetElement = document.getElementById('export-zone'); 
            const scrollContainer = targetElement.closest('.overflow-y-auto');

            if (!targetElement) {
                alert(t.MsgDomError);
                return;
            }

            const originalText = this.innerText;
            this.innerText = t.MsgCreatingReport; 
            this.disabled = true;

            const oldMaxHeight = scrollContainer.style.maxHeight;
            const oldOverflow = scrollContainer.style.overflowY;
            
            scrollContainer.style.maxHeight = 'none';      
            scrollContainer.style.overflowY = 'visible';   

            html2canvas(targetElement, { 
                scale: 2, 
                backgroundColor: '#ffffff' 
            }).then(canvas => {
                scrollContainer.style.maxHeight = oldMaxHeight;
                scrollContainer.style.overflowY = oldOverflow;

                const base64Image = canvas.toDataURL('image/png');

                fetch('/api/Admin/ExportReportToPdf', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ ImageData: base64Image })
                })
                .then(response => {
                    if (!response.ok) throw new Error("Lỗi HTTP: " + response.status);
                    return response.blob(); 
                })
                .then(blob => {
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = `BaoCao_Dashboard_${new Date().getTime()}.pdf`;
                    document.body.appendChild(a);
                    a.click();
                    a.remove();
                    
                    this.innerText = originalText;
                    this.disabled = false;
                })
                .catch(err => {
                    console.error("Lỗi: ", err);
                    alert(t.MsgPdfExportError); 
                    this.innerText = originalText;
                    this.disabled = false;
                });
            });
        });
    }
});
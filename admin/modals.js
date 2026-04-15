/**
 * Custom Modal System V2 for Admin Panel
 * Premium Dark Aesthetic with Icons & Animations
 */

const Modal = {
    overlay: null,
    content: null,

    init() {
        if (document.getElementById('modal-overlay')) return;

        const html = `
            <div id="modal-overlay" class="modal-overlay">
                <div class="modal-content">
                    <div id="modal-icon" class="modal-icon-container">
                        <i class="fas fa-question-circle"></i>
                    </div>
                    <div id="modal-title" class="modal-title"></div>
                    <div id="modal-body" class="modal-body"></div>
                    <div id="modal-input-container" style="display: none; margin-bottom: 1rem;">
                        <input type="text" id="modal-input" class="modal-input" placeholder="Buraya yazın...">
                    </div>
                    <div id="modal-select-container" style="display: none; margin-bottom: 1.5rem;">
                        <select id="modal-unit-select" class="modal-input" style="cursor: pointer;">
                            <option value="1">Dakika</option>
                            <option value="60">Saat</option>
                            <option value="1440">Gün</option>
                        </select>
                    </div>
                    <div class="modal-footer">
                        <button id="modal-confirm" class="primary-btn modal-confirm-btn">ONAYLA</button>
                        <button id="modal-cancel" class="modal-cancel-btn">İPTAL EDİLSİN</button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', html);
        this.overlay = document.getElementById('modal-overlay');
        this.content = this.overlay.querySelector('.modal-content');
    },

    show({ title, message, icon = "fa-question-circle", color = "#f0abfc", showInput = false, showUnitSelect = false, inputPlaceholder = "", confirmText = "ONAYLA", cancelText = "İPTAL EDİLSİN", onConfirm, onCancel }) {
        this.init();

        const iconEl = document.getElementById('modal-icon');
        const titleEl = document.getElementById('modal-title');
        const bodyEl = document.getElementById('modal-body');
        const inputContainer = document.getElementById('modal-input-container');
        const selectContainer = document.getElementById('modal-select-container');
        const input = document.getElementById('modal-input');
        const unitSelect = document.getElementById('modal-unit-select');
        const confirmBtn = document.getElementById('modal-confirm');
        const cancelBtn = document.getElementById('modal-cancel');

        iconEl.innerHTML = `<i class="fas ${icon}"></i>`;
        iconEl.style.color = color;
        titleEl.innerHTML = title;
        bodyEl.innerHTML = message;
        confirmBtn.innerText = confirmText;
        confirmBtn.style.display = 'inline-block'; // Her zaman göster (selectionPrompt özel gizleyebilir)
        cancelBtn.innerText = cancelText;

        if (showInput) {
            inputContainer.style.display = 'block';
            input.value = '';
            input.placeholder = inputPlaceholder;
            setTimeout(() => input.focus(), 100);
        } else {
            inputContainer.style.display = 'none';
        }

        if (showUnitSelect) {
            selectContainer.style.display = 'block';
        } else {
            selectContainer.style.display = 'none';
        }

        this.overlay.classList.remove('closing');
        this.overlay.classList.add('active');

        // Cleanup previous listeners
        const newConfirmBtn = confirmBtn.cloneNode(true);
        const newCancelBtn = cancelBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);
        cancelBtn.parentNode.replaceChild(newCancelBtn, cancelBtn);

        newConfirmBtn.onclick = () => {
            const val = input.value;
            this.hide(() => {
                if (onConfirm) onConfirm(val);
            });
        };

        newCancelBtn.onclick = () => {
            this.hide(() => {
                if (onCancel) onCancel();
            });
        };

        this.overlay.onclick = (e) => {
            if (e.target === this.overlay) {
                this.hide();
                if (onCancel) onCancel();
            }
        };
    },

    confirm(title, message, onConfirm) {
        this.show({
            title,
            message,
            icon: "fa-shield-halved",
            color: "#22d3ee",
            onConfirm
        });
    },

    prompt(title, message, placeholder, onConfirm) {
        this.show({
            title,
            message,
            icon: "fa-user-lock",
            color: "#f43f5e",
            showInput: true,
            inputPlaceholder: placeholder,
            onConfirm
        });
    },

    mutePrompt(title, message, placeholder, onConfirm) {
        this.show({
            title,
            message,
            icon: "fa-microphone-slash",
            color: "#fbbf24",
            showInput: true,
            showUnitSelect: true,
            inputPlaceholder: placeholder,
            confirmText: "SUSTUR",
            onConfirm: (val) => {
                const multiplier = parseInt(document.getElementById('modal-unit-select').value);
                const totalMinutes = parseInt(val) * multiplier;
                if (onConfirm) onConfirm(totalMinutes);
            }
        });
    },

    selectionPrompt(title, message, options, onSelect) {
        let content = `<div style="margin-bottom: 1.5rem; text-align: center; color: var(--text-secondary);">${message}</div>`;
        content += `<div style="display: flex; flex-direction: column; gap: 0.75rem;">`;
        options.forEach(opt => {
            content += `
                <button class="selection-btn" onclick="window.Modal.handleSelect('${opt.id}', '${opt.name}')">
                    <span style="font-weight: 700;">${opt.name}</span>
                    ${opt.id ? `<span style="font-size: 0.7rem; opacity: 0.5;">ID: ${opt.id}</span>` : `<span style="font-size: 0.7rem; color: #facc15;">ID Bulunamadı</span>`}
                </button>
            `;
        });
        content += `</div>`;

        this.show({
            title,
            message: content,
            icon: "fa-users-cog",
            color: "#8b5cf6",
            confirmText: "İPTAL", // Seçim iptali için buton
            onConfirm: () => {} // Onay butonu iptal niyetine çalışacak
        });

        // Footer'daki onay butonunu gizle (isteğe bağlı, ama genelde seçim yaptıkça ilerliyor)
        document.getElementById('modal-confirm').style.display = 'none';
        this.onSelectCallback = onSelect;
    },

    eventSelectionPrompt(title, message, options, onSelect) {
        let content = `<div style="margin-bottom: 2rem; text-align: center; color: var(--text-secondary); line-height:1.5;">${message}</div>`;
        content += `<div class="event-selection-grid">`;
        options.forEach(opt => {
            content += `
                <div class="event-selection-card" onclick="window.Modal.handleEventSelect('${opt.type}', '${opt.name}')" style="--card-accent: ${opt.color}">
                    <div class="event-sel-icon">
                        <i class="fas ${opt.icon}"></i>
                    </div>
                    <div class="event-sel-content">
                        <div class="event-sel-name">${opt.name}</div>
                        <div class="event-sel-desc">${opt.desc}</div>
                    </div>
                </div>
            `;
        });
        content += `
            <button class="load-more-btn" onclick="showToast('Tüm etkinlik türleri yüklendi.', 'info')">
                <i class="fas fa-plus"></i> DAHA FAZLA ETKİNLİK YÜKLE
            </button>
        </div>`;

        this.show({
            title,
            message: content,
            icon: "fa-wand-magic-sparkles",
            color: "#f472b6",
            confirmText: "İPTAL"
        });

        document.getElementById('modal-confirm').style.display = 'none';
        this.onEventSelectCallback = onSelect;
    },

    handleEventSelect(type, name) {
        const callback = this.onEventSelectCallback;
        this.hide(() => {
            if (callback) callback(type, name);
        });
    },

    handleSelect(id, name) {
        const callback = this.onSelectCallback;
        this.hide(() => {
            if (callback) callback(id, name);
        });
    },

    hide(callback) {
        if (this.overlay) {
            this.overlay.classList.add('closing');
            setTimeout(() => {
                this.overlay.classList.remove('active');
                this.overlay.classList.remove('closing');
                if (callback) callback();
            }, 400); // Wait for animation
        }
    }
};

window.Modal = Modal;

// Global Toast System
window.showToast = function (message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    let icon = 'fa-check-circle';
    if (type === 'error') icon = 'fa-exclamation-circle';
    if (type === 'warning') icon = 'fa-triangle-exclamation';

    toast.innerHTML = `<i class="fas ${icon} toast-icon"></i><div class="toast-content">${message}</div>`;
    container.appendChild(toast);

    // Auto remove
    setTimeout(() => {
        toast.style.animation = 'toastOut 0.5s cubic-bezier(0.19, 1, 0.22, 1) forwards';
        setTimeout(() => toast.remove(), 500);
    }, 3500);
};

// Global Logout
async function logout() {
    try {
        const res = await fetch('/api/auth/logout', { method: 'POST' });
        const data = await res.json();
        if (data.success) {
            window.location.href = '/login.html';
        }
    } catch (e) {
        window.location.href = '/login.html';
    }
}

// Global Auth Check (API bazlı doğrula)
async function checkAuth() {
    if (window.location.pathname.endsWith('login.html')) return;

    try {
        const res = await fetch('/api/auth/check');
        const data = await res.json();
        if (!data.authorized) {
            window.location.href = '/login.html';
        }
    } catch (e) {
        // Hata durumunda sessizce devam et
    }
}

// Sayfa yüklendiğinde kontrol et
document.addEventListener('DOMContentLoaded', checkAuth);

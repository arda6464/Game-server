import os

file_path = r"c:\Project\Game-server\admin\profile.html"

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    new_lines.append(line)
    if "modActions.appendChild(kickBtn);" in line:
        indent = line[:line.find("modActions")]
        new_lines.append("\n")
        new_lines.append(f"{indent}const msgBtn = document.createElement('button');\n")
        new_lines.append(f"{indent}msgBtn.className = 'btn-premium';\n")
        new_lines.append(f"{indent}msgBtn.style.background = 'rgba(168, 85, 247, 0.1)';\n")
        new_lines.append(f"{indent}msgBtn.style.color = '#a855f7';\n")
        new_lines.append(f"{indent}msgBtn.style.borderColor = 'rgba(168, 85, 247, 0.2)';\n")
        new_lines.append(f"{indent}msgBtn.innerHTML = '<i class=\"fas fa-paper-plane\"></i> MESAJ GÖNDER';\n")
        new_lines.append(f"{indent}msgBtn.onclick = () => sendMessage(p.id);\n")
        new_lines.append(f"{indent}modActions.appendChild(msgBtn);\n")

# Also add the sendMessage function
final_lines = []
for line in new_lines:
    final_lines.append(line)
    if "async function kickPlayer(id) {" in line:
        # Find the end of kickPlayer function
        pass # Wait, I'll just append it before logout

for i in range(len(final_lines)):
    if "function logout() {" in final_lines[i]:
        final_lines.insert(i, """        async function sendMessage(id) {
            const html = `
                <div style="text-align: left;">
                    <label class="modal-label">Mesaj Başlığı</label>
                    <input type="text" id="msg-title" class="modal-input" value="SİSTEM MESAJI" style="margin-bottom: 1rem;">
                    <label class="modal-label">Mesaj İçeriği</label>
                    <textarea id="msg-content" class="modal-input" style="height: 120px; resize: none; font-family: inherit; width: 100%; box-sizing: border-box;"></textarea>
                </div>
            `;

            Modal.show({
                title: 'Özel Mesaj Gönder',
                message: html,
                icon: 'fa-paper-plane',
                color: '#a855f7',
                confirmText: 'GÖNDER',
                onConfirm: async () => {
                    const title = document.getElementById('msg-title').value;
                    const message = document.getElementById('msg-content').value;

                    if (!message) {
                        showToast('Mesaj içeriği boş olamaz!', 'error');
                        return;
                    }

                    try {
                        const res = await fetch('/api/player/send-message', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ id, title, message })
                        });
                        const result = await res.json();
                        showToast(result.message, result.success ? 'success' : 'error');
                        loadProfile();
                    } catch (e) { showToast('Hata: ' + e.message, 'error'); }
                }
            });
        }

""")
        break

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(final_lines)

print("Successfully updated profile.html")

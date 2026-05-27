import os

file_path = r"c:\Project\Game-server\admin\market.html"

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    # 1. Update Header
    if '<h1 class="page-title">Market & Ekonomi Analizi</h1>' in line:
        indent = line[:line.find('<h1')]
        new_lines.append(f"{indent}<div style=\"display: flex; justify-content: space-between; align-items: center; width: 100%;\">\n")
        new_lines.append(f"{indent}    <div>\n")
        new_lines.append(line)
    elif '<p style="color: var(--text-secondary); margin-top: -1rem;">Sunucu genelindeki varlık dağılımı ve ekonomi' in line:
        new_lines.append(line)
        indent = line[:line.find('<p')]
        new_lines.append(f"{indent}    </div>\n")
        new_lines.append(f"{indent}    <div style=\"display: flex; gap: 0.75rem; align-items: center; background: rgba(255,255,255,0.03); padding: 0.75rem 1.25rem; border-radius: 1.25rem; border: 1px solid var(--glass-border);\">\n")
        new_lines.append(f"{indent}        <div style=\"position: relative;\">\n")
        new_lines.append(f"{indent}            <i class=\"fas fa-user-search\" style=\"position: absolute; left: 1rem; top: 50%; transform: translateY(-50%); color: var(--text-secondary);\"></i>\n")
        new_lines.append(f"{indent}            <input type=\"number\" id=\"player-id-input\" placeholder=\"Oyuncu ID (Opsiyonel)\" style=\"padding: 0.6rem 1rem 0.6rem 2.8rem; background: rgba(0,0,0,0.2); border: 1px solid var(--glass-border); border-radius: 0.75rem; color: #fff; width: 180px; font-weight: 700;\">\n")
        new_lines.append(f"{indent}        </div>\n")
        new_lines.append(f"{indent}        <button onclick=\"fetchData()\" class=\"primary-btn\" style=\"padding: 0.6rem 1.5rem; border-radius: 0.75rem; background: var(--accent-fuchsia);\">GÖRÜNTÜLE</button>\n")
        new_lines.append(f"{indent}        <button onclick=\"clearPlayerFilter()\" class=\"secondary-btn\" style=\"padding: 0.6rem; border-radius: 0.75rem;\" title=\"Filtreyi Temizle\"><i class=\"fas fa-times\"></i></button>\n")
        new_lines.append(f"{indent}    </div>\n")
        new_lines.append(f"{indent}</div>\n")
        # Skip the original p tag closing if needed? No, I added the closing div before.
    
    # 2. Add Player Info Card area
    elif '<div class="market-grid">' in line:
        indent = line[:line.find('<div')]
        new_lines.append(f"{indent}<div id=\"player-view-info\" class=\"card animate-slide-up\" style=\"display:none; grid-column: span 2; margin-bottom: 2rem; background: linear-gradient(90deg, rgba(168, 85, 247, 0.1), transparent); border: 1px solid rgba(168, 85, 247, 0.2);\">\n")
        new_lines.append(f"{indent}    <div style=\"display:flex; justify-content:space-between; align-items:center;\">\n")
        new_lines.append(f"{indent}        <div style=\"display:flex; align-items:center; gap:1.5rem;\">\n")
        new_lines.append(f"{indent}            <div style=\"width:50px; height:50px; background:var(--accent-fuchsia); border-radius:50%; display:flex; align-items:center; justify-content:center; color:#fff; font-size:1.5rem;\"><i class=\"fas fa-user-astronaut\"></i></div>\n")
        new_lines.append(f"{indent}            <div>\n")
        new_lines.append(f"{indent}                <div style=\"font-size:0.8rem; color:var(--text-secondary); font-weight:700;\">OYUNCU MARKETİ GÖRÜNTÜLENİYOR</div>\n")
        new_lines.append(f"{indent}                <div style=\"font-size:1.5rem; font-weight:900;\" id=\"view-player-name\">...</div>\n")
        new_lines.append(f"{indent}            </div>\n")
        new_lines.append(f"{indent}        </div>\n")
        new_lines.append(f"{indent}        <div style=\"display:flex; gap:2rem;\">\n")
        new_lines.append(f"{indent}            <div style=\"text-align:right;\"><div style=\"font-size:0.7rem; color:var(--text-secondary);\">ELMAS</div><div style=\"font-size:1.2rem; font-weight:800; color:var(--accent-cyan);\" id=\"view-player-gems\">0</div></div>\n")
        new_lines.append(f"{indent}            <div style=\"text-align:right;\"><div style=\"font-size:0.7rem; color:var(--text-secondary);\">ALTIN</div><div style=\"font-size:1.2rem; font-weight:800; color:#fbbf24;\" id=\"view-player-coins\">0</div></div>\n")
        new_lines.append(f"{indent}        </div>\n")
        new_lines.append(f"{indent}    </div>\n")
        new_lines.append(f"{indent}</div>\n")
        new_lines.append(line)
    else:
        new_lines.append(line)

# 3. Update JS functions
final_lines = []
for line in new_lines:
    if "async function fetchData() {" in line:
        final_lines.append("        function clearPlayerFilter() {\n")
        final_lines.append("            document.getElementById('player-id-input').value = '';\n")
        final_lines.append("            fetchData();\n")
        final_lines.append("        }\n\n")
        final_lines.append("        async function fetchData() {\n")
        final_lines.append("            const playerId = document.getElementById('player-id-input').value;\n")
        final_lines.append("            const url = playerId ? `/api/market/all?id=${playerId}` : '/api/market/all';\n")
    elif "const resMkt = await fetch('/api/market/all');" in line:
        final_lines.append(f"                const resMkt = await fetch(url);\n")
    elif "renderMarket(dataMkt);" in line:
        final_lines.append("                if (dataMkt.success === false) {\n")
        final_lines.append("                    showToast(dataMkt.message, 'error');\n")
        final_lines.append("                    return;\n")
        final_lines.append("                }\n")
        final_lines.append("                renderMarket(dataMkt);\n")
    elif "function renderMarket(data) {" in line:
        final_lines.append(line)
        final_lines.append("            const infoBox = document.getElementById('player-view-info');\n")
        final_lines.append("            if (data.player) {\n")
        final_lines.append("                infoBox.style.display = 'block';\n")
        final_lines.append("                document.getElementById('view-player-name').innerText = data.player.username + ' (#' + data.player.id + ')';\n")
        final_lines.append("                document.getElementById('view-player-gems').innerText = data.player.gems.toLocaleString();\n")
        final_lines.append("                document.getElementById('view-player-coins').innerText = data.player.coins.toLocaleString();\n")
        final_lines.append("            } else {\n")
        final_lines.append("                infoBox.style.display = 'none';\n")
        final_lines.append("            }\n")
    # Add personal label logic to renderMarket
    elif "<td>${offer.OfferType == 4 ? 'Flash Sale' : 'Özel Teklif'}</td>" in line:
        final_lines.append("                    <td>${offer.IsPersonal ? '<span class=\"status-pill\" style=\"background:rgba(168, 85, 247, 0.1); color:#a855f7; font-size:0.6rem;\">KİŞİYE ÖZEL</span> ' : ''}${offer.OfferType == 4 ? 'Flash Sale' : 'Teklif'}</td>\n")
    else:
        final_lines.append(line)

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(final_lines)

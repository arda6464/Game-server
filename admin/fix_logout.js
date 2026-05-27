const fs = require('fs');
const path = require('path');
const dir = __dirname;
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));

files.forEach(f => {
    const filePath = path.join(dir, f);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Check if it has the bug: Çıkış Yap followed directly by </nav>
    const regex = /<i class="fas fa-sign-out-alt"><\/i> Çıkış Yap\s*<\/nav>/g;
    const newContent = content.replace(regex, '<i class="fas fa-sign-out-alt"></i> Çıkış Yap</a>\n        </nav>');
    
    if (content !== newContent) {
        fs.writeFileSync(filePath, newContent, 'utf8');
        console.log('Fixed ' + f);
    }
});

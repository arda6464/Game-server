using System.Text;

public static class InvitePageBuilder
{
    public static string BuildValidInvite(string ownerName, string typeStr, string token)
    {
        return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Davet Edildin! - Oyun Daveti</title>
    <link href='https://fonts.googleapis.com/css2?family=Outfit:wght@400;700&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary: #00ffa3;
            --secondary: #bc00ff;
            --bg: #0f172a;
            --card-bg: rgba(30, 41, 59, 0.7);
        }}
        
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: 'Outfit', sans-serif; }}
        
        body {{
            background: #0f172a;
            background: radial-gradient(circle at top right, #1e1b4b, #0f172a);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            color: white;
            overflow: hidden;
        }}

        .background-blob {{
            position: absolute;
            width: 500px;
            height: 500px;
            background: linear-gradient(45deg, var(--primary), var(--secondary));
            filter: blur(150px);
            opacity: 0.15;
            z-index: 0;
            animation: move 20s infinite alternate;
        }}

        @keyframes move {{
            from {{ transform: translate(-20%, -20%); }}
            to {{ transform: translate(20%, 20%); }}
        }}

        .card {{
            background: var(--card-bg);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            padding: 3rem;
            border-radius: 2rem;
            text-align: center;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            max-width: 450px;
            width: 90%;
            z-index: 10;
            animation: fadeIn 0.8s ease-out;
        }}

        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}

        .avatar-container {{
            width: 100px;
            height: 100px;
            margin: 0 auto 1.5rem;
            background: linear-gradient(45deg, var(--primary), var(--secondary));
            padding: 4px;
            border-radius: 50%;
            box-shadow: 0 0 20px rgba(0, 255, 163, 0.3);
        }}

        .avatar {{
            width: 100%;
            height: 100%;
            background: #1e293b;
            border-radius: 50%;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 2.5rem;
        }}

        h1 {{ font-size: 2rem; margin-bottom: 0.5rem; background: linear-gradient(to right, #fff, #94a3b8); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }}
        .invite-text {{ color: #94a3b8; line-height: 1.6; margin-bottom: 1.5rem; }}
        .invite-text span {{ color: var(--primary); font-weight: bold; }}

        #status-text {{ font-size: 0.9rem; color: var(--primary); margin-bottom: 1.5rem; min-height: 1.2rem; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; }}

        .btn {{
            display: block;
            background: linear-gradient(45deg, var(--primary), #00d4ff);
            color: #0f172a;
            padding: 1.2rem;
            border-radius: 1rem;
            text-decoration: none;
            font-weight: 700;
            font-size: 1.1rem;
            transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
            box-shadow: 0 10px 15px -3px rgba(0, 255, 163, 0.4);
            text-transform: uppercase;
            letter-spacing: 1px;
        }}

        .btn:hover {{ transform: scale(1.05); box-shadow: 0 20px 25px -5px rgba(0, 255, 163, 0.5); }}
        
        .pulse {{ animation: btnPulse 1.5s infinite; }}
        @keyframes btnPulse {{
            0% {{ transform: scale(1); }}
            50% {{ transform: scale(1.05); box-shadow: 0 0 30px rgba(0, 255, 163, 0.6); }}
            100% {{ transform: scale(1); }}
        }}

        .footer {{ margin-top: 2rem; font-size: 0.85rem; color: #64748b; }}
        .footer a {{ color: var(--primary); text-decoration: none; }}
    </style>
</head>
<body>
    <div class='background-blob'></div>
    <div class='card'>
        <div class='avatar-container'>
            <div class='avatar'>🎮</div>
        </div>
        <h1>Davet Edildin!</h1>
        <p class='invite-text'>
            <span>{ownerName}</span> seni bir <span>{typeStr}</span> davetine çağırdı. Heyecana ortak olmak için hemen katıl!
        </p>
        <div id='status-text'>Bağlantı hazırlanıyor...</div>
        <a href='com.ardagamedevtest://invite/{token}' id='join-btn' class='btn'>OYUNA KATIL</a>
        <p class='footer'>Eğer oyun yüklü değilse <a href='#'>buradan</a> indirebilirsin.</p>
    </div>

    <script>
        const url = 'oyun://invite/{token}';
        const statusText = document.getElementById('status-text');
        const joinBtn = document.getElementById('join-btn');
        
        let countdown = 3;
        
        function startAutoJoin() {{
            const timer = setInterval(() => {{
                countdown--;
                if (countdown <= 0) {{
                    clearInterval(timer);
                    statusText.innerText = 'Oyun başlatılıyor...';
                    window.location.href = url;
                    joinBtn.classList.add('pulse');
                    
                    setTimeout(() => {{
                        statusText.innerText = 'Oyun açılmadı mı? Yukarıdaki butona tıklayın.';
                    }}, 5000);
                }} else {{
                    statusText.innerText = countdown + ' saniye içinde otomatik katılınıyor...';
                }}
            }}, 1000);
        }}
        
        window.onload = () => {{
            setTimeout(startAutoJoin, 500);
        }};
    </script>
</body>
</html>";
    }

    public static string BuildInvalidInvite()
    {
        return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Geçersiz Davet - Oyun Daveti</title>
    <link href='https://fonts.googleapis.com/css2?family=Outfit:wght@400;700&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary: #ff4b2b;
            --secondary: #ff416c;
            --bg: #0f172a;
            --card-bg: rgba(30, 41, 59, 0.7);
        }}
        
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: 'Outfit', sans-serif; }}
        
        body {{
            background: #0f172a;
            background: radial-gradient(circle at top right, #1e1b4b, #0f172a);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            color: white;
            overflow: hidden;
        }}

        .card {{
            background: var(--card-bg);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            padding: 3rem;
            border-radius: 2rem;
            text-align: center;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            max-width: 450px;
            width: 90%;
            z-index: 10;
        }}

        .icon {{ font-size: 4rem; margin-bottom: 1rem; color: var(--primary); }}
        h1 {{ font-size: 1.8rem; margin-bottom: 1rem; color: white; }}
        .text {{ color: #94a3b8; line-height: 1.6; margin-bottom: 2rem; }}

        .btn {{
            display: inline-block;
            background: rgba(255, 255, 255, 0.1);
            color: white;
            padding: 1rem 2rem;
            border-radius: 1rem;
            text-decoration: none;
            font-weight: 700;
            border: 1px solid rgba(255, 255, 255, 0.2);
            transition: all 0.3s;
        }}

        .btn:hover {{ background: rgba(255, 255, 255, 0.2); }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='icon'>⚠️</div>
        <h1>Davet Geçersiz</h1>
        <p class='text'>Bu davet linkinin süresi dolmuş veya iptal edilmiş olabilir. Lütfen arkadaşından yeni bir davet iste.</p>
        <a href='/' class='btn'>ANA SAYFAYA DÖN</a>
    </div>
</body>
</html>";
    }
}

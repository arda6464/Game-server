using System;
using System.Text;

public static class PortfolioPageBuilder
{
    public static string Build(string name, string role, string location, string experienceSummary, string languagesSummary)
    {
        string safeName = Escape(name);
        string safeRole = Escape(role);
        string safeLocation = Escape(location);
        string safeExperience = Escape(experienceSummary);
        string safeLanguages = Escape(languagesSummary);

        return $@"<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta name='color-scheme' content='dark'>
    <title>{safeName} | Portfolio</title>
    <style>
        :root {{
            --bg: #050816;
            --bg-soft: #0a1024;
            --card: rgba(9, 15, 35, 0.72);
            --card-strong: rgba(16, 23, 48, 0.88);
            --line: rgba(175, 205, 255, 0.12);
            --text: #eef4ff;
            --muted: #9db0da;
            --muted-2: #6f80a8;
            --accent: #7cf7d4;
            --accent-2: #74a7ff;
            --accent-3: #ff8ad6;
            --shadow: 0 30px 80px rgba(0, 0, 0, 0.45);
        }}

        * {{
            box-sizing: border-box;
        }}

        html {{
            scroll-behavior: smooth;
        }}

        body {{
            margin: 0;
            min-height: 100vh;
            color: var(--text);
            background:
                radial-gradient(circle at 15% 15%, rgba(116, 167, 255, 0.18), transparent 28%),
                radial-gradient(circle at 85% 12%, rgba(124, 247, 212, 0.12), transparent 24%),
                radial-gradient(circle at 70% 80%, rgba(255, 138, 214, 0.10), transparent 24%),
                linear-gradient(180deg, #030611 0%, var(--bg) 48%, #030611 100%);
            font-family: 'Bahnschrift', 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif;
            overflow-x: hidden;
        }}

        body::before {{
            content: '';
            position: fixed;
            inset: 0;
            pointer-events: none;
            background-image:
                linear-gradient(rgba(255,255,255,0.03) 1px, transparent 1px),
                linear-gradient(90deg, rgba(255,255,255,0.03) 1px, transparent 1px);
            background-size: 72px 72px;
            mask-image: radial-gradient(circle at center, black 42%, transparent 100%);
            opacity: 0.35;
        }}

        .noise {{
            position: fixed;
            inset: 0;
            pointer-events: none;
            opacity: 0.07;
            background-image:
                linear-gradient(transparent 0 96%, rgba(255,255,255,0.5) 96% 100%);
            background-size: 100% 3px;
            mix-blend-mode: screen;
        }}

        .cursor-glow {{
            position: fixed;
            inset: 0 auto auto 0;
            width: 520px;
            height: 520px;
            border-radius: 50%;
            pointer-events: none;
            background: radial-gradient(circle, rgba(124, 247, 212, 0.18), rgba(116, 167, 255, 0.09) 35%, transparent 70%);
            filter: blur(30px);
            transform: translate(-50%, -50%);
            opacity: 0.8;
            transition: opacity 0.2s ease;
        }}

        .orb {{
            position: fixed;
            border-radius: 50%;
            filter: blur(18px);
            opacity: 0.55;
            pointer-events: none;
            animation: drift 18s ease-in-out infinite alternate;
        }}

        .orb.one {{
            width: 380px;
            height: 380px;
            top: -100px;
            left: -80px;
            background: radial-gradient(circle at 30% 30%, rgba(124,247,212,0.45), rgba(124,247,212,0.08) 55%, transparent 72%);
        }}

        .orb.two {{
            width: 360px;
            height: 360px;
            top: 12vh;
            right: -120px;
            background: radial-gradient(circle at 30% 30%, rgba(116,167,255,0.40), rgba(116,167,255,0.10) 58%, transparent 75%);
            animation-duration: 22s;
        }}

        .orb.three {{
            width: 460px;
            height: 460px;
            bottom: -180px;
            left: 34%;
            background: radial-gradient(circle at 40% 40%, rgba(255,138,214,0.32), rgba(255,138,214,0.08) 58%, transparent 76%);
            animation-duration: 26s;
        }}

        @keyframes drift {{
            from {{ transform: translate3d(-10px, -20px, 0) scale(1); }}
            to {{ transform: translate3d(18px, 24px, 0) scale(1.05); }}
        }}

        .wrap {{
            width: min(1220px, calc(100% - 40px));
            margin: 0 auto;
            position: relative;
            z-index: 1;
        }}

        .topbar {{
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 26px 0;
            gap: 16px;
        }}

        .brand {{
            display: inline-flex;
            align-items: center;
            gap: 14px;
            text-decoration: none;
            color: var(--text);
        }}

        .sigil {{
            width: 48px;
            height: 48px;
            border-radius: 16px;
            background: linear-gradient(135deg, rgba(124,247,212,0.22), rgba(116,167,255,0.25));
            border: 1px solid rgba(255,255,255,0.16);
            box-shadow: inset 0 1px 0 rgba(255,255,255,0.16), 0 12px 30px rgba(0,0,0,0.28);
            display: grid;
            place-items: center;
            font-size: 1.05rem;
            font-weight: 800;
            letter-spacing: 0.15em;
        }}

        .brand strong {{
            display: block;
            font-size: 0.98rem;
            letter-spacing: 0.14em;
            text-transform: uppercase;
        }}

        .brand span {{
            color: var(--muted);
            font-size: 0.9rem;
        }}

        .nav {{
            display: flex;
            gap: 14px;
            flex-wrap: wrap;
            justify-content: flex-end;
        }}

        .nav a {{
            color: var(--muted);
            text-decoration: none;
            font-size: 0.92rem;
            padding: 10px 14px;
            border: 1px solid transparent;
            border-radius: 999px;
            transition: 0.25s ease;
        }}

        .nav a:hover {{
            color: var(--text);
            border-color: rgba(255,255,255,0.12);
            background: rgba(255,255,255,0.04);
        }}

        .hero {{
            display: grid;
            grid-template-columns: 1.3fr 0.9fr;
            gap: 22px;
            align-items: stretch;
            padding: 22px 0 30px;
        }}

        .hero-card, .panel {{
            background: linear-gradient(180deg, rgba(255,255,255,0.08), rgba(255,255,255,0.03));
            border: 1px solid rgba(255,255,255,0.10);
            box-shadow: var(--shadow);
            backdrop-filter: blur(18px);
            -webkit-backdrop-filter: blur(18px);
        }}

        .hero-card {{
            border-radius: 34px;
            padding: 34px;
            position: relative;
            overflow: hidden;
            min-height: 520px;
        }}

        .hero-card::after {{
            content: '';
            position: absolute;
            inset: auto -10% -18% auto;
            width: 360px;
            height: 360px;
            background: radial-gradient(circle, rgba(124,247,212,0.14), transparent 65%);
            transform: rotate(-20deg);
            pointer-events: none;
        }}

        .eyebrow {{
            display: inline-flex;
            align-items: center;
            gap: 10px;
            padding: 10px 14px;
            border: 1px solid rgba(255,255,255,0.12);
            border-radius: 999px;
            color: var(--muted);
            font-size: 0.86rem;
            letter-spacing: 0.08em;
            text-transform: uppercase;
            background: rgba(255,255,255,0.03);
        }}

        .eyebrow .dot {{
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: var(--accent);
            box-shadow: 0 0 18px var(--accent);
        }}

        h1 {{
            margin: 18px 0 14px;
            font-size: clamp(3rem, 8vw, 6.4rem);
            line-height: 0.92;
            letter-spacing: -0.06em;
        }}

        .gradient-text {{
            background: linear-gradient(90deg, #ffffff, var(--accent) 38%, var(--accent-2) 72%, var(--accent-3));
            -webkit-background-clip: text;
            background-clip: text;
            color: transparent;
        }}

        .lede {{
            max-width: 720px;
            color: var(--muted);
            font-size: 1.06rem;
            line-height: 1.8;
            margin: 0 0 26px;
        }}

        .chips {{
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
            margin-bottom: 28px;
        }}

        .chip {{
            display: inline-flex;
            align-items: center;
            gap: 10px;
            padding: 12px 16px;
            border-radius: 999px;
            border: 1px solid rgba(255,255,255,0.10);
            background: rgba(255,255,255,0.035);
            color: var(--text);
            font-size: 0.92rem;
        }}

        .chip b {{
            color: var(--accent);
            font-weight: 700;
        }}

        .cta-row {{
            display: flex;
            flex-wrap: wrap;
            gap: 14px;
            margin-bottom: 34px;
        }}

        .cta, .ghost {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-height: 52px;
            padding: 0 18px;
            border-radius: 16px;
            text-decoration: none;
            font-weight: 700;
            letter-spacing: 0.03em;
            transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease, border-color 0.2s ease;
        }}

        .cta {{
            color: #04131a;
            background: linear-gradient(135deg, var(--accent), #d7fff6);
            box-shadow: 0 18px 34px rgba(124,247,212,0.22);
        }}

        .ghost {{
            color: var(--text);
            background: rgba(255,255,255,0.04);
            border: 1px solid rgba(255,255,255,0.12);
        }}

        .cta:hover, .ghost:hover {{
            transform: translateY(-2px);
        }}

        .hero-grid {{
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: 14px;
            margin-top: auto;
        }}

        .mini-card {{
            padding: 18px;
            border-radius: 22px;
            background: rgba(255,255,255,0.04);
            border: 1px solid rgba(255,255,255,0.10);
        }}

        .mini-card .label {{
            color: var(--muted-2);
            font-size: 0.82rem;
            text-transform: uppercase;
            letter-spacing: 0.12em;
            margin-bottom: 10px;
        }}

        .mini-card .value {{
            font-size: 1rem;
            line-height: 1.55;
            color: var(--text);
        }}

        .side {{
            display: grid;
            gap: 22px;
        }}

        .panel {{
            border-radius: 30px;
            padding: 24px;
            overflow: hidden;
            position: relative;
        }}

        .panel h2 {{
            margin: 0 0 14px;
            font-size: 1.1rem;
            letter-spacing: 0.14em;
            text-transform: uppercase;
            color: #dce5ff;
        }}

        .stat-row {{
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 12px;
        }}

        .stat {{
            padding: 18px;
            border-radius: 20px;
            background: rgba(255,255,255,0.035);
            border: 1px solid rgba(255,255,255,0.10);
        }}

        .stat strong {{
            display: block;
            font-size: 1.5rem;
            margin-bottom: 6px;
        }}

        .stat span {{
            color: var(--muted);
            font-size: 0.88rem;
            line-height: 1.5;
        }}

        .timeline {{
            display: grid;
            gap: 12px;
            margin-top: 6px;
        }}

        .timeline-item {{
            display: grid;
            grid-template-columns: 88px 1fr;
            gap: 14px;
            padding: 14px 0;
            border-top: 1px solid rgba(255,255,255,0.08);
        }}

        .timeline-item:first-child {{
            border-top: 0;
            padding-top: 0;
        }}

        .timeline-item .year {{
            color: var(--accent);
            font-weight: 800;
            letter-spacing: 0.08em;
        }}

        .timeline-item .desc {{
            color: var(--muted);
            line-height: 1.6;
        }}

        .sections {{
            display: grid;
            gap: 22px;
            padding-bottom: 40px;
        }}

        .section {{
            padding: 26px;
            border-radius: 30px;
            background: var(--card);
            border: 1px solid var(--line);
            box-shadow: var(--shadow);
        }}

        .section-header {{
            display: flex;
            align-items: end;
            justify-content: space-between;
            gap: 16px;
            margin-bottom: 20px;
            flex-wrap: wrap;
        }}

        .section-header h3 {{
            margin: 0;
            font-size: 1.65rem;
            letter-spacing: -0.03em;
        }}

        .section-header p {{
            margin: 0;
            color: var(--muted);
            max-width: 600px;
            line-height: 1.6;
        }}

        .skills {{
            display: grid;
            grid-template-columns: repeat(4, minmax(0, 1fr));
            gap: 12px;
        }}

        .skill {{
            padding: 18px;
            border-radius: 22px;
            background: linear-gradient(180deg, rgba(255,255,255,0.05), rgba(255,255,255,0.02));
            border: 1px solid rgba(255,255,255,0.10);
            min-height: 158px;
        }}

        .skill .icon {{
            width: 42px;
            height: 42px;
            border-radius: 14px;
            display: grid;
            place-items: center;
            background: rgba(124,247,212,0.10);
            color: var(--accent);
            margin-bottom: 12px;
            font-size: 1.1rem;
        }}

        .skill strong {{
            display: block;
            margin-bottom: 8px;
            font-size: 1.04rem;
        }}

        .skill p {{
            margin: 0;
            color: var(--muted);
            line-height: 1.65;
            font-size: 0.95rem;
        }}

        .project-grid {{
            display: grid;
            grid-template-columns: 1.15fr 0.85fr;
            gap: 14px;
        }}

        .project-main {{
            padding: 24px;
            border-radius: 24px;
            background:
                radial-gradient(circle at top right, rgba(124,247,212,0.12), transparent 38%),
                rgba(255,255,255,0.04);
            border: 1px solid rgba(255,255,255,0.10);
            min-height: 320px;
        }}

        .project-main h4 {{
            margin: 0 0 8px;
            font-size: 1.35rem;
        }}

        .project-main p {{
            color: var(--muted);
            line-height: 1.75;
            margin-bottom: 18px;
        }}

        .codeframe {{
            border-radius: 20px;
            background: #07111f;
            border: 1px solid rgba(255,255,255,0.10);
            overflow: hidden;
        }}

        .codebar {{
            display: flex;
            gap: 8px;
            padding: 14px 16px;
            border-bottom: 1px solid rgba(255,255,255,0.08);
            background: rgba(255,255,255,0.02);
        }}

        .codebar span {{
            width: 10px;
            height: 10px;
            border-radius: 50%;
            background: rgba(255,255,255,0.35);
        }}

        .codebar span:nth-child(1) {{ background: #ff6b6b; }}
        .codebar span:nth-child(2) {{ background: #ffd166; }}
        .codebar span:nth-child(3) {{ background: #4ade80; }}

        pre {{
            margin: 0;
            padding: 18px;
            white-space: pre-wrap;
            color: #dce8ff;
            line-height: 1.75;
            font-size: 0.95rem;
            overflow-x: auto;
        }}

        .project-side {{
            display: grid;
            gap: 14px;
        }}

        .project-quote {{
            padding: 22px;
            border-radius: 24px;
            background: rgba(255,255,255,0.04);
            border: 1px solid rgba(255,255,255,0.10);
            color: var(--text);
            line-height: 1.8;
        }}

        .project-quote strong {{
            display: block;
            font-size: 0.9rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            color: var(--accent);
            margin-bottom: 12px;
        }}

        .contact {{
            display: grid;
            grid-template-columns: 1fr auto;
            align-items: center;
            gap: 18px;
        }}

        .contact p {{
            margin: 8px 0 0;
            color: var(--muted);
            line-height: 1.7;
            max-width: 760px;
        }}

        .badge-row {{
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            justify-content: flex-end;
        }}

        .badge {{
            padding: 11px 14px;
            border-radius: 999px;
            border: 1px solid rgba(255,255,255,0.10);
            background: rgba(255,255,255,0.04);
            color: var(--text);
            font-size: 0.9rem;
        }}

        .footer-note {{
            text-align: center;
            color: var(--muted-2);
            font-size: 0.88rem;
            padding: 24px 0 46px;
        }}

        .floaty {{
            animation: floaty 5.8s ease-in-out infinite;
        }}

        @keyframes floaty {{
            0%, 100% {{ transform: translateY(0px); }}
            50% {{ transform: translateY(-8px); }}
        }}

        .reveal {{
            opacity: 0;
            transform: translateY(24px);
            transition: opacity 0.8s ease, transform 0.8s ease;
        }}

        .reveal.visible {{
            opacity: 1;
            transform: translateY(0);
        }}

        @media (max-width: 1024px) {{
            .hero, .project-grid, .contact {{
                grid-template-columns: 1fr;
            }}

            .skills {{
                grid-template-columns: repeat(2, minmax(0, 1fr));
            }}
        }}

        @media (max-width: 720px) {{
            .wrap {{
                width: min(100% - 24px, 1220px);
            }}

            .topbar {{
                align-items: flex-start;
                flex-direction: column;
            }}

            .hero-card, .section, .panel {{
                border-radius: 24px;
                padding: 20px;
            }}

            .hero-grid {{
                grid-template-columns: 1fr;
            }}

            .skills {{
                grid-template-columns: 1fr;
            }}

            .timeline-item {{
                grid-template-columns: 1fr;
                gap: 6px;
            }}

            h1 {{
                font-size: clamp(2.6rem, 14vw, 4.6rem);
            }}

            .contact {{
                text-align: left;
            }}

            .badge-row {{
                justify-content: flex-start;
            }}
        }}
    </style>
</head>
<body>
    <div class='noise'></div>
    <div class='cursor-glow' id='cursorGlow'></div>
    <div class='orb one'></div>
    <div class='orb two'></div>
    <div class='orb three'></div>

    <main class='wrap'>
        <header class='topbar'>
            <a class='brand' href='#top'>
                <div class='sigil'>AS</div>
                <div>
                    <strong>{safeName}</strong>
                    <span>{safeRole} • {safeLocation}</span>
                </div>
            </a>
            <nav class='nav'>
                <a href='#about'>Hakkımda</a>
                <a href='#skills'>Yetenekler</a>
                <a href='#work'>Öne Çıkan İş</a>
                <a href='#contact'>İletişim</a>
            </nav>
        </header>

        <section class='hero' id='top'>
            <article class='hero-card reveal'>
                <div class='eyebrow'><span class='dot'></span> oyun geliştirici • sistem kurucu • network odaklı</div>
                <h1><span class='gradient-text'>{safeName}</span><br> oyunları sadece yapmıyor, atmosfer kuruyor.</h1>
                <p class='lede'>
                    {safeExperience} {safeLanguages} Adana'dan çıkan, teknik tarafı sağlam, hissi güçlü ve savaşı
                    sunucuda çözen oyun sistemleri kurmayı seven bir geliştiriciyim.
                </p>

                <div class='chips'>
                    <div class='chip'><b>1 yıl</b> Unity deneyimi</div>
                    <div class='chip'><b>2 yıl</b> C# kodlama</div>
                    <div class='chip'><b>Sıfırdan</b> network mimarisi</div>
                    <div class='chip'><b>Battle royale</b> altyapı tecrübesi</div>
                </div>

                <div class='cta-row'>
                    <a class='cta' href='#work'>Projeyi incele</a>
                    <a class='ghost' href='#contact'>Birlikte üretelim</a>
                </div>

                <div class='hero-grid'>
                    <div class='mini-card floaty'>
                        <div class='label'>Odak</div>
                        <div class='value'>Network, combat loop, multiplayer his ve sistem mimarisi.</div>
                    </div>
                    <div class='mini-card floaty' style='animation-delay: .35s;'>
                        <div class='label'>Tarz</div>
                        <div class='value'>Klasik olmayan, sonuç odaklı, oyuncuya akılda kalan deneyimler.</div>
                    </div>
                    <div class='mini-card floaty' style='animation-delay: .7s;'>
                        <div class='label'>Merak</div>
                        <div class='value'>JS ve Python ile gerektiğinde tool, otomasyon ve prototip üretimi.</div>
                    </div>
                </div>
            </article>

            <aside class='side'>
                <div class='panel reveal'>
                    <h2>Profil Snapshot</h2>
                    <div class='stat-row'>
                        <div class='stat'><strong>Unity</strong><span>1 yıl boyunca gameplay, UI ve oyun akışı odaklı üretim.</span></div>
                        <div class='stat'><strong>C#</strong><span>2 yıl boyunca backend, game logic ve sistem yazımı.</span></div>
                        <div class='stat'><strong>Network</strong><span>Sıfırdan multiplayer altyapı ve battle royale denemeleri.</span></div>
                        <div class='stat'><strong>Adana</strong><span>Enerjisi yüksek, üretken ve direkt çalışan bir çalışma stili.</span></div>
                    </div>
                </div>

                <div class='panel reveal'>
                    <h2>Mini Yolculuk</h2>
                    <div class='timeline'>
                        <div class='timeline-item'>
                            <div class='year'>01</div>
                            <div class='desc'>Unity ile oyun üretim disiplinini oturttun, sahne akışı ve oyuncu deneyimi tarafını öğrendin.</div>
                        </div>
                        <div class='timeline-item'>
                            <div class='year'>02</div>
                            <div class='desc'>C# tarafında daha derine inip sistem tasarımı, performans ve modüler kod yazmaya geçtin.</div>
                        </div>
                        <div class='timeline-item'>
                            <div class='year'>03</div>
                            <div class='desc'>Network ve battle royale gibi zor alanlara girip, sadece oyun değil altyapı da kurabildiğini gösterdin.</div>
                        </div>
                    </div>
                </div>
            </aside>
        </section>

        <section class='sections'>
            <article class='section reveal' id='about'>
                <div class='section-header'>
                    <div>
                        <h3>Hakkımda</h3>
                        <p>Benim için iyi bir oyun, sadece güzel görünen değil; arkasında sağlam kurgu, temiz teknik yapı ve oyuncuda duygusal iz bırakan bir sistemdir.</p>
                    </div>
                </div>
                <div class='project-grid'>
                    <div class='project-main'>
                        <h4>Arda Sürücü</h4>
                        <p>
                            {safeRole} olarak odak noktam; oynanış hissini, network doğruluğunu ve teknik sürdürülebilirliği aynı masada
                            birleştirmek. Küçük araçlar, server mantığı, oyun döngüsü, state sync ve sistem tasarımı tarafında rahat
                            çalışıyorum. Gerektiğinde JS ve Python ile destekleyici araçlar da üretebiliyorum.
                        </p>
                        <div class='codeframe'>
                            <div class='codebar'><span></span><span></span><span></span></div>
                            <pre>GameLoop
  ├─ input
  ├─ prediction
  ├─ server validation
  ├─ snapshot sync
  └─ satisfying combat feel</pre>
                        </div>
                    </div>
                    <div class='project-side'>
                        <div class='project-quote'>
                            <strong>Felsefe</strong>
                            Güçlü oyunlar, oyuncuya sadece içerik değil, karakter de verir.
                        </div>
                        <div class='project-quote'>
                            <strong>Çalışma şekli</strong>
                            Hızlı prototip, sonra sertleştirme. Önce hissi yakalarım, sonra sistemi kusursuzlaştırırım.
                        </div>
                    </div>
                </div>
            </article>

            <article class='section reveal' id='skills'>
                <div class='section-header'>
                    <div>
                        <h3>Yetenek Alanları</h3>
                        <p>Teknik gücü tek bir satıra sıkıştırmak yerine, seni güçlü yapan parçaları net ve etkileyici biçimde gösteriyorum.</p>
                    </div>
                </div>
                <div class='skills'>
                    <div class='skill'>
                        <div class='icon'>U</div>
                        <strong>Unity Gameplay</strong>
                        <p>Oyuncu akışı, UI, state yönetimi, sahne organizasyonu ve oyun hislerini toparlama.</p>
                    </div>
                    <div class='skill'>
                        <div class='icon'>C#</div>
                        <strong>System Design</strong>
                        <p>Net sınıf yapısı, backend mantığı, event-driven düzen ve okunabilir kod üretimi.</p>
                    </div>
                    <div class='skill'>
                        <div class='icon'>N</div>
                        <strong>Networking</strong>
                        <p>Sıfırdan network kurma, multiplayer senkronizasyon ve battle royale deneyimi.</p>
                    </div>
                    <div class='skill'>
                        <div class='icon'>T</div>
                        <strong>Tooling</strong>
                        <p>JS ve Python ile yardımcı araçlar, veri işleme, otomasyon ve üretim hızlandırma.</p>
                    </div>
                </div>
            </article>

            <article class='section reveal' id='work'>
                <div class='section-header'>
                    <div>
                        <h3>Öne Çıkan İş</h3>
                        <p>Tek bir proje üzerinden hem teknik kapasiteyi hem de oyun geliştirme cesaretini net şekilde anlatan bölüm.</p>
                    </div>
                </div>
                <div class='project-grid'>
                    <div class='project-main'>
                        <h4>Sıfırdan Network Yazılmış Battle Royale</h4>
                        <p>
                            Oyunun en zor tarafına, yani network ve gerçek zamanlı akışa doğrudan girilmiş. Bu, sadece kod yazabildiğini değil,
                            aynı zamanda ölçeklenebilir sistem düşünerek hareket edebildiğini gösteriyor.
                        </p>
                        <div class='codeframe'>
                            <div class='codebar'><span></span><span></span><span></span></div>
                            <pre>client input -> server auth
server state  -> snapshot broadcast
hit logic     -> server validation
combat feel   -> low-latency iteration</pre>
                        </div>
                    </div>
                    <div class='project-side'>
                        <div class='project-quote'>
                            <strong>Teknik etki</strong>
                            Hazır sistem tüketmek yerine altyapıyı kurma cesareti var.
                        </div>
                        <div class='project-quote'>
                            <strong>Değer</strong>
                            Oyun geliştirme ekibinde hem yapımcı hem problem çözücü rolüne uygun profil.
                        </div>
                    </div>
                </div>
            </article>

            <article class='section reveal' id='contact'>
                <div class='contact'>
                    <div>
                        <div class='eyebrow'><span class='dot'></span> iletişim için hazır</div>
                        <h3 style='margin: 14px 0 0; font-size: 2rem;'>Bu enerjiye sahip bir portfolyo, ekiplerde kolay unutulmaz.</h3>
                        <p>
                            Eğer istersen bir sonraki adımda bunu e-posta, GitHub, itch.io, LinkedIn ve gerçek proje ekran görüntüleriyle
                            daha da kişiselleştirip “iş başvurusuna hazır” hale getirebiliriz.
                        </p>
                    </div>
                    <div class='badge-row'>
                        <div class='badge'>{safeLocation}</div>
                        <div class='badge'>Game Developer</div>
                        <div class='badge'>Unity + C#</div>
                        <div class='badge'>Network Systems</div>
                    </div>
                </div>
            </article>
        </section>

        <div class='footer-note'>Klasik portfolyo değil, seni teknik ve yaratıcı olarak tek bakışta anlatan bir sahne.</div>
    </main>

    <script>
        const glow = document.getElementById('cursorGlow');
        window.addEventListener('pointermove', (event) => {{
            glow.style.left = event.clientX + 'px';
            glow.style.top = event.clientY + 'px';
        }});

        const revealItems = document.querySelectorAll('.reveal');
        const observer = new IntersectionObserver((entries) => {{
            entries.forEach(entry => {{
                if (entry.isIntersecting) {{
                    entry.target.classList.add('visible');
                }}
            }});
        }}, {{ threshold: 0.18 }});

        revealItems.forEach(item => observer.observe(item));
    </script>
</body>
</html>";
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

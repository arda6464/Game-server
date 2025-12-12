using System;
using System.Net;
using System.Net.Mail;
using System.Text;

public static class EmailServiceSync
{
    // Static olarak tanımlanan SMTP ayarları
    private static string smtpServer = "smtp.gmail.com";
    private static int smtpPort = 587;
    private static string smtpUsername = "";
    private static string smtpPassword = "";
    private static bool useSSL = true;
    private static string fromEmail = "noreply@sirketiniz.com";
    private static string fromName = "Doğrulama Sistemi";

    // Static constructor - ayarları yükler
    static EmailServiceSync()
    {
        Console.WriteLine("E-posta servisi başlatılıyor...");
        // İstersen burada config dosyasından ayarları okuyabilirsin
        // LoadConfig();
    }

    // Ayarları değiştirmek için static method
    public static void Configure(string server, int port, string username, string password,
                                bool ssl = true, string from = null, string displayName = null)
    {
        smtpServer = server;
        smtpPort = port;
        smtpUsername = username;
        smtpPassword = password;
        useSSL = ssl;
        fromEmail = from ?? username;
        fromName = displayName ?? "Doğrulama Sistemi";
        
        Console.WriteLine("E-posta servisi ayarları güncellendi");
    }

    // Doğrulama kodu e-postası gönder (SENKRON)
    public static bool SendVerificationCode(string toEmail, string verificationCode, string userName = null)
    {
        try
        {
            string subject = "Doğrulama Kodunuz";
            string body = CreateVerificationEmailBody(verificationCode, userName);

            return SendEmail(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] E-posta gönderilirken hata: {ex.Message}");
            return false;
        }
    }

    // E-posta gövdesi oluştur
   private static string  CreateVerificationEmailBody(string code, string userName)
{
    string formattedCode = FormatCode(code);
    string currentYear = DateTime.Now.Year.ToString();
    string greeting = string.IsNullOrEmpty(userName) ? "Merhaba," : $"Merhaba {userName},";

        return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Doğrulama Kodu - Oyun Adı</title>
    <style>
        /* Reset ve temel stiller */
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f7fa;
            margin: 0;
            padding: 20px;
        }}
        
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
        }}
        
        /* Header */
        .email-header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        
        .logo {{
            font-size: 28px;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
        }}
        
        .logo-icon {{
            font-size: 32px;
        }}
        
        .email-title {{
            font-size: 22px;
            margin-top: 10px;
            opacity: 0.9;
        }}
        
        /* Content */
        .email-content {{
            padding: 40px 30px;
        }}
        
        .greeting {{
            font-size: 18px;
            color: #2d3748;
            margin-bottom: 20px;
        }}
        
        .message {{
            color: #4a5568;
            margin-bottom: 30px;
            font-size: 16px;
        }}
        
        /* Kod kutusu */
        .code-container {{
            background: linear-gradient(135deg, #f6f9ff 0%, #f0f4ff 100%);
            border: 2px dashed #c3dafe;
            border-radius: 10px;
            padding: 25px;
            text-align: center;
            margin: 30px 0;
        }}
        
        .code-label {{
            color: #4a5568;
            font-size: 14px;
            margin-bottom: 10px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        
        .verification-code {{
            font-size: 42px;
            font-weight: bold;
            letter-spacing: 8px;
            color: #2d3748;
            background: white;
            padding: 20px;
            border-radius: 8px;
            margin: 15px 0;
            display: inline-block;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
            font-family: 'Courier New', monospace;
        }}
        
        .expiry-notice {{
            color: #718096;
            font-size: 14px;
            margin-top: 10px;
            font-style: italic;
        }}
        
        /* Güvenlik uyarıları */
        .security-section {{
            background-color: #fffaf0;
            border-left: 4px solid #f6ad55;
            padding: 20px;
            margin: 30px 0;
            border-radius: 0 8px 8px 0;
        }}
        
        .security-title {{
            color: #dd6b20;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        
        .security-list {{
            list-style: none;
            padding-left: 5px;
        }}
        
        .security-list li {{
            margin-bottom: 8px;
            padding-left: 24px;
            position: relative;
        }}
        
        .security-list li:before {{
            content: '⚠️';
            position: absolute;
            left: 0;
        }}
        
        /* Buton (opsiyonel) */
        .action-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            padding: 14px 32px;
            border-radius: 8px;
            font-weight: bold;
            margin: 20px 0;
            text-align: center;
            transition: transform 0.2s, box-shadow 0.2s;
        }}
        
        .action-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
        }}
        
        /* Footer */
        .email-footer {{
            background-color: #f7fafc;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e2e8f0;
            color: #718096;
            font-size: 14px;
        }}
        
        .social-links {{
            margin: 20px 0;
        }}
        
        .social-icon {{
            display: inline-block;
            margin: 0 10px;
            color: #718096;
            text-decoration: none;
            font-size: 20px;
            transition: color 0.2s;
        }}
        
        .social-icon:hover {{
            color: #667eea;
        }}
        
        .footer-links {{
            margin-top: 15px;
        }}
        
        .footer-links a {{
            color: #667eea;
            text-decoration: none;
            margin: 0 10px;
        }}
        
        .footer-links a:hover {{
            text-decoration: underline;
        }}
        
        .copyright {{
            margin-top: 20px;
            font-size: 12px;
            color: #a0aec0;
        }}
        
        /* Responsive */
        @media (max-width: 600px) {{
            .email-content {{
                padding: 25px 20px;
            }}
            
            .verification-code {{
                font-size: 32px;
                letter-spacing: 6px;
                padding: 15px;
            }}
            
            .email-header {{
                padding: 20px 15px;
            }}
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <!-- Header -->
        <div class='email-header'>
            <div class='logo'>
                <span class='logo-icon'>🎮</span>
                <span>OYUN ADI</span>
            </div>
            <div class='email-title'>Doğrulama Kodu</div>
        </div>
        
        <!-- Content -->
        <div class='email-content'>
            <p class='greeting'>{greeting}</p>
            
            <p class='message'>
                Hesabınıza giriş yapmak için doğrulama kodunuz aşağıdadır. 
                Bu kodu giriş ekranında kullanarak işleminizi tamamlayabilirsiniz.
            </p>
            
            <!-- Kod Kutusu -->
            <div class='code-container'>
                <div class='code-label'>Doğrulama Kodu</div>
                <div class='verification-code'>{formattedCode}</div>
                <div class='expiry-notice'>⏰ Bu kod 10 dakika boyunca geçerlidir</div>
            </div>
            
            <p class='message'>
                Kodu kimseyle paylaşmayın. Eğer bu talebi siz yapmadıysanız, 
                lütfen bu e-postayı dikkate almayın ve hemen destek ekibimizle iletişime geçin.
            </p>
            
            <!-- Güvenlik Uyarıları -->
            <div class='security-section'>
                <div class='security-title'>
                    <span>🔒</span>
                    <span>Güvenlik Uyarıları</span>
                </div>
                <ul class='security-list'>
                    <li>Bu kodu asla kimseyle paylaşmayın</li>
                    <li>Oyun ekibimiz hiçbir zaman sizden şifre veya doğrulama kodu istemez</li>
                    <li>Şüpheli aktiviteleri hemen bize bildirin</li>
                    <li>5 başarısız denemeden sonra kod geçersiz olacaktır</li>
                </ul>
            </div>
            
            <!-- Opsiyonel: Hızlı Giriş Butonu -->
            <div style='text-align: center;'>
                <a href='https://oyunadresi.com/giris' class='action-button'>
                    🎯 Hemen Giriş Yap
                </a>
            </div>
        </div>
        
        <!-- Footer -->
        <div class='email-footer'>
            <div class='social-links'>
                <a href='https://twitter.com/oyunadi' class='social-icon'>🐦</a>
                <a href='https://discord.gg/oyunadi' class='social-icon'>💬</a>
                <a href='https://youtube.com/oyunadi' class='social-icon'>📺</a>
                <a href='https://instagram.com/oyunadi' class='social-icon'>📷</a>
            </div>
            
            <p>Bu otomatik bir e-postadır, lütfen yanıtlamayın.</p>
            
            <div class='footer-links'>
                <a href='https://oyunadresi.com/yardim'>Yardım Merkezi</a>
                <a href='https://oyunadresi.com/gizlilik'>Gizlilik Politikası</a>
                <a href='https://oyunadresi.com/sartlar'>Kullanım Şartları</a>
                <a href='https://oyunadresi.com/iletisim'>İletişim</a>
            </div>
            
            <div class='copyright'>
                © {currentYear} Oyun Adı. Tüm hakları saklıdır.<br>
                Bu e-posta, üyelik işlemleri kapsamında gönderilmiştir.
            </div>
        </div>
    </div>
</body>
</html>";
        Console.WriteLine("mail gönderildi");
}

private static string FormatCode(string code)
{
    // Kod formatlama: 123-456
    if (code.Length == 6)
    {
        return $"{code.Substring(0, 3)}<span style='color: #667eea;'>-</span>{code.Substring(3, 3)}";
    }
    
    // Veya daha güzel: her 3 karakterde bir boşluk
    if (code.Length % 3 == 0)
    {
        StringBuilder formatted = new StringBuilder();
        for (int i = 0; i < code.Length; i += 3)
        {
            if (i > 0) formatted.Append(" ");
            formatted.Append(code.Substring(i, 3));
        }
        return formatted.ToString();
    }
    
    return code;
}
   
    // Genel e-posta gönderme metodu (SENKRON)
    public static bool SendEmail(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now}] E-posta gönderiliyor: {toEmail}");
            
            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = useSSL;
                smtpClient.Timeout = 10000; // 10 saniye timeout    

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
                
                Console.WriteLine($"[{DateTime.Now}] E-posta gönderildi: {toEmail}");
                return true;
            }
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"[{DateTime.Now}] SMTP Hatası: {smtpEx.StatusCode} - {smtpEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] E-posta gönderme hatası: {ex.Message}");
            return false;
        }
    }

    // Çoklu e-posta gönderme
    public static bool SendBulkVerificationCodes(string[] toEmails, string verificationCode)
    {
        Console.WriteLine($"[{DateTime.Now}] Toplu e-posta gönderiliyor ({toEmails.Length} adet)");
        
        int successCount = 0;
        foreach (string email in toEmails)
        {
            if (SendVerificationCode(email, verificationCode))
            {
                successCount++;
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] {email} adresine gönderilemedi");
            }
        }
        
        Console.WriteLine($"[{DateTime.Now}] Toplu e-posta tamamlandı: {successCount}/{toEmails.Length} başarılı");
        return successCount == toEmails.Length;
    }

    
    // Ayarları göster
    public static void ShowSettings()
    {
        Console.WriteLine("=== E-POSTA SERVİSİ AYARLARI ===");
        Console.WriteLine($"SMTP Server: {smtpServer}");
        Console.WriteLine($"SMTP Port: {smtpPort}");
        Console.WriteLine($"Kullanıcı Adı: {smtpUsername}");
        Console.WriteLine($"Şifre: {(string.IsNullOrEmpty(smtpPassword) ? "AYARLANMAMIŞ" : "***")}");
        Console.WriteLine($"SSL Kullan: {useSSL}");
        Console.WriteLine($"Gönderen: {fromEmail}");
        Console.WriteLine($"Gönderen Adı: {fromName}");
        Console.WriteLine("================================");
    }
}
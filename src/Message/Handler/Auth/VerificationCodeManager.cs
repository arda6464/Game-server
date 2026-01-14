using System;
using System.Collections.Generic;

public static class VerificationCodeManager
{
    // Static dictionary - tüm uygulama boyunca tek instance
    private static Dictionary<string, VerificationCode> verificationCodes = 
        new Dictionary<string, VerificationCode>(StringComparer.OrdinalIgnoreCase);
    
    private static readonly object lockObject = new object(); // Thread safety için
    
    public class VerificationCode
    {
        public string Code { get; set; }
        public DateTime ExpirationTime { get; set; }
        public int Attempts { get; set; }
        public string Email { get; set; }
        public DateTime CreatedTime { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpirationTime;
        public bool IsValid => !IsExpired && Attempts < 5;
        public double MinutesRemaining => (ExpirationTime - DateTime.UtcNow).TotalMinutes;
    }

    // Static constructor
    static VerificationCodeManager()
    {
        Console.WriteLine($"[{DateTime.Now}] VerificationCodeManager static constructor called");
        Console.WriteLine($"[{DateTime.Now}] Dictionary initialized with StringComparer.OrdinalIgnoreCase");
    }

    // Kod oluşturma
    public static string GenerateCode()
    {
        Random random = new Random();
        int code = random.Next(100000, 999999);
        Console.WriteLine($"[{DateTime.Now}] [GenerateCode] Generated: {code}");
        return code.ToString();
    }

    // Kod kaydetme
    public static void SaveCode(string email, string code, int validMinutes = 10)
    {
        lock (lockObject) // Thread-safe
        {
            string normalizedEmail = NormalizeEmail(email);
            Console.WriteLine($"[{DateTime.Now}] [SaveCode] Saving for: '{email}' -> '{normalizedEmail}', Code: {code}");
            
            var verificationCode = new VerificationCode
            {
                Code = code,
                Email = normalizedEmail,
                CreatedTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddMinutes(validMinutes),
                Attempts = 0
            };
            
            verificationCodes[normalizedEmail] = verificationCode;
            
            // Debug
            Console.WriteLine($"[{DateTime.Now}] [SaveCode] Total codes: {verificationCodes.Count}");
           
        }
    }

    // Kod doğrulama
    public static bool VerifyCode(string email, string userCode)
    {
        lock (lockObject) // Thread-safe
        {
            string normalizedEmail = NormalizeEmail(email);
            Console.WriteLine($"[{DateTime.Now}] [VerifyCode] Checking: '{email}' -> '{normalizedEmail}', Code: {userCode}");
            
        
                
            
            if (!verificationCodes.ContainsKey(normalizedEmail))
            {
                
                // Aradığımız key'in karakterlerini de göster
                Console.Write($"[{DateTime.Now}] [VerifyCode] Searching for key chars: ");
                for (int i = 0; i < normalizedEmail.Length; i++)
                {
                    Console.Write($"'{normalizedEmail[i]}'({(int)normalizedEmail[i]}) ");
                }
                Console.WriteLine();
                
                return false;
            }
            
         
            var storedCode = verificationCodes[normalizedEmail];
            
            if (storedCode.IsExpired)
            {
               
                verificationCodes.Remove(normalizedEmail);
                return false;
            }
            
            if (storedCode.Attempts >= 5)
            {
               
                verificationCodes.Remove(normalizedEmail);
                return false;
            }
            
            storedCode.Attempts++;
          
            
            bool codesMatch = string.Equals(storedCode.Code, userCode, StringComparison.Ordinal);
    
            
            if (codesMatch)
            {
             
                verificationCodes.Remove(normalizedEmail);
                return true;
            }
            
            
            return false;
        }
    }

    // Email normalizasyonu
    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;
            
        return email.Trim().ToLowerInvariant();
    }

    
    
    

 
    
   
    
    
    
  
    public static void CleanupExpiredCodes()
    {
        lock (lockObject)
        {
            var emailsToRemove = new List<string>();
            
            foreach (var kvp in verificationCodes)
            {
                if (kvp.Value.IsExpired)
                {
                    emailsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var email in emailsToRemove)
            {
                verificationCodes.Remove(email);
                Console.WriteLine($"[{DateTime.Now}] [CleanupExpiredCodes] Removed expired code for: '{email}'");
            }
            
            if (emailsToRemove.Count > 0)
            {
                Console.WriteLine($"[{DateTime.Now}] [CleanupExpiredCodes] Removed {emailsToRemove.Count} expired codes");
            }
        }
    }
    
   
    }
    
   
    

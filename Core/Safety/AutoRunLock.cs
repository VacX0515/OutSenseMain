using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.Safety
{
    /// <summary>
    /// AutoRun 실행 중 장비 조작/프로그램 종료 잠금 관리
    /// </summary>
    public class AutoRunLock
    {
        private string _passwordHash;
        private static readonly string ConfigPath =
            Path.Combine(PathSettings.Instance.ConfigPath, "AutoRunLock.dat");

        public bool IsLocked { get; private set; }
        public bool HasPassword => !string.IsNullOrEmpty(_passwordHash);

        public AutoRunLock()
        {
            LoadPassword();
        }

        /// <summary>비밀번호 설정</summary>
        public void SetPassword(string password)
        {
            _passwordHash = HashPassword(password);
            SavePassword();
        }

        /// <summary>비밀번호 제거</summary>
        public void ClearPassword()
        {
            _passwordHash = null;
            try { if (File.Exists(ConfigPath)) File.Delete(ConfigPath); }
            catch { }
        }

        /// <summary>잠금 활성화 (AutoRun 시작 시)</summary>
        public void Lock()
        {
            IsLocked = true;
        }

        /// <summary>잠금 해제 (AutoRun 종료 시 또는 비밀번호 인증)</summary>
        public void Unlock()
        {
            IsLocked = false;
        }

        /// <summary>비밀번호 검증 후 잠금 해제</summary>
        public bool TryUnlock(string password)
        {
            if (!HasPassword)
            {
                Unlock();
                return true;
            }

            if (HashPassword(password) == _passwordHash)
            {
                Unlock();
                return true;
            }
            return false;
        }

        /// <summary>비밀번호 검증</summary>
        public bool VerifyPassword(string password)
        {
            if (!HasPassword) return true;
            return HashPassword(password) == _passwordHash;
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
            return Convert.ToBase64String(bytes);
        }

        private void SavePassword()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, _passwordHash);
            }
            catch { }
        }

        private void LoadPassword()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    _passwordHash = File.ReadAllText(ConfigPath).Trim();
            }
            catch { }
        }
    }
}

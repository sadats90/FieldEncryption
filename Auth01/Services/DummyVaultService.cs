using System.Text.Json;
using System.Security.Cryptography;

namespace Auth01.Services
{
    public class DummyVaultService
    {
        private readonly string _filePath;
        private VaultData _vault;

        public DummyVaultService(string filePath)
        {
            _filePath = filePath;
            LoadVault();
        }

        private void LoadVault()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _vault = JsonSerializer.Deserialize<VaultData>(json) ?? new VaultData();
            }
            else
            {
                _vault = new VaultData();
                SaveVault();
            }
        }

        private void SaveVault()
        {
            var json = JsonSerializer.Serialize(_vault, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        // -----------------------------
        // Per-user key access (byte[])
        // -----------------------------
        public byte[] GetOrCreateUserKey(int userId)
        {
            if (!_vault.UserKeys.ContainsKey(userId.ToString()))
            {
                var key = RandomNumberGenerator.GetBytes(32); // 256-bit AES key
                _vault.UserKeys[userId.ToString()] = Convert.ToBase64String(key);
                SaveVault();
            }

            return Convert.FromBase64String(_vault.UserKeys[userId.ToString()]);
        }

        private class VaultData
        {
            public string MasterKey { get; set; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            public Dictionary<string, string> UserKeys { get; set; } = new Dictionary<string, string>();
        }
    }
}

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Auth;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Models.Auth;

namespace PMS.Infrastructure.Services;

public class InMemoryAuthService(IPersonnelService personnelService, IAccessControlService accessControlService) : IAuthService
{
    private const int ProtectedAdminPersonnelId = 1;
    private const string StateKey = "auth_accounts";
    private const string DefaultPassword = "123456";
    private static readonly object SyncRoot = new();
    private static readonly List<AuthAccountState> Accounts = SqliteJsonStore.LoadOrSeed(StateKey, () => new List<AuthAccountState>());
    private static readonly ConcurrentDictionary<string, AuthSessionDto> Sessions = new(StringComparer.Ordinal);
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(12);
    private static readonly Dictionary<char, string> PinyinCharMap = new()
    {
        ['李'] = "li", ['贝'] = "bei", ['何'] = "he", ['道'] = "dao", ['飞'] = "fei",
        ['张'] = "zhang", ['茹'] = "ru", ['陈'] = "chen", ['宇'] = "yu", ['侯'] = "hou",
        ['海'] = "hai", ['亮'] = "liang", ['舒'] = "shu", ['高'] = "gao", ['成'] = "cheng"
    };

    public async Task<LoginResultDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = NormalizeAccount((request.Account ?? string.Empty).Trim());
        var password = request.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        await EnsureAccountSeedsAsync(cancellationToken);

        AuthAccountState? state;
        lock (SyncRoot)
        {
            state = ResolveAccountState(account);
        }

        if (state is null || !VerifyPassword(password, state.PasswordHash, state.PasswordSalt))
        {
            return null;
        }

        var profile = await accessControlService.GetCurrentAsync(state.PersonnelId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var session = new AuthSessionDto
        {
            AccessToken = token,
            PersonnelId = state.PersonnelId,
            ExpiresAt = DateTime.UtcNow.Add(SessionTtl)
        };

        Sessions[token] = session;

        return new LoginResultDto
        {
            AccessToken = token,
            ExpiresAt = session.ExpiresAt,
            PersonnelId = profile.PersonnelId,
            PersonnelName = profile.PersonnelName,
            RoleType = profile.RoleType,
            SystemRole = profile.SystemRole,
            IsAdmin = profile.IsAdmin
        };
    }

    public Task<AuthSessionDto?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult<AuthSessionDto?>(null);
        }

        if (!Sessions.TryGetValue(accessToken, out var session))
        {
            return Task.FromResult<AuthSessionDto?>(null);
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            Sessions.TryRemove(accessToken, out _);
            return Task.FromResult<AuthSessionDto?>(null);
        }

        return Task.FromResult<AuthSessionDto?>(session);
    }

    public Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(Sessions.TryRemove(accessToken, out _));
    }

    private async Task EnsureAccountSeedsAsync(CancellationToken cancellationToken)
    {
        var personnel = await personnelService.QueryAsync(new PersonnelQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var personnelById = personnel.Items.ToDictionary(x => x.Id);

        var changed = false;
        lock (SyncRoot)
        {
            var existingAccounts = Accounts
                .Select(x => x.Account)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var person in personnel.Items)
            {
                var targetAccount = BuildUniqueAccount(existingAccounts, person.Name, person.Id);

                var existing = Accounts.FirstOrDefault(x => x.PersonnelId == person.Id);
                if (existing is not null)
                {
                    if (!string.Equals(existing.Account, targetAccount, StringComparison.OrdinalIgnoreCase))
                    {
                        existingAccounts.Remove(existing.Account);
                        existing.Account = targetAccount;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existingAccounts.Add(existing.Account);
                        changed = true;
                    }

                    if (!VerifyPassword(DefaultPassword, existing.PasswordHash, existing.PasswordSalt))
                    {
                        var (migratedHash, migratedSalt) = HashPassword(DefaultPassword);
                        existing.PasswordHash = migratedHash;
                        existing.PasswordSalt = migratedSalt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        changed = true;
                    }

                    continue;
                }

                var (hash, salt) = HashPassword(DefaultPassword);

                Accounts.Add(new AuthAccountState
                {
                    PersonnelId = person.Id,
                    Account = targetAccount,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    UpdatedAt = DateTime.UtcNow
                });

                existingAccounts.Add(targetAccount);
                changed = true;
            }

            var removed = Accounts.RemoveAll(x => !personnelById.ContainsKey(x.PersonnelId));
            changed = changed || removed > 0;

            if (changed)
            {
                SqliteJsonStore.Save(StateKey, Accounts);
            }
        }
    }

    private static string BuildUniqueAccount(HashSet<string> existingAccounts, string personnelName, int personnelId)
    {
        var baseAccount = BuildAccountFromName(personnelName, personnelId);
        if (!existingAccounts.Contains(baseAccount))
        {
            return baseAccount;
        }

        var candidate = $"{baseAccount}{personnelId}";
        if (!existingAccounts.Contains(candidate))
        {
            return candidate;
        }

        var seq = 1;
        while (existingAccounts.Contains($"{candidate}{seq}"))
        {
            seq++;
        }

        return $"{candidate}{seq}";
    }

    private static string BuildAccountFromName(string personnelName, int personnelId)
    {
        var normalized = (personnelName ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"user{personnelId}";
        }

        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var isAsciiLetterOrDigit = ch <= sbyte.MaxValue && char.IsLetterOrDigit(ch);
            if (isAsciiLetterOrDigit || ch == '_' || ch == '-')
            {
                builder.Append(ch);
                continue;
            }

            if (PinyinCharMap.TryGetValue(ch, out var py))
            {
                builder.Append(py);
            }
        }

        var account = builder.ToString();
        if (string.IsNullOrWhiteSpace(account))
        {
            return $"user{personnelId}";
        }

        return account;
    }


    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return (Convert.ToBase64String(derived), Convert.ToBase64String(saltBytes));
    }

    private static bool VerifyPassword(string password, string passwordHash, string passwordSalt)
    {
        byte[] saltBytes;
        byte[] expectedHash;

        try
        {
            saltBytes = Convert.FromBase64String(passwordSalt);
            expectedHash = Convert.FromBase64String(passwordHash);
        }
        catch
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private class AuthAccountState
    {
        public int PersonnelId { get; set; }
        public string Account { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    private static string NormalizeAccount(string account)
    {
        if (string.Equals(account, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return "admin";
        }

        return account;
    }

    private static AuthAccountState? ResolveAccountState(string account)
    {
        if (string.Equals(account, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Accounts.FirstOrDefault(x => string.Equals(x.Account, "admin", StringComparison.OrdinalIgnoreCase))
                ?? Accounts.FirstOrDefault(x => x.PersonnelId == ProtectedAdminPersonnelId);
        }

        return Accounts.FirstOrDefault(x => string.Equals(x.Account, account, StringComparison.OrdinalIgnoreCase));
    }
}

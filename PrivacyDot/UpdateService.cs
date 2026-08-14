using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace PrivacyDot;

internal sealed class UpdateInfo
{
    public UpdateInfo(
        Version version,
        Uri downloadUri,
        Uri releaseUri,
        string expectedSha256,
        long assetSize)
    {
        Version = version;
        DownloadUri = downloadUri;
        ReleaseUri = releaseUri;
        ExpectedSha256 = expectedSha256;
        AssetSize = assetSize;
    }

    public Version Version { get; }

    public Uri DownloadUri { get; }

    public Uri ReleaseUri { get; }

    public string ExpectedSha256 { get; }

    public long AssetSize { get; }
}

internal sealed class UpdateCheckResult
{
    public UpdateCheckResult(Version latestVersion, UpdateInfo? availableUpdate)
    {
        LatestVersion = latestVersion;
        AvailableUpdate = availableUpdate;
    }

    public Version LatestVersion { get; }

    public UpdateInfo? AvailableUpdate { get; }
}

internal sealed class UpdateService : IDisposable
{
    internal const string InstallerAssetName = "PrivacyDotSetup.exe";
    internal const string LatestReleaseApiUrl =
        "https://api.github.com/repos/farflashgroup/PrivacyDot/releases/latest";
    internal const int MaximumReleaseMetadataSize = 1024 * 1024;

    private const long MaximumInstallerSize = 100L * 1024L * 1024L;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareDelete = 0x00000004;
    private const uint CreateAlways = 2;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;
    private static readonly TimeSpan UpdateCheckTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan InstallerDownloadTimeout = TimeSpan.FromMinutes(5);
    private readonly HttpClient _httpClient;

    public UpdateService(HttpMessageHandler? handler = null)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        _httpClient = handler is null
            ? new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                CheckCertificateRevocationList = true,
                MaxAutomaticRedirections = 5
            })
            : new HttpClient(handler, disposeHandler: true);
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"PrivacyDot/{FormatVersion(InstalledVersion)}");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
    }

    public static Version InstalledVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        Version? currentVersion = null,
        CancellationToken cancellationToken = default)
    {
        using var requestCancellation = CreateTimeoutCancellation(
            cancellationToken,
            UpdateCheckTimeout);
        using var response = await _httpClient
            .GetAsync(
                LatestReleaseApiUrl,
                HttpCompletionOption.ResponseHeadersRead,
                requestCancellation.Token)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is long contentLength
            && contentLength > MaximumReleaseMetadataSize)
        {
            throw new InvalidDataException("The GitHub release response was unexpectedly large.");
        }

        var json = await ReadLimitedTextAsync(
                response.Content,
                MaximumReleaseMetadataSize,
                requestCancellation.Token)
            .ConfigureAwait(false);
        requestCancellation.Token.ThrowIfCancellationRequested();
        return ParseLatestRelease(json, currentVersion ?? InstalledVersion);
    }

    public async Task<string> DownloadInstallerAsync(
        UpdateInfo update,
        CancellationToken cancellationToken = default)
    {
        if (update.AssetSize <= 0 || update.AssetSize > MaximumInstallerSize)
        {
            throw new InvalidDataException("The update installer size was not trusted.");
        }

        var updateDirectory = Path.Combine(Path.GetTempPath(), "PrivacyDot", "Updates");
        Directory.CreateDirectory(updateDirectory);
        var installerPath = Path.Combine(
            updateDirectory,
            $"PrivacyDotSetup-{FormatVersion(update.Version)}-{Guid.NewGuid():N}.exe");

        try
        {
            using var requestCancellation = CreateTimeoutCancellation(
                cancellationToken,
                InstallerDownloadTimeout);
            using var response = await _httpClient
                .GetAsync(
                    update.DownloadUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    requestCancellation.Token)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is long contentLength
                && contentLength != update.AssetSize)
            {
                throw new InvalidDataException("The update download size did not match the GitHub release asset.");
            }

            using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var destination = new FileStream(
                installerPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await CopyWithSizeLimitAsync(
                        source,
                        destination,
                        update.AssetSize,
                        requestCancellation.Token)
                    .ConfigureAwait(false);
            }

            var downloadedSize = new FileInfo(installerPath).Length;

            if (downloadedSize != update.AssetSize)
            {
                throw new InvalidDataException("The downloaded update was incomplete.");
            }

            var actualSha256 = ComputeSha256(installerPath);

            if (!string.Equals(actualSha256, update.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
            }

            MarkAsInternetDownloaded(installerPath, update.DownloadUri, update.ReleaseUri);
            return installerPath;
        }
        catch
        {
            TryDelete(installerPath);
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    internal static UpdateCheckResult ParseLatestRelease(string json, Version currentVersion)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("GitHub returned an empty release response.");
        }

        GitHubReleasePayload? release;

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var serializer = new DataContractJsonSerializer(typeof(GitHubReleasePayload));
            release = serializer.ReadObject(stream) as GitHubReleasePayload;
        }
        catch (SerializationException ex)
        {
            throw new InvalidDataException("GitHub returned an invalid release response.", ex);
        }

        if (release is null || release.Draft || release.Prerelease)
        {
            throw new InvalidDataException("GitHub did not return a stable release.");
        }

        var latestVersion = ParseVersionTag(release.TagName);

        if (latestVersion <= currentVersion)
        {
            return new UpdateCheckResult(latestVersion, availableUpdate: null);
        }

        var asset = release.Assets?.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, InstallerAssetName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.State, "uploaded", StringComparison.OrdinalIgnoreCase));

        if (asset is null || asset.Size <= 0 || asset.Size > MaximumInstallerSize)
        {
            throw new InvalidDataException("The latest release does not contain a valid PrivacyDot installer.");
        }

        var releaseTag = release.TagName!.Trim();
        var escapedReleaseTag = Uri.EscapeDataString(releaseTag);
        var downloadUri = ParseTrustedGitHubUri(
            asset.BrowserDownloadUrl,
            "installer download",
            $"/farflashgroup/PrivacyDot/releases/download/{escapedReleaseTag}/",
            "/" + InstallerAssetName);
        var releaseUri = ParseTrustedGitHubUri(
            release.HtmlUrl,
            "release page",
            "/farflashgroup/PrivacyDot/releases/tag/",
            "/" + escapedReleaseTag);
        var expectedSha256 = ParseSha256Digest(asset.Digest);
        var update = new UpdateInfo(latestVersion, downloadUri, releaseUri, expectedSha256, asset.Size);
        return new UpdateCheckResult(latestVersion, update);
    }

    internal static Version ParseVersionTag(string? tagName)
    {
        var versionText = tagName?.Trim().TrimStart('v', 'V');

        if (string.IsNullOrWhiteSpace(versionText) || !Version.TryParse(versionText, out var version))
        {
            throw new InvalidDataException("The latest GitHub release has an invalid version tag.");
        }

        return version;
    }

    internal static string FormatVersion(Version version)
    {
        return version.Build >= 0 ? version.ToString(3) : version.ToString();
    }

    internal static async Task<long> CopyWithSizeLimitAsync(
        Stream source,
        Stream destination,
        long maximumSize,
        CancellationToken cancellationToken)
    {
        if (maximumSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSize));
        }

        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            var bytesRead = await source
                .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (bytesRead == 0)
            {
                return total;
            }

            if (bytesRead > maximumSize - total)
            {
                throw new InvalidDataException("The update download exceeded the GitHub release asset size.");
            }

            await destination
                .WriteAsync(buffer, 0, bytesRead, cancellationToken)
                .ConfigureAwait(false);
            total += bytesRead;
        }
    }

    internal static string ReadInternetZoneIdentifier(string path)
    {
        using var stream = OpenAlternateDataStream(
            path + ":Zone.Identifier",
            GenericRead,
            FileShareRead | FileShareDelete,
            OpenExisting,
            FileAccess.Read);
        using var reader = new StreamReader(stream, Encoding.ASCII);
        return reader.ReadToEnd();
    }

    private static Uri ParseTrustedGitHubUri(
        string? value,
        string fieldName,
        string requiredPathPrefix,
        string? requiredPathSuffix = null)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
            || !uri.IsDefaultPort
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || !uri.AbsolutePath.StartsWith(requiredPathPrefix, StringComparison.OrdinalIgnoreCase)
            || (requiredPathSuffix is not null
                && !uri.AbsolutePath.EndsWith(requiredPathSuffix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException($"The GitHub {fieldName} URL was not trusted.");
        }

        return uri;
    }

    private static string ParseSha256Digest(string? digest)
    {
        const string prefix = "sha256:";

        if (digest is null
            || string.IsNullOrWhiteSpace(digest)
            || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The GitHub release asset does not include a SHA-256 digest.");
        }

        var value = digest.Substring(prefix.Length);

        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException("The GitHub release asset has an invalid SHA-256 digest.");
        }

        return value;
    }

    private static CancellationTokenSource CreateTimeoutCancellation(
        CancellationToken cancellationToken,
        TimeSpan timeout)
    {
        var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCancellation.CancelAfter(timeout);
        return linkedCancellation;
    }

    private static async Task<string> ReadLimitedTextAsync(
        HttpContent content,
        int maximumSize,
        CancellationToken cancellationToken)
    {
        using var source = await content.ReadAsStreamAsync().ConfigureAwait(false);
        using var destination = new MemoryStream();

        try
        {
            await CopyWithSizeLimitAsync(source, destination, maximumSize, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException("The GitHub release response was unexpectedly large.", ex);
        }

        return Encoding.UTF8.GetString(destination.ToArray());
    }

    private static void MarkAsInternetDownloaded(string path, Uri downloadUri, Uri releaseUri)
    {
        var zoneIdentifier = string.Join(
            "\r\n",
            "[ZoneTransfer]",
            "ZoneId=3",
            $"HostUrl={downloadUri.AbsoluteUri}",
            $"ReferrerUrl={releaseUri.AbsoluteUri}",
            string.Empty);
        using var stream = OpenAlternateDataStream(
            path + ":Zone.Identifier",
            GenericWrite,
            FileShareRead,
            CreateAlways,
            FileAccess.Write);
        using var writer = new StreamWriter(stream, Encoding.ASCII);
        writer.Write(zoneIdentifier);
    }

    private static FileStream OpenAlternateDataStream(
        string path,
        uint desiredAccess,
        uint shareMode,
        uint creationDisposition,
        FileAccess fileAccess)
    {
        var handle = CreateFileW(
            path,
            desiredAccess,
            shareMode,
            IntPtr.Zero,
            creationDisposition,
            FileAttributeNormal,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "Windows could not preserve the update's Internet-zone provenance.");
        }

        return new FileStream(handle, fileAccess);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha256 = SHA256.Create();
        return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // A failed update should not be hidden by a temporary-file cleanup failure.
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DataContract]
    private sealed class GitHubReleasePayload
    {
        [DataMember(Name = "tag_name")]
        public string? TagName { get; set; }

        [DataMember(Name = "html_url")]
        public string? HtmlUrl { get; set; }

        [DataMember(Name = "draft")]
        public bool Draft { get; set; }

        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }

        [DataMember(Name = "assets")]
        public GitHubReleaseAssetPayload[]? Assets { get; set; }
    }

    [DataContract]
    private sealed class GitHubReleaseAssetPayload
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "state")]
        public string? State { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [DataMember(Name = "digest")]
        public string? Digest { get; set; }

        [DataMember(Name = "size")]
        public long Size { get; set; }
    }
}

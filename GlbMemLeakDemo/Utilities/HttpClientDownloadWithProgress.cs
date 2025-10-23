using System.Security.Cryptography;

namespace Redpoint.Mobile.Utilities;

public class HttpClientDownloadWithProgress : IDisposable
{
    private const int BufferSize = 64 * 1024;
    private const int ReadTimeoutSeconds = 30;
    private bool shouldDispose;
    private HttpClient httpClient;
    private string contentChecksum;
    private long contentSize = 0;
    public string ContentChecksum => contentChecksum;
    public long ContentSize => contentSize;
    

    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);
    public event ProgressChangedHandler ProgressChanged;

    public async Task Download(HttpClient client,
        string downloadUrl,
        string destinationFilePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new ArgumentException("Download URL cannot be null or whitespace.", nameof(downloadUrl));
        if (string.IsNullOrWhiteSpace(destinationFilePath))
            throw new ArgumentException("Destination file path cannot be null or whitespace.", nameof(destinationFilePath));

        try
        {
            shouldDispose = client == null;
            httpClient = client ?? new HttpClient { Timeout = TimeSpan.FromDays(1) };

            // Optional: Prevent chunked encoding issues
            httpClient.DefaultRequestHeaders.AcceptEncoding.Clear();

            using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                await DownloadFileFromHttpResponseMessage(response, destinationFilePath, cancellationToken);
            }
        }
        
#if ANDROID
        catch (Java.Lang.Exception)
        {
            // Work around for the cancellation on Android being interpreted as an internal error rather than a cancellation.
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The download was cancelled");
            }

            throw;
        }
#endif
        
        finally
        {
            // Used to ensure the try above is valid on iOS
        }
    }

    private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response, string destinationFilePath, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
            await ProcessContentStream(totalBytes, contentStream, destinationFilePath, cancellationToken);
        }
    }

    private async Task ProcessContentStream(long? totalDownloadSize,
        Stream contentStream,
        string destinationFilePath,
        CancellationToken cancellationToken)
    {
        contentChecksum = string.Empty;
        byte[] buffer = new byte[BufferSize];
        long totalBytesRead = 0L;

        using (MD5 md5 = MD5.Create())
        using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
        {
            bool isMoreToRead = true;

            while (isMoreToRead)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Timeout protection for each ReadAsync
                var readTask = contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(ReadTimeoutSeconds), cancellationToken);

                var completedTask = await Task.WhenAny(readTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Read operation timed out.");
                }

                var bytesRead = await readTask;

                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    contentChecksum = BitConverter.ToString(md5.Hash);
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                    break;
                }

                // Write and hash
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);

                totalBytesRead += bytesRead;

                // Trigger progress every read
                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
            }

            contentSize = totalBytesRead;
            // Ensure file flush
            await fileStream.FlushAsync(cancellationToken);
        }
    }

    private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
    {
        try
        {
            double? progressPercentage = null;
            if (totalDownloadSize.HasValue && totalDownloadSize.Value > 0)
            {
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
            }

            ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
        }
        catch (Exception e)
        {
            
        }
    }

    public void Dispose()
    {
        if (shouldDispose)
        {
            httpClient?.Dispose();
        }
    }
}
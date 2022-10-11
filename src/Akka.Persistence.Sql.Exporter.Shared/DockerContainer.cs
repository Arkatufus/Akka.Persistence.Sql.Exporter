// -----------------------------------------------------------------------
//  <copyright file="DockerContainer.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Docker.DotNet;
using Docker.DotNet.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Akka.Persistence.Sql.Exporter.Shared;

public abstract class DockerContainer: IAsyncDisposable, IDisposable
{
    private Stream? _stream;
    private readonly CancellationTokenSource _logsCts = new ();
    private Task? _readDockerTask;

    protected DockerContainer(string imageName, string tag, string containerName)
    {
        ImageName = imageName;
        Tag = tag;
        ContainerName = containerName;
        Client = new DockerClientConfiguration().CreateClient();

        OutputPath = Env.OutputPath;
        OutputDirectory = new DirectoryInfo(OutputPath);
        if(!OutputDirectory.Exists)
            OutputDirectory.Create();

        OnStdOut += (_, _) => { };
    }
    
    public virtual string DatabaseName => "akka_persistence_tests";
    public string OutputPath { get; }
    public DirectoryInfo OutputDirectory { get; }

    private string ImageName { get; }
    
    private string Tag { get; }
    
    private string FullImageName => $"{ImageName}:{Tag}";
    
    public string ContainerName { get; }

    protected virtual string? ReadyMarker { get; } = null;
    protected virtual int ReadyCount { get; } = 1;

    protected virtual TimeSpan ReadyTimeout { get; } = TimeSpan.FromMinutes(1);
    
    public DockerClient Client { get; }

    public event EventHandler<OutputReceivedArgs> OnStdOut;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var images = await Client.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "reference",
                    new Dictionary<string, bool>
                    {
                        {FullImageName, true}
                    }
                }
            }
        }, cancellationToken); 

        if (images.Count == 0)
            await Client.Images.CreateImageAsync(
                new ImagesCreateParameters {FromImage = ImageName, Tag = Tag}, null,
                new Progress<JSONMessage>(message =>
                {
                    Console.WriteLine(!string.IsNullOrEmpty(message.ErrorMessage)
                        ? message.ErrorMessage
                        : $"{message.ID} {message.Status} {message.ProgressMessage}");
                }), cancellationToken);
        
        // configure container parameters
        var options = new CreateContainerParameters();
        ConfigureContainer(options);
        options.Image = FullImageName;
        options.Name = ContainerName;
        options.Tty = true;
        
        // create the container
        await Client.Containers.CreateContainerAsync(options, cancellationToken);
        
        // start the container
        await Client.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters(), cancellationToken);
        
        // Create streams
        _stream = await Client.Containers.GetContainerLogsAsync(
            id: ContainerName,
            parameters: new ContainerLogsParameters
            {
                Follow = true,
                ShowStdout = true,
                ShowStderr = true,
                Timestamps = true
            },
            cancellationToken: cancellationToken);
        _readDockerTask = ReadDockerStreamAsync();

        // Wait until container is completely ready
        if(ReadyMarker is { })
            await AwaitUntilReadyAsync(ReadyMarker, ReadyTimeout);

        await AfterContainerStartedAsync(cancellationToken);
    }

    protected abstract void ConfigureContainer(CreateContainerParameters parameters);

    protected virtual Task AfterContainerStartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task AwaitUntilReadyAsync(string marker, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<string>();
        var count = 0;
        void LineProcessor(object? sender, OutputReceivedArgs args)
        {
            if(args.Output.Contains(marker))
            {
                count++;
                if(ReadyCount == count)
                    tcs.SetResult(args.Output);
            }
        }

        OnStdOut += LineProcessor;
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            var task = await Task.WhenAny(Task.Delay(Timeout.Infinite, cts.Token), tcs.Task);
            if(task == tcs.Task)
                return;
            throw new Exception($"Docker image failed to run within {timeout}.");
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
            OnStdOut -= LineProcessor;
        }
    }

    private readonly Dictionary<int, string> _execCache = new ();
    public async Task ExecuteCommandAsync(params string[] command)
    {
        if (!_execCache.TryGetValue(command.GetHashCode(), out var id))
        {
            var createResponse = await Client.Exec.ExecCreateContainerAsync(ContainerName, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = command
            });
            id = createResponse.ID;
            _execCache[command.GetHashCode()] = id;
        }

        var stream = await Client.Exec.StartAndAttachContainerExecAsync(id, false);
        using (stream)
        {
            var (stdOut, stdErr) = await stream.ReadOutputToEndAsync(default);
            if(!string.IsNullOrWhiteSpace(stdOut))
            {
                Console.WriteLine(">>>>>>>> StdOut");
                Console.WriteLine(stdOut);
                Console.WriteLine("<<<<<<<< StdOut");
            }

            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                throw new Exception(stdErr);
            }
        }
    }

    public async Task DownloadAsync(
        string path,
        string outputPath,
        string outputFile,
        bool extract = false,
        bool createDirectory = true)
    {
        var response = await Client.Containers.GetArchiveFromContainerAsync(
            id: ContainerName,
            parameters: new GetArchiveFromContainerParameters { Path = path },
            statOnly: false);

        var stream = response.Stream;
        try
        {
            var downloadFile = Path.Combine(outputPath, outputFile);
            await using (stream)
            {
                await stream.DumpToFile(downloadFile);
            }

            if (extract)
            {
                var directoryName = Path.GetFileNameWithoutExtension(outputFile);
                var extractPath = createDirectory ? Path.Combine(outputPath, directoryName) : outputPath;
                if (!Directory.Exists(extractPath))
                    Directory.CreateDirectory(extractPath);
                using var archive = ArchiveFactory.Open(downloadFile);
                using var reader = archive.ExtractAllEntries();
                reader.WriteAllToDirectory(extractPath, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to download file. {e}");
        }
    }
    
    private async Task ReadDockerStreamAsync()
    {
        using var reader = new StreamReader(_stream!);

        var tcs = new TaskCompletionSource();
        _logsCts.Token.Register(() => tcs.SetResult());
        
        while (!_logsCts.IsCancellationRequested)
        {
            var task = reader.ReadLineAsync();
            var result = await Task.WhenAny(tcs.Task, task);
            if (result != task)
                break;

            var line = task.Result;
            if (!string.IsNullOrEmpty(line))
                OnStdOut(this, new OutputReceivedArgs(line));
        }
    }

    private bool _disposing;
    public async ValueTask DisposeAsync()
    {
        // Perform async cleanup.
        await DisposeAsyncCore().ConfigureAwait(false);
        
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposing) return;
        _disposing = true;

        _logsCts.Cancel();
        _logsCts.Dispose();
        
        if (_readDockerTask is { })
            await _readDockerTask;
        
        if(_stream is { })
            await _stream.DisposeAsync();

        try
        {
            await Client.Containers.StopContainerAsync(
                id: ContainerName,
                parameters: new ContainerStopParameters());

            await Client.Containers.RemoveContainerAsync(
                id: ContainerName,
                parameters: new ContainerRemoveParameters { Force = true });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to stop and/or remove docker container. {e}");
        }
        finally
        {
            Client.Dispose();
        }
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if(_disposing) return;
            _disposing = true;

            _logsCts.Cancel();
            _logsCts.Dispose();
            _readDockerTask?.GetAwaiter().GetResult();
            _stream?.Dispose();
            
            try
            {
                Client.Containers.StopContainerAsync(
                        id: ContainerName, 
                        parameters: new ContainerStopParameters())
                    .GetAwaiter().GetResult();
                
                Client.Containers.RemoveContainerAsync(ContainerName, new ContainerRemoveParameters { Force = true })
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to stop and/or remove docker container. {e}");
            }
            finally
            {
                Client.Dispose();
            }
        }
    }
}
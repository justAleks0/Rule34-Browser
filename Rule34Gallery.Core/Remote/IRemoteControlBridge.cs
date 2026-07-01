namespace Rule34Gallery.Core.Remote;

public interface IRemoteControlBridge
{
    RemoteStateSnapshot CaptureState();

    Task<RemoteCommandResponse> ExecuteAsync(RemoteCommandRequest command, CancellationToken cancellationToken);
}

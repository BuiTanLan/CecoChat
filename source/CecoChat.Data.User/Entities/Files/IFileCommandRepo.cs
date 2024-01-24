namespace CecoChat.Data.User.Entities.Files;

public interface IFileCommandRepo
{
    Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path, long allowedUserId);
}

public readonly struct AssociateFileResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool Duplicate { get; init; }
}

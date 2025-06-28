using System;
using System.Runtime.InteropServices;

namespace Lexxys;

public interface IBlobService
{
		string Domain { get; }

		bool CanOpen(string reference);
        string CreateReference(BlobObjectId id, string? type = null, IDictionary<string, object?>? metadata = null);

		IBlobInfo GetBlobInfo(string reference);
		void Write(string reference, Stream stream, FileWriteMode fileWrite = default);
		void Copy(string source, string target, bool overwrite = false);
		void Move(string source, string target, bool overwrite = false);
		void Delete(string reference);

		Task<IBlobInfo> GetBlobInfoAsync(string reference, CancellationToken cancellation = default);
		Task WriteAsync(string reference, Stream stream, FileWriteMode fileWrite = default, CancellationToken cancellation = default);
		Task CopyAsync(string source, string target, bool overwrite = false, CancellationToken cancellation = default);
		Task MoveAsync(string source, string target, bool overwrite = false, CancellationToken cancellation = default);
		Task DeleteAsync(string reference, CancellationToken cancellation = default);

}


public enum FileWriteMode
{
	/// <summary>
	/// Creates a new file or overwrites an existing one.
	/// </summary>
	Write = 0,
	/// <summary>
	/// Creates a new file. Throws an exception if the file already exists.
	/// </summary>
	Create = 1,
	/// <summary>
	/// Appends data to the end of an existing file or creates a new file if it does not exist.
	/// </summary>
	Append = 2,
}

public class BlobObjectId
{
    private readonly BlobObjectIdType _type;
	private readonly IdValue _value;

	public BlobObjectId()
	{
	}

	public BlobObjectId(Guid id)
    {
        _type = BlobObjectIdType.Guid;
        _value = new IdValue(id);
	}

    public BlobObjectId(string? id)
    {
        _type = BlobObjectIdType.String;
		_value = new IdValue(id ?? string.Empty);
    }

    public BlobObjectId(long id)
    {
        _type = BlobObjectIdType.Int;
		_value = new IdValue(id);
    }

    public BlobObjectIdType Type => _type;
    public Guid GuidId => _type == BlobObjectIdType.Guid ? _value.GuidId: throw new InvalidCastException("BlobObjectId is not a Guid");
    public string StringId => _type == BlobObjectIdType.String ? _value.StringId ?? string.Empty: throw new InvalidCastException("BlobObjectId is not a string");
    public long LongId => _type == BlobObjectIdType.Int ? _value.IntId: throw new InvalidCastException("BlobObjectId is not a long");

    public override string ToString()
    {
        return _type switch
        {
            BlobObjectIdType.Int => _value.IntId.ToString(),
            BlobObjectIdType.Guid => _value.GuidId.ToString("n"),
            BlobObjectIdType.String => _value.StringId ?? string.Empty,
            _ => throw new InvalidOperationException("Unknown BlobObjectIdType")
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is BlobObjectId other &&
            _type == other._type &&
            (_type switch
            {
                BlobObjectIdType.Guid => _value.GuidId.Equals(other._value.GuidId),
                BlobObjectIdType.String => string.Equals(_value.StringId, other._value.StringId, StringComparison.Ordinal),
                BlobObjectIdType.Int => _value.IntId == other._value.IntId,
                _ => false
            });
    }

	public override int GetHashCode() => _type switch
    {
        BlobObjectIdType.Guid => _value.GuidId.GetHashCode(),
        BlobObjectIdType.String => _value.StringId?.GetHashCode() ?? 0,
        BlobObjectIdType.Int => _value.IntId.GetHashCode(),
        _ => throw new InvalidOperationException("Unknown BlobObjectIdType")
    };

	public static bool operator ==(BlobObjectId left, BlobObjectId right) => left.Equals(right);

    public static bool operator !=(BlobObjectId left, BlobObjectId right) => !(left == right);

    public static implicit operator BlobObjectId(Guid id) => new BlobObjectId(id);
    public static implicit operator BlobObjectId(int id) => new BlobObjectId(id);
    public static implicit operator BlobObjectId(long id) => new BlobObjectId(id);
	public static implicit operator BlobObjectId(string? id) => new BlobObjectId(id);

    public static explicit operator Guid(BlobObjectId id) => id._type == BlobObjectIdType.Guid ? id._value.GuidId: throw new InvalidCastException("BlobObjectId is not a Guid");
    public static explicit operator long(BlobObjectId id) => id._type == BlobObjectIdType.Int ? id._value.IntId: throw new InvalidCastException("BlobObjectId is not a long");
    public static explicit operator string(BlobObjectId id) => id._type == BlobObjectIdType.String ? id._value.StringId ?? string.Empty: throw new InvalidCastException("BlobObjectId is not a string");

    public static BlobObjectId Parse(string id) =>
        Guid.TryParse(id, out var guid) ?
            new BlobObjectId(guid):
        long.TryParse(id, out var longId) ?
            new BlobObjectId(longId):
            new BlobObjectId(id);

    public static BlobObjectId FromObject(object id) => id switch
    {
        Guid guid => new BlobObjectId(guid),
        long longId => new BlobObjectId(longId),
        string strId => new BlobObjectId(strId),
        _ => throw new ArgumentException("Unsupported type for BlobObjectId", nameof(id))
    };

	private readonly struct IdValue
	{
		public readonly Guid GuidId;
		public readonly long IntId;
		public readonly string? StringId;

		public IdValue(Guid value) => GuidId = value;
		public IdValue(long value) => IntId = value;
		public IdValue(string? value) => StringId = value;
	}
}

public enum BlobObjectIdType
{
    Int = 0,
    Guid = 1,
    String = 2,
 }
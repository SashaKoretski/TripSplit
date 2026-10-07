using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using TripSplit.DataAccess.Infrastructure;

namespace TripSplit.DataAccess.Tests.TestDoubles;

/// <summary>
/// Минимальный набор поддельных (fake) реализаций System.Data.Common, позволяющий
/// юнит-тестировать репозитории (raw ADO.NET/Npgsql) без реальной базы данных.
/// Каждый вызов DbConnection.CreateCommand() выдает следующий "сценарий" из очереди,
/// заранее сконфигурированный тестом (возвращаемые строки или количество affected rows).
/// </summary>
public sealed class FakeCommandScript
{
    public List<Dictionary<string, object?>>? Rows { get; init; }
    public int AffectedRows { get; init; }

    public static FakeCommandScript Reader(params Dictionary<string, object?>[] rows) =>
        new() { Rows = rows.ToList() };

    public static FakeCommandScript EmptyReader() =>
        new() { Rows = new List<Dictionary<string, object?>>() };

    public static FakeCommandScript NonQuery(int affectedRows) =>
        new() { AffectedRows = affectedRows };
}

public sealed class FakeDbDataReader : DbDataReader
{
    private readonly List<Dictionary<string, object?>> _rows;
    private readonly List<string> _columns;
    private int _index = -1;

    public FakeDbDataReader(IEnumerable<Dictionary<string, object?>> rows)
    {
        _rows = rows.ToList();
        _columns = _rows.Count > 0 ? _rows[0].Keys.ToList() : new List<string>();
    }

    private object? RawValue(int ordinal) => _rows[_index][_columns[ordinal]];

    public override object this[int ordinal] => RawValue(ordinal) ?? DBNull.Value;
    public override object this[string name] => RawValue(GetOrdinal(name)) ?? DBNull.Value;

    public override int Depth => 0;
    public override int FieldCount => _columns.Count;
    public override bool HasRows => _rows.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => -1;

    public override bool Read()
    {
        _index++;
        return _index < _rows.Count;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override bool NextResult() => false;
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    public override int GetOrdinal(string name)
    {
        var idx = _columns.IndexOf(name);
        if (idx < 0) throw new IndexOutOfRangeException($"Column '{name}' not found in fake row");
        return idx;
    }

    public override string GetName(int ordinal) => _columns[ordinal];

    public override bool IsDBNull(int ordinal) => RawValue(ordinal) is null or DBNull;
    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) =>
        Task.FromResult(IsDBNull(ordinal));

    public override object GetValue(int ordinal) => RawValue(ordinal) ?? DBNull.Value;

    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }

    public override Guid GetGuid(int ordinal) => (Guid)RawValue(ordinal)!;
    public override string GetString(int ordinal) => (string)RawValue(ordinal)!;
    public override decimal GetDecimal(int ordinal) => (decimal)RawValue(ordinal)!;
    public override long GetInt64(int ordinal) => Convert.ToInt64(RawValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(RawValue(ordinal));
    public override short GetInt16(int ordinal) => Convert.ToInt16(RawValue(ordinal));
    public override bool GetBoolean(int ordinal) => (bool)RawValue(ordinal)!;
    public override double GetDouble(int ordinal) => Convert.ToDouble(RawValue(ordinal));
    public override float GetFloat(int ordinal) => Convert.ToSingle(RawValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(RawValue(ordinal));
    public override char GetChar(int ordinal) => (char)RawValue(ordinal)!;
    public override DateTime GetDateTime(int ordinal) => (DateTime)RawValue(ordinal)!;
    public override Type GetFieldType(int ordinal) => RawValue(ordinal)?.GetType() ?? typeof(object);
    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

    public override T GetFieldValue<T>(int ordinal) => (T)RawValue(ordinal)!;

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        throw new NotSupportedException("Not needed by TripSplit mappers");

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        throw new NotSupportedException("Not needed by TripSplit mappers");

    public override IEnumerator GetEnumerator() => _rows.GetEnumerator();
}

public sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; } = DbType.Object;
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    public override int Size { get; set; }
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }
    public override void ResetDbType() => DbType = DbType.Object;
}

public sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<FakeDbParameter> _items = new();

    public IReadOnlyList<(string Name, object? Value)> Captured =>
        _items.Select(p => (p.ParameterName, p.Value)).ToList();

    public override int Add(object value)
    {
        _items.Add((FakeDbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var v in values) Add(v!);
    }

    public override void Clear() => _items.Clear();
    public override bool Contains(object value) => _items.Contains((FakeDbParameter)value);
    public override bool Contains(string value) => _items.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    public override int Count => _items.Count;
    public override IEnumerator GetEnumerator() => _items.GetEnumerator();
    protected override DbParameter GetParameter(int index) => _items[index];
    protected override DbParameter GetParameter(string parameterName) =>
        _items.First(p => p.ParameterName == parameterName);
    public override int IndexOf(object value) => _items.IndexOf((FakeDbParameter)value);
    public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value) => _items.Insert(index, (FakeDbParameter)value);
    public override bool IsFixedSize => false;
    public override bool IsReadOnly => false;
    public override bool IsSynchronized => false;
    public override void Remove(object value) => _items.Remove((FakeDbParameter)value);
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));
    protected override void SetParameter(int index, DbParameter value) => _items[index] = (FakeDbParameter)value;
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var idx = IndexOf(parameterName);
        _items[idx] = (FakeDbParameter)value;
    }
    public override object SyncRoot => this;
}

public sealed class FakeDbCommand : DbCommand
{
    private readonly FakeCommandScript _script;
    private readonly FakeDbParameterCollection _parameters = new();

    public FakeDbCommand(FakeCommandScript script) => _script = script;

    public IReadOnlyList<(string Name, object? Value)> CapturedParameters => _parameters.Captured;

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }
    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        new FakeDbDataReader(_script.Rows ?? new List<Dictionary<string, object?>>());

    public override int ExecuteNonQuery() => _script.AffectedRows;
    public override object? ExecuteScalar() => _script.Rows?.FirstOrDefault()?.Values.FirstOrDefault();
    public override void Prepare() { }
}

public sealed class FakeDbTransaction : DbTransaction
{
    private readonly FakeDbConnection _connection;
    public FakeDbTransaction(FakeDbConnection connection) => _connection = connection;
    public bool Committed { get; private set; }
    public bool RolledBack { get; private set; }
    protected override DbConnection DbConnection => _connection;
    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    public override void Commit() => Committed = true;
    public override void Rollback() => RolledBack = true;
}

/// <summary>
/// Поддельное соединение: каждый CreateCommand() выдает следующий сценарий из очереди,
/// заданной тестом в порядке ожидаемых SQL-вызовов репозитория.
/// </summary>
public sealed class FakeDbConnection : DbConnection
{
    private readonly Queue<FakeCommandScript> _scripts;
    public List<FakeDbCommand> ExecutedCommands { get; } = new();

    public FakeDbConnection(params FakeCommandScript[] scripts) => _scripts = new Queue<FakeCommandScript>(scripts);

    [AllowNull]
    public override string ConnectionString { get; set; } = "fake";
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "fake";
    public override ConnectionState State => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }

    protected override DbCommand CreateDbCommand()
    {
        var script = _scripts.Count > 0 ? _scripts.Dequeue() : FakeCommandScript.NonQuery(0);
        var cmd = new FakeDbCommand(script);
        ExecutedCommands.Add(cmd);
        return cmd;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeDbTransaction(this);
}

/// <summary>Factory, отдающая заранее сконфигурированное поддельное соединение (без сети/БД).</summary>
public sealed class FakeDbConnectionFactory : IDbConnectionFactory
{
    private readonly FakeDbConnection _connection;
    public FakeDbConnectionFactory(FakeDbConnection connection) => _connection = connection;
    public Task<DbConnection> OpenAsync(CancellationToken ct = default) => Task.FromResult<DbConnection>(_connection);
}

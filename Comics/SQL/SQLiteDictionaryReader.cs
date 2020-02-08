using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Collections;

namespace Comics.SQL {
    /** 
     * This class provides a layer of abstraction above SQLite.SQLiteDataReader and SQLite.DatabaseConnection.ExecuteReader
     * when executing a simple SELECT statement, so that reader results can be accessed like a dictionary, without the
     * user having to keep track of the order of columns. 
     * 
     * The keys provided are not sanitized. They must be trusted.
     */
    public class SQLiteDictionaryReader: IReadOnlyDictionary<string, object> {
        private readonly SQLiteDataReader reader;
        // not sure if using a dictionary will actually save time, but it will emit the correct error when using a nonexistent key
        private readonly Dictionary<string, int> keys = new Dictionary<string, int>();

        /**
         * arguments:
         *     reader - the reader to wrap around
         *     keys - in order, the names for each column of the reader
         */
        public SQLiteDictionaryReader(SQLiteDataReader reader, IEnumerable<string> keys) {
            // maps keys [a, b, c] to a dictionary {a: 0, b: 1, c: 2}
            var currentIndex = 0;
            this.keys = keys.ToDictionary((key) => key, (key) => currentIndex++);

            this.reader = reader;
        }

        public bool IsClosed => this.reader.IsClosed;
        public bool HasRows => this.reader.HasRows;
        public int FieldCount => this.reader.FieldCount;

        public bool Read() => this.reader.Read();
        public void Close() => this.reader.Close();

        /**
         * Executes a simple select statement.
         * 
         * The argument *keys* must be directly available column names. These are the same keys that are used to query 
         * the resulting SQLiteDictionaryReader.
         */
        public static SQLiteDictionaryReader ExecuteSelect(SQLiteConnection connection, string table, IEnumerable<string> keys, 
                    string restOfQuery = "", IDictionary<string, object> queryParameters = null) {
            using (var command = new SQLiteCommand(string.Format("SELECT {0} FROM {1} {2}", string.Join(", ", keys), table, restOfQuery), connection)) {
                if (queryParameters != null) {
                    foreach (var pair in queryParameters) {
                        command.Parameters.AddWithValue(pair.Key, pair.Value);
                    }
                }
                var reader = command.ExecuteReader();
                return new SQLiteDictionaryReader(reader, keys);
            }
        }

        /* we're only supporting SQLite's native data types + int32 and bool */
        public object GetValue(string key) => this.reader.GetValue(this.keys[key]);
        public bool GetBoolean(string key) => this.reader.GetBoolean(this.keys[key]);
        public int GetInt32(string key) => this.reader.GetInt32(this.keys[key]);
        public long GetInt64(string key) => this.reader.GetInt64(this.keys[key]);
        public double GetDouble(string key) => this.reader.GetDouble(this.keys[key]);
        public string GetString(string key) => this.reader.GetString(this.keys[key]);
        public object GetBlob(string key, bool readOnly) => this.reader.GetBlob(this.keys[key], readOnly);

        /* helper functions; these look really spaghetti dont they */
        public object GetValueOrNull(string key) => this.reader.IsDBNull(this.keys[key]) ? null : this.reader.GetValue(this.keys[key]);
        public bool? GetBooleanOrNull(string key) => this.reader.IsDBNull(this.keys[key]) ? (bool?)null : this.reader.GetBoolean(this.keys[key]);
        public int? GetInt32OrNull(string key) => this.reader.IsDBNull(this.keys[key]) ? (int?)null : this.reader.GetInt32(this.keys[key]);
        public long? GetInt64OrNull(string key) => this.reader.IsDBNull(this.keys[key]) ? (long?)null : this.reader.GetInt64(this.keys[key]);
        public double? GetDoubleOrNull(string key) => this.reader.IsDBNull(this.keys[key]) ? (double?)null : this.reader.GetDouble(this.keys[key]);
        public string GetStringOrNull(string key) => this.reader.IsDBNull(this.keys[key]) ? null : this.reader.GetString(this.keys[key]);
        public object GetBlobOrNull(string key, bool readOnly) => this.reader.IsDBNull(this.keys[key]) ? null : this.reader.GetBlob(this.keys[key], readOnly);

        /* implementation of IDictionary */
        public IEnumerable<string> Keys => this.keys.Keys;
        public IEnumerable<object> Values => this.keys.Select((_, index) => this.reader.GetValue(index));
        public int Count => this.keys.Count;

        public object this[string key] => this.GetValue(key);

        public bool ContainsKey(string key) => this.keys.ContainsKey(key);

        public bool TryGetValue(string key, out object value) {
            if (!this.ContainsKey(key)) {
                value = default;
                return false;
            }

            value = this[key];
            return true;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            => this.Keys.Select((key, index) => new KeyValuePair<string, object>(key, this[key])).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

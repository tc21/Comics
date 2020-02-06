using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Comics.SQL {
    class Database {
        private const string table_comics = "comics";
        private const string table_tags = "tags";
        private const string table_tags_xref = "comic_tags";
        private const string table_progress = "progress";

        private const string key_path = "folder";
        private const string key_unique_id = "unique_name";
        private const string key_title = "title";
        private const string key_author = "author";
        private const string key_category = "category";
        private const string key_display_title = "display_title";
        private const string key_display_author = "display_author";
        private const string key_display_category = "display_category";
        private const string key_thumbnail_source = "thumbnail_source";
        private const string key_loved = "loved";
        private const string key_disliked = "disliked";
        private const string key_active = "active";
        private const string key_date_added = "date_added";

        private const string key_tag_name = "name";
        private const string key_xref_comic_id = "comicid";
        private const string key_xref_tag_id = "tagid";

        private const string key_progress_comicid = "comicid";
        private const string key_progress = "progress";

        private static string profile = null;
        private static DatabaseConnection shared = null;


        public class DatabaseConnection {
            public SQLiteConnection Connection { get; }
            private const int version = 2;

            private DatabaseConnection(string path) {
                Connection = new SQLiteConnection("Data Source=" + path + ";Version=3;");
                Connection.Open();
            }

            ~DatabaseConnection() {
                try {
                    Connection.Close();
                } catch (ObjectDisposedException) {
                    // do nothing
                }
            }

            private int ExecuteNonQuery(SQLiteCommand c) {
                return c.ExecuteNonQuery();
            }

            private object ExecuteScalar(SQLiteCommand c) {
                return c.ExecuteScalar();
            }

            /** this is now deprecated and should only be used when reading single columns */
            private SQLiteDataReader ExecuteReader(SQLiteCommand c) {
                return c.ExecuteReader();
            }

            public static DatabaseConnection ForCurrentProfile(bool empty = false) {
                bool init = (empty || !File.Exists(Defaults.Profile.DatabaseFile));
                bool reload = (shared is null || profile != Defaults.Profile.DatabaseFile);

                if (init) {
                    SQLiteConnection.CreateFile(Defaults.Profile.DatabaseFile);
                }


                if (reload) {
                    shared = new DatabaseConnection(Defaults.Profile.DatabaseFile);
                    profile = Defaults.Profile.DatabaseFile;
                }

                if (init) {
                    shared.Init();
                }

                if (init || reload) {
                    shared.Migrate();
                }

                return shared;
            }

            private void Migrate() {
                var version = (long)ExecuteScalar(new SQLiteCommand("SELECT version FROM version", this.Connection));

                if (version > DatabaseConnection.version) {
                    throw new Exception("Database version too high!");
                }

                while (version < DatabaseConnection.version) {
                    version += 1;

                    this.ExecuteResource(string.Format("Comics.SQL.Migrations.{0}.sql", version));

                    ExecuteNonQuery(new SQLiteCommand(
                        string.Format("UPDATE version SET version = {0}", version),
                        this.Connection
                    ));
                }
            }

            private void ExecuteResource(string name) {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

                using (var reader = new StreamReader(stream)) {
                    var command = new SQLiteCommand(reader.ReadToEnd(), this.Connection);
                    ExecuteNonQuery(command);
                }
            }

            private void Init() {
                var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Comics.SQL.init.sql");

                using (StreamReader reader = new StreamReader(rs)) {
                    var command = new SQLiteCommand(reader.ReadToEnd(), this.Connection);
                    ExecuteNonQuery(command);
                }
            }

            private List<int> GetRowids(string table, Dictionary<string, object> constraints) {
                var constraintStrings = new List<string>();

                var command = new SQLiteCommand();

                foreach (var c in constraints) {
                    constraintStrings.Add(c.Key + " = @" + c.Key);
                    command.Parameters.AddWithValue("@" + c.Key, c.Value);
                }

                var constraintString = "";
                if (constraintStrings.Count != 0) {
                    constraintString = " WHERE " + string.Join(" AND ", constraintStrings);
                }

                command.CommandText = "SELECT rowid FROM " + table + constraintString;
                command.Connection = Connection;

                var reader = command.ExecuteReader();
                var ids = new List<int>();

                while (reader.Read()) {
                    ids.Add(reader.GetInt32(0));
                }

                return ids;
            }

            // returns the number of rows affected
            private int EditRows(string table, Dictionary<string, object> constraints, Dictionary<string, object> values) {
                var valueStrings = new List<string>();
                var constraintStrings = new List<string>();

                var command = new SQLiteCommand();


                foreach (var v in values) {
                    valueStrings.Add(v.Key + " = @v_" + v.Key);
                    command.Parameters.AddWithValue("@v_" + v.Key, v.Value);
                }

                foreach (var c in constraints) {
                    constraintStrings.Add(c.Key + " = @c_" + c.Key);
                    command.Parameters.AddWithValue("@c_" + c.Key, c.Value);
                }

                var valueString = "";
                if (valueStrings.Count != 0) {
                    valueString = " SET " + string.Join(", ", valueStrings);
                }

                var constraintString = "";
                if (constraintStrings.Count != 0) {
                    constraintString = " WHERE " + string.Join(" AND ", constraintStrings);
                }

                command.CommandText = "UPDATE " + table + valueString + constraintString;
                command.Connection = Connection;

                return ExecuteNonQuery(command);
            }

            private int ComicRowid(Comic comic) {
                var rowids = GetRowids(table_comics, new Dictionary<string, object> { [key_unique_id] = comic.UniqueIdentifier });
                if (rowids.Count != 1) {
                    throw new Exception("ComicRowId: expected 1 id, got " + rowids.Count.ToString());
                }

                return rowids[0];
            }

            public int AddComic(Comic comic, bool verify = true) {
                if (verify && HasComic(comic.UniqueIdentifier)) {
                    throw new Exception("Comic already exists");  // use own exception if you ever want to handle this
                }

                var command = new SQLiteCommand(
                    string.Format(
                        "INSERT INTO {11} ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})" +
                        "VALUES (@{0}, @{1}, @{2}, @{3}, @{4}, @{5}, @{6}, @{7}, @{8}, @{9}, @{10})",
                        key_path, key_unique_id, key_display_title, key_display_author, key_display_category, key_thumbnail_source,
                        key_loved, key_disliked, key_title, key_author, key_category, table_comics
                    ),
                    Connection
                );

                command.Parameters.AddWithValue("@" + key_path, comic.path);
                command.Parameters.AddWithValue("@" + key_unique_id, comic.UniqueIdentifier);
                command.Parameters.AddWithValue("@" + key_display_title, comic.Title);
                command.Parameters.AddWithValue("@" + key_display_author, comic.Author);
                command.Parameters.AddWithValue("@" + key_display_category, comic.Category);
                command.Parameters.AddWithValue("@" + key_thumbnail_source, comic.ThumbnailSource);
                command.Parameters.AddWithValue("@" + key_loved, Convert.ToInt32(comic.Loved));
                command.Parameters.AddWithValue("@" + key_disliked, Convert.ToInt32(comic.Disliked));
                command.Parameters.AddWithValue("@" + key_title, comic.real_title);
                command.Parameters.AddWithValue("@" + key_author, comic.real_author);
                command.Parameters.AddWithValue("@" + key_category, comic.real_category);

                if (ExecuteNonQuery(command) == 0) {
                    throw new Exception("Insertion failed");
                }

                int rowid = Convert.ToInt32(ExecuteScalar(new SQLiteCommand("SELECT last_insert_rowid()", Connection)));

                foreach (var tag in comic.Tags) {
                    AssociateTag(rowid, TagRowid(tag));
                }

                return rowid;
            }

            public bool UpdateComic(Comic comic, bool verify = true) {
                if (verify && !HasComic(comic.UniqueIdentifier)) {
                    throw new Exception("Comic does not exist");  // use own exception if you ever want to handle this
                }

                var comicid = ComicRowid(comic);

                var updateResult = EditRows(
                    table_comics,
                    new Dictionary<string, object> {
                        ["rowid"] = comicid
                    },
                    new Dictionary<string, object> {
                        [key_path] = comic.path,
                        [key_title] = comic.real_title,
                        [key_author] = comic.real_author,
                        [key_category] = comic.real_category,
                        [key_disliked] = comic.Disliked,
                        [key_loved] = comic.Loved,
                        [key_display_title] = comic.Title,
                        [key_display_author] = comic.Author,
                        [key_display_category] = comic.Category,
                        [key_active] = true
                    }
                ) != 0;

                if (!updateResult) {
                    return false;
                }

                var storedTags = GetComic(comicid).Tags;

                var delete = storedTags.Except(comic.Tags);
                var add = comic.Tags.Except(storedTags);

                foreach (var tag in delete) {
                    var xrefid = (int)ComicTagXrefId(comicid, TagRowid(tag));
                    RemoveRow(table_tags_xref, xrefid);
                }

                foreach (var tag in add) {
                    var tagid = AddTag(tag);
                    AssociateTag(comicid, tagid);
                }

                return true;
            }

            public int AddTag(string tag) {
                var command = new SQLiteCommand(
                    string.Format("INSERT INTO {0} ({1}) VALUES (@{1})", table_tags, key_tag_name),
                    Connection
                );
                command.Parameters.AddWithValue("@" + key_tag_name, tag);
                ExecuteNonQuery(command);

                var idCommand = new SQLiteCommand(
                    string.Format("SELECT rowid FROM {0} WHERE {1} = @{1}", table_tags, key_tag_name),
                    Connection);
                idCommand.Parameters.AddWithValue("@" + key_tag_name, tag);
                return Convert.ToInt32(ExecuteScalar(idCommand));
            }

            private int TagRowid(string tag) {
                // I made the judgement call that there's not a significant performance implication
                return AddTag(tag);
            }

            public bool HasComic(string uniqueIdentifier) {
                return GetRowids(table_comics, new Dictionary<string, object> { [key_unique_id] = uniqueIdentifier }).Count != 0;
            }

            private static readonly List<string> getComicQueryKeys = new List<string> {
                    "rowid", key_path, key_title, key_author, key_category, key_display_title, key_display_author,
                    key_display_category, key_thumbnail_source, key_loved, key_disliked, key_date_added
                };

            private SQLiteDictionaryReader GetComicReaderWithContraint(string constraintName, object constraintValue) {
                var parameters = new Dictionary<string, object> {
                    { "@" + constraintName, constraintValue }
                };

                return SQLiteDictionaryReader.ExecuteSelect(this.Connection, table_comics, getComicQueryKeys,
                    string.Format(" WHERE {0} = @{0}", constraintName), parameters);
            }

            public Comic GetComic(string uniqueIdentifier) 
                => this.GetComic(key_unique_id, uniqueIdentifier);

            public Metadata GetComicMetadata(string uniqueIdentifier) 
                => this.GetComicMetadata(key_unique_id, uniqueIdentifier);

            private Comic GetComic(int rowid)
                => this.GetComic("rowid", rowid);

            private Comic GetComic(string constraintName, object constraintValue) {
                var reader = this.GetComicReaderWithContraint(constraintName, constraintValue);

                if (!reader.HasRows) {
                    return null;
                }

                reader.Read();

                return this.ComicFromRow(reader);
            }

            private Metadata GetComicMetadata(string constraintName, object constraintValue) {
                var reader = this.GetComicReaderWithContraint(constraintName, constraintValue);
                
                if (!reader.HasRows) {
                    return null;
                }

                reader.Read();

                return this.ComicMetadataFromRow(reader);
            }

            public IEnumerable<Comic> AllComics() {
                var reader = this.GetComicReaderWithContraint(key_active, 1);

                var comics = new List<Comic>();

                while (reader.Read()) {
                    var comic = this.ComicFromRow(reader);
                    if (comic != null) {
                        comics.Add(comic);
                    }
                }

                return comics;
            }

            private bool RemoveRow(string table, int rowid) {
                var result = ExecuteNonQuery(new SQLiteCommand(string.Format("DELETE FROM {0} WHERE rowid = {1}", table, rowid), Connection));
                return result == 1;
            }

            private bool TagAssociated(Comic comic, string tag) {
                return TagAssociated(ComicRowid(comic), TagRowid(tag));
            }

            private bool TagAssociated(int comicid, int tagid) {
                return ComicTagXrefId(comicid, tagid) != null;
            }

            private int? ComicTagXrefId(int comicid, int tagid) {
                var rowids = GetRowids(
                    table_tags_xref,
                    new Dictionary<string, object> {
                        [key_xref_comic_id] = comicid,
                        [key_xref_tag_id] = tagid
                    }
                );

                if (rowids.Count == 0) {
                    return null;
                }

                return rowids[0];
            }

            private void AssociateTag(int comicid, int tagid) {
                ExecuteNonQuery(new SQLiteCommand(
                    string.Format(
                        "INSERT INTO {2} ({3}, {4}) VALUES ({0}, {1})",
                        comicid, tagid, table_tags_xref, key_xref_comic_id, key_xref_tag_id
                    ),
                    Connection
                ));
            }
            
            public void AssociateTag(Comic comic, string tag) {
                var tagid = AddTag(tag);
                AssociateTag(ComicRowid(comic), tagid);
            }

            // ordering is an implementation detail defined by getComicQuery
            // returns null if data is invalid
            private Comic ComicFromRow(SQLiteDictionaryReader reader) {
                var path = reader.GetString(key_path);
                var title = reader.GetString(key_title);
                var author = reader.GetString(key_author);
                var category = reader.GetString(key_category);
                var rowid = reader.GetInt32("rowid");
                var metadata = this.ComicMetadataFromRow(reader);
                var dateAdded = reader.GetString(key_date_added);

                try {
                    var comic = new Comic(title, author, category, path, metadata, validate: false) {
                        DateAdded = dateAdded
                    };
                    return comic;
                } catch (ComicLoadException) {
                    this.InvalidateComic(rowid);
                }

                return null;
            }

            private Metadata ComicMetadataFromRow(SQLiteDictionaryReader reader) {
                var rowid = reader.GetInt32("rowid");

                var m = new Metadata {
                    Title = reader.GetStringOrNull(key_display_title),
                    Author = reader.GetStringOrNull(key_display_author),
                    Category = reader.GetStringOrNull(key_display_category),
                    ThumbnailSource = reader.GetStringOrNull(key_thumbnail_source),
                    Loved = reader.GetBoolean(key_loved),
                    Disliked = reader.GetBoolean(key_disliked),
                    Tags = new HashSet<string>(this.ReadTags(rowid))
                };

                return m;
            }

            private List<string> ReadTags(int comicid) {
                var command = new SQLiteCommand(
                    string.Format(
                        "SELECT {0} FROM {1} WHERE {2} = {3}",
                        key_xref_tag_id, table_tags_xref, key_xref_comic_id, comicid
                    ),
                    Connection
                );

                var reader = ExecuteReader(command);

                var tags = new List<string>();

                while (reader.Read()) {
                    var tagid = reader.GetInt32(0);
                    tags.Add(GetTag(tagid));
                }

                return tags;
            }

            private string GetTag(int tagid) {
                return (string)ExecuteScalar(new SQLiteCommand(
                    string.Format("SELECT {0} from {1} WHERE rowid = {2}", key_tag_name, table_tags, tagid),
                    Connection
                ));
            }

            public void InvalidateAllComics() {
                ExecuteNonQuery(new SQLiteCommand(
                    string.Format("UPDATE {0} SET {1} = 0", table_comics, key_active),
                    Connection
                ));
            }

            public void InvalidateComic(Comic comic) {
                var comicid = ComicRowid(comic);
                InvalidateComic(comicid);
            }

            private void InvalidateComic(int comicid) {
                ExecuteNonQuery(new SQLiteCommand(
                    string.Format("UPDATE {0} SET {1} = 0 WHERE rowid = {2}", table_comics, key_active, comicid),
                    Connection
                ));
            }
            
            public int GetProgress(Comic comic) {
                return GetProgress(ComicRowid(comic));
            }

            public void SetProgress(Comic comic, int progress) {
                SetProgress(ComicRowid(comic), progress);
            }

            private int GetProgress(int comicid) {
                var progress = ExecuteReader(new SQLiteCommand(
                    string.Format("SELECT {0} FROM {1} WHERE {2} = {3}", table_progress, key_progress, key_progress_comicid, comicid),
                    Connection
                ));

                if (progress.HasRows) {
                    progress.Read();
                    return progress.GetInt32(0);
                }

                return 0;
            }

            private void SetProgress(int comicid, int progress) {
                // note: dependent on the implementation of ON CONFLICT REPLACE
                ExecuteNonQuery(new SQLiteCommand(
                    string.Format(
                        "INSERT INTO {0} ({1}, {2}) VALUES ({3}, {4})", 
                        table_progress, key_progress_comicid, key_progress, comicid, progress),
                    Connection
                ));
            }
        }

        public static class Manager {
            public static void UpdateComic(Comic c) {
                var conn = DatabaseConnection.ForCurrentProfile();
                if (conn.HasComic(c.UniqueIdentifier)) {
                    if (!conn.UpdateComic(c)) {
                        throw new Exception("Failed to update comic");
                    }
                } else {
                    conn.AddComic(c);
                }
            }

            public static void RemoveComic(Comic c) {
                var conn = DatabaseConnection.ForCurrentProfile();
                if (conn.HasComic(c.UniqueIdentifier)) {
                    conn.InvalidateComic(c);
                }
            }

            public static void RemoveComics(IEnumerable<Comic> cs) {
                var conn = DatabaseConnection.ForCurrentProfile();

                foreach (var c in cs) {
                    if (conn.HasComic(c.UniqueIdentifier)) {
                        conn.InvalidateComic(c);
                    }
                }
            }

            public static IEnumerable<Comic> AllComics() {
                var conn = DatabaseConnection.ForCurrentProfile();
                return conn.AllComics();
            }


            public static Metadata GetMetadata(string uniqueIdentifier) {
                var conn = DatabaseConnection.ForCurrentProfile();
                return conn.GetComicMetadata(uniqueIdentifier);
            }
        }
    }
}

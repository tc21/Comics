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

        private const string key_tag_name = "name";
        private const string key_xref_comic_id = "comicid";
        private const string key_xref_tag_id = "tagid";


        public class DatabaseConnection {
            public SQLiteConnection Connection { get; }
            private const int version = 1;

            private DatabaseConnection(string path) {
                Connection = new SQLiteConnection("Data Source=" + path + ";Version=3;");
                Connection.Open();
            }

            ~DatabaseConnection() {
                Connection.Close();
            }
        
            public static DatabaseConnection ForCurrentProfile(bool empty = false) {
                bool init = (empty || !File.Exists(Defaults.Profile.DatabaseFile));

                if (init) {
                    SQLiteConnection.CreateFile(Defaults.Profile.DatabaseFile);
                }

                var connection = new DatabaseConnection(Defaults.Profile.DatabaseFile);
                
                if (init) {
                    connection.Init();
                }

                return connection;
            }

            private void Init() {
                var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Comics.SQL.init.sql");

                using (StreamReader reader = new StreamReader(rs)) {
                    var command = new SQLiteCommand(reader.ReadToEnd(), this.Connection);
                    command.ExecuteNonQuery();
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
                    constraintString = " WHERE " + string.Join(", ", constraintStrings);
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
                    constraintString = " WHERE " + string.Join(", ", constraintStrings);
                }

                command.CommandText = "UPDATE " + table + valueString + constraintString;
                command.Connection = Connection;

                return command.ExecuteNonQuery();
            }

            private int ComicRowid(Comic comic) {
                var rowids = GetRowids(table_comics, new Dictionary<string, object> { [key_unique_id] = comic.path });
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
                command.Parameters.AddWithValue("@" + key_thumbnail_source, comic.ThumbnailSourcePath);
                command.Parameters.AddWithValue("@" + key_loved, Convert.ToInt32(comic.Loved));
                command.Parameters.AddWithValue("@" + key_disliked, Convert.ToInt32(comic.Disliked));
                command.Parameters.AddWithValue("@" + key_title, comic.real_title);
                command.Parameters.AddWithValue("@" + key_author, comic.real_author);
                command.Parameters.AddWithValue("@" + key_category, comic.real_category);

                if (command.ExecuteNonQuery() == 0) {
                    throw new Exception("Insertion failed");
                }

                int rowid = Convert.ToInt32(new SQLiteCommand("SELECT last_insert_rowid()", Connection).ExecuteScalar());

                foreach (var tag in comic.Tags) {
                    AssociateTag(rowid, TagRowid(tag));
                }

                return rowid;
            }

            public bool UpdateComic(Comic comic, bool verify = true) {
                if (verify && !HasComic(comic.UniqueIdentifier)) {
                    throw new Exception("Comic does not exist");  // use own exception if you ever want to handle this
                }

                return EditRows(
                    table_comics,
                    new Dictionary<string, object> {
                        [key_unique_id] = comic.UniqueIdentifier
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
            }

            public int AddTag(string tag) {
                var command = new SQLiteCommand(
                    string.Format("INSERT INTO {0} ({1}) VALUES (@{1})", table_tags, key_tag_name),
                    Connection
                );
                command.Parameters.AddWithValue("@" + key_tag_name, tag);
                command.ExecuteNonQuery();

                var idCommand = new SQLiteCommand(
                    string.Format("SELECT rowid FROM {0} WHERE {1} = @{1}", table_tags, key_tag_name),
                    Connection);
                command.Parameters.AddWithValue("@" + key_tag_name, tag);
                return Convert.ToInt32(command.ExecuteScalar());
            }

            private int TagRowid(string tag) {
                // I made the judgement call that there's not a significant performance implication
                return AddTag(tag);
            }

            public bool HasComic(string uniqueIdentifier) {
                return GetRowids(table_comics, new Dictionary<string, object> { [key_unique_id] = uniqueIdentifier }).Count != 0;
            }

            public Comic GetComic(string uniqueIdentifier) {
                return GetComic(key_unique_id, uniqueIdentifier);
            }

            private Comic GetComic(int rowid) {
                return GetComic("rowid", rowid);
            }

            private Comic GetComic(string constraintName, object constraintValue) {
                var command = new SQLiteCommand(
                    string.Format(
                        "SELECT {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9} FROM {10} WHERE {11} = @{11}",
                        key_path, key_title, key_author, key_category, key_display_title, key_display_author,
                        key_display_category, key_thumbnail_source, key_loved, key_disliked, table_comics, constraintName
                    ),
                    Connection
                );

                command.Parameters.AddWithValue("@" + constraintName, constraintValue);

                var reader = command.ExecuteReader();

                if (!reader.HasRows) {
                    return null;
                }

                reader.Read();

                return ComicFromRow(reader);
            }

            public IEnumerable<Comic> AllComics() {
                var command = new SQLiteCommand(
                    string.Format(
                        "SELECT {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9} FROM {10} WHERE {11} = 1",
                        key_path, key_title, key_author, key_category, key_display_title, key_display_author,
                        key_display_category, key_thumbnail_source, key_loved, key_disliked, table_comics, key_active
                    ),
                    Connection
                );

                var reader = command.ExecuteReader();

                while (reader.Read()) {
                    yield return ComicFromRow(reader);
                }
            }

            private void AssociateTag(int comicid, int tagid) {
                new SQLiteCommand(
                    string.Format(
                        "INSERT INTO {2} ({3}, {4}) VALUES ({0}, {1})",
                        comicid, tagid, table_tags_xref, key_xref_comic_id, key_xref_tag_id
                    ),
                    Connection
                ).ExecuteNonQuery();
            }
            
            public void AssociateTag(Comic comic, string tag) {
                AssociateTag(ComicRowid(comic), TagRowid(tag));
            }

            // ordering is an implementation detail
            private Comic ComicFromRow(SQLiteDataReader reader) {
                var path = reader.GetString(0);
                var title = reader.GetString(1);
                var author = reader.GetString(2);
                var category = reader.GetString(3);

                Metadata m = new Metadata {
                    Title = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Author = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Category = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ThumbnailSource = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Loved = reader.GetBoolean(8),
                    Disliked = reader.GetBoolean(9)
                };

                return new Comic(title, author, category, path, m);
            }

            public void InvalidateAllComics() {
                new SQLiteCommand(
                    string.Format("UPDATE {0} SET {1} = 0", table_comics, key_active),
                    Connection
                ).ExecuteNonQuery();
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

            public static IEnumerable<Comic> AllComics() {
                var conn = DatabaseConnection.ForCurrentProfile();
                return conn.AllComics();
            }
        }
    }
}

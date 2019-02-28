using System;
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
        public class DatabaseConnection {
            public SQLiteConnection Connection { get; }

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

            private List<int> GetRowids(string table, Dictionary<string, string> constraints) {
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

            private int ComicRowid(Comic comic) {
                var rowids = GetRowids("comics", new Dictionary<string, string> { ["folder"] = comic.ContainingPath });
                if (rowids.Count != 1) {
                    throw new Exception("ComicRowId: expected 1 id, got " + rowids.Count.ToString());
                }

                return rowids[0];
            }

            public int AddComic(Comic comic) {
                var path = comic.ContainingPath;
                if (GetRowids("comics", new Dictionary<string, string> { ["folder"] = path }).Count != 0) {
                    throw new Exception("Comic already exists");  // use own exception if you ever want to handle this
                }

                var command = new SQLiteCommand(
                    "INSERT INTO comics (folder, display_name, thumbnail_file, category, loved, disliked)" +
                    "VALUES (@folder, @display_name, @thumbnail_file, @category, @loved, @disliked)",
                    Connection
                );

                command.Parameters.AddWithValue("@folder", path);
                command.Parameters.AddWithValue("@display_name", comic.Title.Display);
                command.Parameters.AddWithValue("@thumbnail_file", comic.ThumbnailPath);
                command.Parameters.AddWithValue("@category", comic.Category);
                command.Parameters.AddWithValue("@loved", Convert.ToInt32(comic.Loved));
                command.Parameters.AddWithValue("@disliked", Convert.ToInt32(comic.Disliked));
                
                if (command.ExecuteNonQuery() == 0) {
                    throw new Exception("Insertion failed");
                }

                int rowid = Convert.ToInt32(new SQLiteCommand("SELECT last_insert_rowid()", Connection).ExecuteScalar());

                foreach (var tag in comic.Tags) {
                    AssociateTag(rowid, TagRowid(tag));
                }

                return rowid;
            }

            public int AddTag(string tag) {
                var command = new SQLiteCommand("INSERT INTO tags (name) VALUES (@name)", Connection);
                command.Parameters.AddWithValue("@name", tag);
                command.ExecuteNonQuery();

                var idCommand = new SQLiteCommand("SELECT rowid FROM tags WHERE name = @name", Connection);
                command.Parameters.AddWithValue("@name", tag);
                return Convert.ToInt32(command.ExecuteScalar());
            }

            private int TagRowid(string tag) {
                // I made the judgement call that there's not a significant performance implication
                return AddTag(tag);
            }


            private void AssociateTag(int comicid, int tagid) {
                new SQLiteCommand(
                    string.Format("INSERT INTO comic_tags (comic_rowid, tag_rowid) VALUES ({0}, {1})", comicid, tagid),
                    Connection
                ).ExecuteNonQuery();
            }
            
            public void AssociateTag(Comic comic, string tag) {
                AssociateTag(ComicRowid(comic), TagRowid(tag));
            }
        }
    }
}

CREATE TABLE comics (
    -- rowid INTEGER PRIMARY KEY AUTOINCREMENT,
    folder TEXT NOT NULL,
    display_name TEXT NOT NULL,
    thumbnail_file TEXT,
    category TEXT,
    loved INTEGER CHECK (loved IN (0, 1)),
    disliked INTEGER CHECK (loved IN (0, 1))
);

CREATE TABLE tags (
    name TEXT NOT NULL UNIQUE ON CONFLICT IGNORE
);

CREATE TABLE comic_tags (
    comicid INTEGER NOT NULL,
    tagid INTEGER NOT NULL,
    UNIQUE(comicid, tagid) ON CONFLICT IGNORE
);
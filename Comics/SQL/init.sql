CREATE TABLE comics (
    -- rowid INTEGER PRIMARY KEY AUTOINCREMENT,
    folder TEXT NOT NULL,

    unique_name TEXT UNIQUE NOT NULL,

    title TEXT NOT NULL,
    author TEXT NOT NULL,
    category TEXT NOT NULL,

    display_title TEXT,
    display_author TEXT,
    display_category TEXT,

    thumbnail_source TEXT,

    loved INTEGER CHECK (loved IN (0, 1)),
    disliked INTEGER CHECK (loved IN (0, 1)),

    active INTEGER DEFAULT 1 CHECK (active IN (0, 1))
);

CREATE TABLE tags (
    name TEXT NOT NULL UNIQUE ON CONFLICT IGNORE
);

CREATE TABLE comic_tags (
    comicid INTEGER NOT NULL,
    tagid INTEGER NOT NULL,
    UNIQUE(comicid, tagid) ON CONFLICT IGNORE
);
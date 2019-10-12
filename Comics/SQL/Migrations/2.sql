-- This migration adds a date_added field to track when something was first added to the database

ALTER TABLE comics RENAME TO comics_temp;

CREATE TABLE comics (
    folder TEXT NOT NULL,

    unique_name TEXT UNIQUE NOT NULL,

    title TEXT NOT NULL,
    author TEXT NOT NULL,
    category TEXT NOT NULL,

    display_title TEXT,
    display_author TEXT,
    display_category TEXT,

    thumbnail_source TEXT,
	
	date_added TEXT NOT NULL DEFAULT current_timestamp,

    loved INTEGER DEFAULT 0 CHECK (loved IN (0, 1)),
    disliked INTEGER DEFAULT 0 CHECK (loved IN (0, 1)),

    active INTEGER DEFAULT 1 CHECK (active IN (0, 1))
);

INSERT INTO comics (rowid, folder, unique_name, title, author, category, display_title,
                    display_author, display_category, thumbnail_source, loved, disliked, active)
	SELECT          rowid, folder, unique_name, title, author, category, display_title, 
	                display_author, display_category, thumbnail_source, loved, disliked, active
		FROM comics_temp;
	
DROP TABLE comics_temp;

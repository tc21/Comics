-- This migration should never be run automatically, but exists because it needed to be run manually.
-- NOTE: IT IS NOT NECESSARY TO UPDATE VERSION in .sql files. This is done automatically in code.
--       YOU WILL NEED TO UPDATE the constant SQL.Database.DatabaseConnection.version to reflect the newest version

CREATE TABLE version (
    version INTEGER NOT NULL
);

INSERT INTO version (version) VALUES (1);

CREATE TABLE progress (
    comicid INTEGER NOT NULL,
    progress INTEGER NOT NULL,
    
    UNIQUE(comicid) ON CONFLICT REPLACE
);
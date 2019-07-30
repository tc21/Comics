-- This migration should never be run automatically, but exists because it needed to be run manually.

CREATE TABLE version (
    version INTEGER NOT NULL
);

INSERT INTO version (version) VALUES (1);

CREATE TABLE progress (
    comicid INTEGER NOT NULL,
    progress INTEGER NOT NULL,
    
    UNIQUE(comicid) ON CONFLICT REPLACE
);
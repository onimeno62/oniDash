# Music Data Model

## Library and files
LibrarySource: id, name, rootPath, enabled, scanOnStartup, watchChanges, createdAt, updatedAt, lastScanAt.

AudioFile: id, sourceId, path, normalizedPath, fileName, extension, size, modifiedAt, duration, codec, bitrate, sampleRate, bitDepth, channels, optional checksum/hash, scanStatus, error.

## Catalogue
Artist: id, name, sortName, normalizedName, optional MusicBrainz ID, biography, image ID.

Album: id, optional artistId, title, sortTitle, normalizedTitle, optional year/releaseDate/releaseType/genre/label/MusicBrainz ID/artworkId, compilation.

Track: id, audioFileId, artistId, optional albumId, title, sortTitle, trackNumber, discNumber, optional year/genre/composer/conductor/albumArtist/MusicBrainz ID/lyrics/ReplayGain values.

Genre: id, name, normalizedName.

## Organization and history
Playlist: id, name, description, optional artworkId, isSmart, smartQuery, createdAt, updatedAt.
PlaylistItem: id, playlistId, trackId, position, addedAt.
Favorite: id, optional userId, entityType, entityId, createdAt.
PlayHistory: id, trackId, startedAt, optional completedAt, playedSeconds, completionRatio, source.

Artwork: id, sourceType, optional path, hash, width, height, mimeType, optional providerId.
ScanJob: id, optional sourceId, status, filesDiscovered, filesProcessed, filesFailed, startedAt, completedAt, errorLog.

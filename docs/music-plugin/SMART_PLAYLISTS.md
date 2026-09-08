# Smart Playlist Specification

Smart playlists are query definitions, not copied track lists. Example rules include genre, year, play count, favorite, artist, duration, bitrate, never played, recent play/add dates, album artist, and rating when ratings exist.

Support boolean AND, OR, and NOT. Allow sorting by random, title, artist, album, year, play count, last played, and date added, with a maximum result count.

The UI should provide a visual rule builder with field, operator, value, grouping, sort, and limit. An advanced internal representation is acceptable. Cache only when useful.

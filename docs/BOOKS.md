# Books dashboard

The Books plugin adds a dedicated reading-library dashboard at `/books` using the same dark-first oniDash visual system as Music and Movies.

## Included

- Library selection and reindex action
- Total, read, unread and page-count KPIs
- Continue Reading shelf with page/progress indicators
- All books, unread, read and favorites views
- Search across title, author, series and year
- Sorting by title, author, recent reading, rating and pages
- Responsive cover grid with lazy artwork
- Book information drawer
- Read/unread state
- Favorites
- 1–5 star ratings
- Manual page progress updates
- Series, format, year and page-count metadata

## API boundary

The frontend client expects `/books` catalog endpoints, `/books/continue`, `/books/:id/read`, `/books/:id/favorite`, `/books/:id/progress`, `/books/:id/rating`, `/books/reindex`, and `/api/books/:id/cover`.

This keeps the UI contract explicit while the local Windows service can implement filesystem scanning, EPUB/PDF/CBZ metadata extraction, cover caching, metadata providers, reader integration and file organization behind the same API.

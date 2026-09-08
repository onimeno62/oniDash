# Music Plugin Design System

Use the existing oniDash spacing scale, font system, semantic color tokens, and shared components.

Hierarchy: Display → H1 → H2 → H3 → body → metadata → labels. Track titles have stronger weight than secondary metadata.

Artwork priority: embedded, cached provider, generated placeholder. Never make layout depend on image dimensions. Use object-fit cover.

Motion should communicate navigation, playback, opening/closing, hover/focus, and queue changes. Keep it short and interruptible, and respect prefers-reduced-motion.

Music may derive atmospheric accents from artwork, but content must remain readable and accessible. Never hardcode arbitrary colors inside individual components when semantic tokens exist.

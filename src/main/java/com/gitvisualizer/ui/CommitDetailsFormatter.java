package com.gitvisualizer.ui;

import com.gitvisualizer.model.Commit;

import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;

public final class CommitDetailsFormatter {

    private CommitDetailsFormatter() {
    }

    public static String message(Commit c) {
        if (c == null || c.commit() == null || c.commit().message() == null) {
            return "(No message)";
        }
        String m = c.commit().message().trim();
        return truncate(m, 200);
    }

    public static String author(Commit c) {
        if (c == null) {
            return "(Unknown)";
        }
        if (c.commit() != null && c.commit().author() != null && c.commit().author().name() != null) {
            return c.commit().author().name();
        }
        if (c.author() != null && c.author().login() != null) {
            return c.author().login();
        }
        return "(Unknown)";
    }

    public static String date(Commit c) {
        if (c == null || c.commit() == null) {
            return "(No date)";
        }
        String raw = null;
        if (c.commit().committer() != null && c.commit().committer().date() != null) {
            raw = c.commit().committer().date();
        } else if (c.commit().author() != null && c.commit().author().date() != null) {
            raw = c.commit().author().date();
        }
        if (raw == null || raw.isEmpty()) {
            return "(No date)";
        }
        try {
            OffsetDateTime dt = OffsetDateTime.parse(raw);
            return dt.format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm"));
        } catch (DateTimeParseException e) {
            return raw;
        }
    }

    public static Commit fromNodeData(com.gitvisualizer.graph.GraphNodeData node) {
        if (node == null) {
            return null;
        }
        return new Commit(
                node.sha(),
                null,
                null,
                null,
                new com.gitvisualizer.model.CommitInfo(
                        new com.gitvisualizer.model.GitAuthor(node.authorName(), null, node.date()),
                        new com.gitvisualizer.model.GitAuthor(node.authorName(), null, node.date()),
                        node.message(),
                        null,
                        null,
                        0
                ),
                null,
                null,
                null
        );
    }

    private static String truncate(String s, int max) {
        if (s.length() <= max) {
            return s;
        }
        return s.substring(0, max) + "...";
    }
}

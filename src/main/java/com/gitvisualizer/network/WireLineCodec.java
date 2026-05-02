package com.gitvisualizer.network;

/** TCP messages are one JSON object per line; Windows often sends {@code \r\n}—strip the stray {@code \r}. */
public final class WireLineCodec {

    private WireLineCodec() {
    }

    public static String normalizeLine(String line) {
        if (line.isEmpty()) {
            return line;
        }
        if (line.charAt(line.length() - 1) == '\r') {
            return line.substring(0, line.length() - 1);
        }
        return line;
    }
}

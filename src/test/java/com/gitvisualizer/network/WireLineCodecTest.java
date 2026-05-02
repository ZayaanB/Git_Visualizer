package com.gitvisualizer.network;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

class WireLineCodecTest {

    @Test
    void stripsTrailingCrOnly() {
        assertEquals("{}", WireLineCodec.normalizeLine("{}\r"));
        assertEquals("abc", WireLineCodec.normalizeLine("abc"));
    }
}

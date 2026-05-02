package com.gitvisualizer.game;

import org.junit.jupiter.api.Test;

import java.util.Random;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class MergeConflictGameTest {

    @Test
    void picksFiveDistinctConflictsWhenEnoughNodes() {
        MergeConflictGame g = new MergeConflictGame(new Random(42));
        g.pickConflictsIfNeeded(20);
        assertEquals(5, g.conflictIndices().size());
    }

    @Test
    void winAfterFiveResolves() {
        MergeConflictGame g = new MergeConflictGame(new Random(1));
        g.pickConflictsIfNeeded(10);
        for (int i = 0; i < 5; i++) {
            int idx = g.conflictIndices().iterator().next();
            assertTrue(g.resolveConflict(idx));
        }
        assertTrue(g.gameEnded());
        assertTrue(g.win());
    }

    @Test
    void loseWhenTimerExpires() {
        MergeConflictGame g = new MergeConflictGame();
        g.reset();
        boolean ended = false;
        for (int i = 0; i < 20 && !ended; i++) {
            ended = g.tick(30f);
        }
        assertTrue(ended);
        assertFalse(g.win());
    }

    @Test
    void timerWarningOnce() {
        MergeConflictGame g = new MergeConflictGame();
        g.reset();
        assertTrue(g.warningShouldPlay(11f, 9f));
        assertFalse(g.warningShouldPlay(9f, 8f));
    }
}

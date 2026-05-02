package com.gitvisualizer.game;

import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Random;
import java.util.Set;

/** Five-minute timer, five random conflict nodes, resolve-by-hold; host owns truth in co-op. */
public final class MergeConflictGame {

    public static final float TIMER_DURATION_SEC = 300f;
    public static final int CONFLICT_COUNT = 5;
    public static final float RESOLVE_HOLD_SEC = 3f;
    public static final float TIMER_WARNING_SEC = 10f;

    private final Random rng;
    private float remainingTime = TIMER_DURATION_SEC;
    private int resolvedCount;
    private final Set<Integer> conflictIndices = new LinkedHashSet<>();
    private boolean gameEnded;
    private boolean win;
    private boolean timerStarted = true;
    private boolean warningPlayed;

    public MergeConflictGame() {
        this(new Random());
    }

    public MergeConflictGame(Random rng) {
        this.rng = rng;
    }

    public void reset() {
        remainingTime = TIMER_DURATION_SEC;
        resolvedCount = 0;
        conflictIndices.clear();
        gameEnded = false;
        win = false;
        timerStarted = true;
        warningPlayed = false;
    }

    /**
     * Call when graph is ready (total nodes known). No-op if conflicts already chosen.
     */
    public void pickConflictsIfNeeded(int totalNodeCount) {
        if (!conflictIndices.isEmpty() || totalNodeCount < CONFLICT_COUNT) {
            return;
        }
        List<Integer> indices = new ArrayList<>();
        for (int i = 0; i < totalNodeCount; i++) {
            indices.add(i);
        }
        Collections.shuffle(indices, rng);
        for (int i = 0; i < CONFLICT_COUNT; i++) {
            conflictIndices.add(indices.get(i));
        }
    }

    /** Server tick; returns true if game just ended (loss). */
    public boolean tick(float deltaSeconds) {
        if (!timerStarted || gameEnded) {
            return false;
        }
        remainingTime -= deltaSeconds;
        if (remainingTime <= 0f) {
            remainingTime = 0f;
            endGame(false);
            return true;
        }
        return false;
    }

    public boolean resolveConflict(int nodeIndex) {
        if (gameEnded || !conflictIndices.contains(nodeIndex)) {
            return false;
        }
        conflictIndices.remove(nodeIndex);
        resolvedCount++;
        if (resolvedCount >= CONFLICT_COUNT) {
            endGame(true);
        }
        return true;
    }

    public boolean isConflictNode(int globalIndex) {
        return conflictIndices.contains(globalIndex);
    }

    private void endGame(boolean won) {
        gameEnded = true;
        win = won;
        timerStarted = false;
    }

    public float remainingTime() {
        return remainingTime;
    }

    public void setRemainingTime(float t) {
        this.remainingTime = Math.max(0f, t);
    }

    public int resolvedCount() {
        return resolvedCount;
    }

    public void setResolvedCount(int n) {
        this.resolvedCount = n;
    }

    public Set<Integer> conflictIndices() {
        return Collections.unmodifiableSet(conflictIndices);
    }

    public void replaceConflictIndices(List<Integer> indices) {
        conflictIndices.clear();
        if (indices != null) {
            for (Integer i : indices) {
                if (i != null) {
                    conflictIndices.add(i);
                }
            }
        }
    }

    public boolean gameEnded() {
        return gameEnded;
    }

    public boolean win() {
        return win;
    }

    public boolean timerStarted() {
        return timerStarted;
    }

    public boolean warningShouldPlay(float previousDisplayedSec, float currentDisplayedSec) {
        if (warningPlayed) {
            return false;
        }
        if (previousDisplayedSec > TIMER_WARNING_SEC && currentDisplayedSec <= TIMER_WARNING_SEC) {
            warningPlayed = true;
            return true;
        }
        return false;
    }

    public boolean isWarningPlayed() {
        return warningPlayed;
    }

    public void setWarningPlayed(boolean warningPlayed) {
        this.warningPlayed = warningPlayed;
    }

    /** Apply state pushed by the host (joining player does not simulate the timer locally). */
    public void applyNetworkSnapshot(float remainingTime, int resolved, List<Integer> conflicts, boolean ended, Boolean winFlag) {
        this.remainingTime = remainingTime;
        this.resolvedCount = resolved;
        replaceConflictIndices(conflicts != null ? conflicts : List.of());
        if (ended) {
            gameEnded = true;
            win = winFlag != null && winFlag;
            timerStarted = false;
        }
    }
}
